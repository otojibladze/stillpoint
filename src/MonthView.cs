using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class MonthView : Control
    {
        const double FullDaySeconds = 8 * 3600;

        Dictionary<DateTime, double> totals = new Dictionary<DateTime, double>();
        DateTime month;
        DateTime selected;
        int hover = -1;
        readonly Font titleFont = Theme.SemiBold(11.5f);
        readonly Font smallFont = Theme.Regular(8f);
        readonly Font boldFont = Theme.SemiBold(9.5f);

        public MonthView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Font = Theme.Regular(9.5f);
            selected = DateTime.Today;
            month = First(selected);
        }

        public event EventHandler SelectedChanged;

        public DateTime Selected
        {
            get => selected;
            set
            {
                var day = value.Date;
                if (day == selected) return;
                selected = day;
                month = First(day);
                Invalidate();
                SelectedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetTotals(Dictionary<DateTime, double> next)
        {
            bool same = next.Count == totals.Count;
            if (same)
                foreach (var pair in next)
                    if (!totals.TryGetValue(pair.Key, out double old) || (long)old / 60 != (long)pair.Value / 60) { same = false; break; }
            totals = next;
            if (!same) Invalidate();
        }

        static DateTime First(DateTime day) => new DateTime(day.Year, day.Month, 1);

        static DayOfWeek FirstDay => CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

        DateTime GridStart => month.AddDays(-(((int)month.DayOfWeek - (int)FirstDay + 7) % 7));

        int HeaderHeight => (int)(titleFont.Height * 2.4f);

        int WeekHeight => (int)(smallFont.Height * 2.2f);

        public override Size GetPreferredSize(Size proposedSize) =>
            new Size(proposedSize.Width, HeaderHeight + WeekHeight + 6 * (boldFont.Height + smallFont.Height + (int)(Font.Height * 0.7f)));

        Rectangle PrevBox => new Rectangle(Width - HeaderHeight * 2, 0, HeaderHeight, HeaderHeight);

        Rectangle NextBox => new Rectangle(Width - HeaderHeight, 0, HeaderHeight, HeaderHeight);

        Rectangle TodayBox
        {
            get
            {
                int w = TextRenderer.MeasureText("Today", Font, Size.Empty, TextFormatFlags.NoPadding).Width + Font.Height;
                return new Rectangle(PrevBox.Left - w, 0, w, HeaderHeight);
            }
        }

        RectangleF Cell(int index)
        {
            float top = HeaderHeight + WeekHeight;
            float w = Width / 7f;
            float h = (Height - top) / 6f;
            return new RectangleF(index % 7 * w, top + index / 7 * h, w, h);
        }

        int CellAt(Point p)
        {
            if (p.Y < HeaderHeight + WeekHeight) return -1;
            for (int i = 0; i < 42; i++)
                if (Cell(i).Contains(p)) return i;
            return -1;
        }

        void ShowMonth(int delta)
        {
            month = month.AddMonths(delta);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int days = e.KeyCode == Keys.Left ? -1 : e.KeyCode == Keys.Right ? 1 : e.KeyCode == Keys.Up ? -7 : e.KeyCode == Keys.Down ? 7 : 0;
            if (e.KeyCode == Keys.PageUp) ShowMonth(-1);
            else if (e.KeyCode == Keys.PageDown) ShowMonth(1);
            else if (days != 0) Selected = selected.AddDays(days);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ShowMonth(e.Delta > 0 ? -1 : 1);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int next = CellAt(e.Location);
            bool button = PrevBox.Contains(e.Location) || NextBox.Contains(e.Location) || TodayBox.Contains(e.Location);
            Cursor = next >= 0 || button ? Cursors.Hand : Cursors.Default;
            if (next == hover) return;
            hover = next;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left) return;
            if (PrevBox.Contains(e.Location)) ShowMonth(-1);
            else if (NextBox.Contains(e.Location)) ShowMonth(1);
            else if (TodayBox.Contains(e.Location))
            {
                Selected = DateTime.Today;
                month = First(DateTime.Today);
                Invalidate();
            }
            else
            {
                int cell = CellAt(e.Location);
                if (cell >= 0) Selected = GridStart.AddDays(cell);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            var culture = CultureInfo.CurrentCulture;
            int header = HeaderHeight;

            Draw.Text(g, month.ToString("MMMM yyyy", culture), titleFont, Theme.Ink, new Rectangle(2, 0, TodayBox.Left - 2, header), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            Draw.Text(g, "Today", Font, Theme.AccentText, TodayBox, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Chevron(g, PrevBox, -1);
            Chevron(g, NextBox, 1);

            var names = culture.DateTimeFormat.AbbreviatedDayNames;
            for (int i = 0; i < 7; i++)
            {
                var cell = Cell(i);
                string name = names[((int)FirstDay + i) % 7];
                Draw.Text(g, name, smallFont, Theme.Muted, new Rectangle((int)cell.X, header, (int)cell.Width, WeekHeight), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            var start = GridStart;
            float inset = Math.Max(2f, Font.Height * 0.12f);
            float radius = Font.Height * 0.35f;
            for (int i = 0; i < 42; i++)
            {
                var day = start.AddDays(i);
                bool inMonth = day.Month == month.Month;
                totals.TryGetValue(day, out double seconds);
                var box = RectangleF.Inflate(Cell(i), -inset, -inset);
                float t = (float)Math.Min(1, seconds / FullDaySeconds);
                Color fill = seconds > 0 ? Blend(Theme.Surface, Theme.Accent, 0.16f + 0.5f * t) : inMonth ? Theme.Surface : Theme.Background;
                using (var path = Draw.Rounded(box, radius))
                {
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                    if (day == selected)
                        using (var pen = new Pen(Theme.Ink, Math.Max(2f, Font.Height * 0.11f))) g.DrawPath(pen, path);
                    else if (i == hover)
                        using (var pen = new Pen(Theme.Border, 1f)) g.DrawPath(pen, path);
                }

                bool today = day == DateTime.Today;
                var numberFont = today ? boldFont : Font;
                Color numberColor = today ? Theme.AccentText : inMonth ? Theme.Ink : Theme.Border;
                if (today && t > 0.5f) numberColor = Theme.Ink;
                var r = Rectangle.Round(box);
                int numberH = numberFont.Height;
                int timeH = seconds >= 60 ? smallFont.Height : 0;
                int y = r.Top + (r.Height - numberH - timeH) / 2;
                Draw.Text(g, day.Day.ToString(culture), numberFont, numberColor, new Rectangle(r.Left, y, r.Width, numberH), TextFormatFlags.HorizontalCenter);
                if (timeH > 0)
                    Draw.Text(g, Fmt.Hm(seconds), smallFont, inMonth ? Theme.Ink : Theme.Muted, new Rectangle(r.Left, y + numberH, r.Width, timeH), TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        void Chevron(Graphics g, Rectangle box, int direction)
        {
            float s = Font.Height * 0.28f;
            float cx = box.Left + box.Width / 2f, cy = box.Top + box.Height / 2f;
            using (var pen = new Pen(Theme.Ink, Math.Max(1.5f, Font.Height * 0.09f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - direction * s * 0.5f, cy - s),
                    new PointF(cx + direction * s * 0.5f, cy),
                    new PointF(cx - direction * s * 0.5f, cy + s),
                });
        }

        static Color Blend(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                titleFont.Dispose();
                smallFont.Dispose();
                boldFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
