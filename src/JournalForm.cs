using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class JournalForm : Form
    {
        readonly JournalView view;

        public JournalForm(TrayApp app)
        {
            SuspendLayout();
            Ui.SetupWindow(this, "Pebble's journal", new Size(440, 640));
            ShowInTaskbar = false;
            var root = Ui.Column(new Padding(22, 4, 22, 20));
            view = new JournalView(app) { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0) };
            Ui.AddRow(root, view);
            Controls.Add(new ScrollView(root));
            ResumeLayout(false);
            PerformLayout();
        }

        public void RefreshEntries() => view.Reload(false);
    }

    sealed class JournalView : Control
    {
        const TextFormatFlags Wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsTranslateTransform;
        const TextFormatFlags Line = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsTranslateTransform;
        const int Step = 30, Cap = 180;
        readonly TrayApp app;
        readonly Font title = Theme.SemiBold(10f);
        readonly Font small = Theme.Regular(9f);
        readonly List<(DateTime Day, string Text)> entries = new List<(DateTime, string)>();
        readonly List<(int Index, Rectangle Box, int TextTop)> layout = new List<(int, Rectangle, int)>();
        readonly Dictionary<DateTime, Bitmap> thumbs = new Dictionary<DateTime, Bitmap>();
        int shown = Step, layoutWidth = -1, layoutHeight, thumbBack;
        Rectangle more;
        string key = "";
        bool hasMore;

        public JournalView(TrayApp app)
        {
            this.app = app;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            Font = Theme.Regular(9.5f);
            Reload(true);
        }

        int Pad => Font.Height;
        int Thumb => Font.Height * 4;

        public void Reload(bool force)
        {
            var sessions = app.Tracker.Sessions;
            string next = sessions.Count + "|" + app.Album.Version + "|" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "|" + shown + "|" + (int)(DateTime.Now.TimeOfDay.TotalMinutes / 10);
            if (!force && next == key) return;
            key = next;
            entries.Clear();
            var today = DateTime.Today;
            var first = today;
            foreach (var s in sessions) if (s.Start.Date < first) first = s.Start.Date;
            int total = (today - first).Days + 1;
            for (int i = 0; i < Math.Min(shown, total); i++)
            {
                var day = today.AddDays(-i);
                entries.Add((day, app.JournalEntry(day)));
            }
            hasMore = total > shown && shown < Cap;
            layoutWidth = -1;
            foreach (var b in thumbs.Values) b.Dispose();
            thumbs.Clear();
            Parent?.PerformLayout();
            Invalidate();
        }

        void Measure(int width)
        {
            if (width == layoutWidth) return;
            layoutWidth = width;
            layout.Clear();
            int y = 0, textLeft = Pad + Thumb + Pad, textWidth = Math.Max(40, width - textLeft - Pad);
            for (int i = 0; i < entries.Count; i++)
            {
                int textTop = Pad + title.Height + Font.Height / 3;
                int textHeight = TextRenderer.MeasureText(entries[i].Text, Font, new Size(textWidth, int.MaxValue), Wrap).Height;
                int height = Math.Max(Pad * 2 + Thumb, textTop + textHeight + Pad);
                layout.Add((i, new Rectangle(0, y, width, height), textTop));
                y += height + Font.Height * 3 / 4;
            }
            more = hasMore ? new Rectangle(0, y, width, Font.Height * 2) : Rectangle.Empty;
            if (hasMore) y += more.Height;
            layoutHeight = Math.Max(1, y);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = proposedSize.Width > 1 && proposedSize.Width < 20000 ? proposedSize.Width : Math.Max(Width, 200);
            Measure(width);
            return new Size(width, layoutHeight);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width != layoutWidth) Parent?.PerformLayout();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = hasMore && more.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!hasMore || !more.Contains(e.Location)) return;
            shown = Math.Min(Cap, shown + Step);
            Reload(true);
        }

        Bitmap ThumbFor(DateTime day, int size, Color around)
        {
            if (around.ToArgb() != thumbBack)
            {
                thumbBack = around.ToArgb();
                foreach (var old in thumbs.Values) old.Dispose();
                thumbs.Clear();
            }
            if (thumbs.TryGetValue(day, out var bmp) && bmp.Width == size) return bmp;
            bmp?.Dispose();
            bmp = GardenPicture.Render(app.GardenOn(day), 0, new Size(size, size), size * 0.18f, around);
            thumbs[day] = bmp;
            return bmp;
        }

        string Heading(DateTime day)
        {
            var culture = CultureInfo.CurrentCulture;
            if (day == DateTime.Today) return "Today";
            if (day == DateTime.Today.AddDays(-1)) return "Yesterday";
            return day.ToString("dddd d MMMM", culture);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var clip = Rectangle.Intersect(e.ClipRectangle, ClientRectangle);
            if (clip.Width <= 0 || clip.Height <= 0) return;
            Measure(Width);
            using (var buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, clip))
            {
                var g = buffer.Graphics;
                g.Clear(Parent?.BackColor ?? Theme.Background);
                int textLeft = Pad + Thumb + Pad, textWidth = Math.Max(40, Width - textLeft - Pad);
                using (var fill = new SolidBrush(Theme.Surface))
                using (var edge = new Pen(Theme.Track))
                    foreach (var (index, box, textTop) in layout)
                    {
                        if (box.Bottom < clip.Top) continue;
                        if (box.Top > clip.Bottom) break;
                        var (day, text) = entries[index];
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var card = Draw.Rounded(new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1.5f, box.Height - 1.5f), Font.Height * 0.6f))
                        {
                            g.FillPath(fill, card);
                            g.DrawPath(edge, card);
                        }
                        g.SmoothingMode = SmoothingMode.None;
                        g.DrawImage(ThumbFor(day, Thumb, Theme.Surface), box.X + Pad, box.Y + Pad);
                        TextRenderer.DrawText(g, Heading(day), title, new Rectangle(box.X + textLeft, box.Y + Pad, textWidth, title.Height), Theme.Ink, Line | TextFormatFlags.Left);
                        if (day != DateTime.Today && day != DateTime.Today.AddDays(-1))
                            TextRenderer.DrawText(g, day.Year == DateTime.Today.Year ? "" : day.Year.ToString(CultureInfo.CurrentCulture), small, new Rectangle(box.X + textLeft, box.Y + Pad, textWidth, title.Height), Theme.Muted, Line | TextFormatFlags.Right);
                        TextRenderer.DrawText(g, text, Font, new Rectangle(box.X + textLeft, box.Y + textTop, textWidth, box.Height - textTop), Theme.Ink, Wrap);
                    }
                if (hasMore && more.IntersectsWith(clip))
                    TextRenderer.DrawText(g, "Show older days", Font, more, Theme.AccentText, Line | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                buffer.Render(e.Graphics);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                title.Dispose();
                small.Dispose();
                foreach (var b in thumbs.Values) b.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
