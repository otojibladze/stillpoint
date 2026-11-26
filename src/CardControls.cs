using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class Card : TableLayoutPanel
    {
        public Card()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            ColumnCount = 1;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            AutoSize = true;
            Dock = DockStyle.Fill;
            Margin = new Padding(0);
            Padding = new Padding(4);
            BackColor = Theme.Surface;
        }

        public void Clear()
        {
            Controls.Clear();
            RowStyles.Clear();
            RowCount = 0;
        }

        public void AddRow(Control left, Control right)
        {
            var row = Ui.TwoSides(left, right);
            row.Padding = new Padding(12, 10, 12, 10);
            row.BackColor = Theme.Surface;
            if (RowCount > 0)
                row.Paint += (s, e) =>
                {
                    using (var line = new Pen(Theme.Line)) e.Graphics.DrawLine(line, 0, 0, row.Width, 0);
                };
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(row, 0, RowCount++);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float radius = Font.Height * 0.75f;
            using (var path = Draw.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), radius))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var edge = new Pen(Theme.Track))
            {
                g.FillPath(fill, path);
                g.DrawPath(edge, path);
            }
        }
    }

    sealed class Stepper : Control
    {
        int value = 60;
        int pressed;

        public Stepper()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Font = Theme.SemiBold(10f);
            Size = new Size(146, 32);
            Cursor = Cursors.Hand;
        }

        public int Minimum { get; set; } = 1;
        public int Maximum { get; set; } = 999;
        public event EventHandler ValueChanged;

        public int Value
        {
            get => value;
            set
            {
                int next = Math.Max(Minimum, Math.Min(Maximum, value));
                if (next == this.value) return;
                this.value = next;
                Invalidate();
            }
        }

        static int StepFrom(int v, int direction)
        {
            int step = direction > 0 ? (v < 30 ? 5 : v < 120 ? 15 : 30) : (v <= 30 ? 5 : v <= 120 ? 15 : 30);
            int next = v + direction * step;
            return direction > 0 ? next - next % step : next + (step - next % step) % step;
        }

        void Step(int direction)
        {
            int before = value;
            Value = StepFrom(value, direction);
            if (value != before) ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        int Zone => Height;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            pressed = e.X < Zone ? -1 : e.X > Width - Zone ? 1 : 0;
            if (pressed != 0) Step(pressed);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = 0;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Step(Math.Sign(e.Delta));
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) Step(1);
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) Step(-1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            float radius = Height * 0.28f;
            using (var path = Draw.Rounded(r, radius))
            using (var fill = new SolidBrush(Theme.Background))
            using (var edge = new Pen(Theme.Border))
            {
                g.FillPath(fill, path);
                if (pressed != 0)
                {
                    var clip = pressed < 0 ? new RectangleF(0, 0, Zone, Height) : new RectangleF(Width - Zone, 0, Zone, Height);
                    g.SetClip(clip);
                    using (var down = new SolidBrush(Theme.Track)) g.FillPath(down, path);
                    g.ResetClip();
                }
                g.DrawPath(edge, path);
            }
            float s = Height * 0.17f;
            float w = Math.Max(1.5f, Height * 0.05f);
            using (var pen = new Pen(value > Minimum ? Theme.Ink : Theme.Border, w))
                g.DrawLine(pen, Zone / 2f - s, Height / 2f, Zone / 2f + s, Height / 2f);
            using (var pen = new Pen(value < Maximum ? Theme.Ink : Theme.Border, w))
            {
                float cx = Width - Zone / 2f;
                g.DrawLine(pen, cx - s, Height / 2f, cx + s, Height / 2f);
                g.DrawLine(pen, cx, Height / 2f - s, cx, Height / 2f + s);
            }
            string text = value.ToString(CultureInfo.CurrentCulture) + " min";
            Draw.Text(g, text, Font, Theme.Ink, new Rectangle(Zone, 0, Width - Zone * 2, Height), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    sealed class PlayButton : Control
    {
        bool hover;

        public PlayButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(28, 28);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height) - 2f;
            float w = Math.Max(1.5f, size * 0.055f);
            var circle = new RectangleF(1f, 1f, size, size);
            var color = hover ? Theme.Accent : Theme.AccentText;
            if (hover) using (var fill = new SolidBrush(Theme.Accent)) g.FillEllipse(fill, circle);
            else using (var pen = new Pen(color, w)) g.DrawEllipse(pen, RectangleF.Inflate(circle, -w / 2, -w / 2));
            float cx = 1f + size / 2f + size * 0.05f, cy = 1f + size / 2f, t = size * 0.17f;
            using (var brush = new SolidBrush(hover ? Theme.OnInk : color))
                g.FillPolygon(brush, new[] { new PointF(cx - t * 0.8f, cy - t), new PointF(cx + t, cy), new PointF(cx - t * 0.8f, cy + t) });
        }
    }
}
