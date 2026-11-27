using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stillpoint
{
    enum ButtonKind { Primary, Ghost }

    static class Draw
    {
        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Text(Graphics g, string text, Font font, Color color, Rectangle bounds, TextFormatFlags align) =>
            TextRenderer.DrawText(g, text, font, bounds, color, align | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

        public static void Gear(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float r = Math.Min(bounds.Width, bounds.Height) * 0.2f;
            float cx = bounds.Left + bounds.Width / 2f, cy = bounds.Top + bounds.Height / 2f;
            using (var teeth = new Pen(color, r * 0.42f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
            {
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                    g.DrawLine(teeth, cx + dx * r * 0.7f, cy + dy * r * 0.7f, cx + dx * r * 1.18f, cy + dy * r * 1.18f);
                }
            }
            using (var body = new Pen(color, r * 0.42f))
                g.DrawEllipse(body, cx - r * 0.66f, cy - r * 0.66f, r * 1.32f, r * 1.32f);
        }

        public static void Clock(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Math.Max(1.4f, b.Width * 0.1f), r = b.Width * 0.42f, cx = b.Left + b.Width / 2f, cy = b.Top + b.Height / 2f;
            using (var pen = new Pen(color, w) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                g.DrawLine(pen, cx, cy, cx, cy - r * 0.55f);
                g.DrawLine(pen, cx, cy, cx + r * 0.42f, cy + r * 0.2f);
            }
        }

        public static void Stopwatch(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Math.Max(1.4f, b.Width * 0.1f), r = b.Width * 0.38f, cx = b.Left + b.Width / 2f, cy = b.Top + b.Height * 0.56f;
            using (var pen = new Pen(color, w) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                g.DrawLine(pen, cx, cy, cx + r * 0.42f, cy - r * 0.42f);
                g.DrawLine(pen, cx - r * 0.3f, b.Top + w / 2, cx + r * 0.3f, b.Top + w / 2);
                g.DrawLine(pen, cx, b.Top + w / 2, cx, cy - r);
            }
        }

        public static void Cross(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(b.Width, b.Height) * 0.16f, cx = b.Left + b.Width / 2f, cy = b.Top + b.Height / 2f;
            using (var pen = new Pen(color, Math.Max(1.3f, s * 0.35f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
            }
        }

        public static void Sprout(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Math.Max(1.4f, b.Width * 0.1f), s = b.Width, x = b.Left, y = b.Top;
            using (var pen = new Pen(color, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.DrawBezier(pen, x + s * 0.5f, y + s * 0.95f, x + s * 0.5f, y + s * 0.7f, x + s * 0.48f, y + s * 0.5f, x + s * 0.52f, y + s * 0.3f);
                g.DrawBezier(pen, x + s * 0.5f, y + s * 0.55f, x + s * 0.3f, y + s * 0.62f, x + s * 0.1f, y + s * 0.45f, x + s * 0.12f, y + s * 0.3f);
                g.DrawBezier(pen, x + s * 0.12f, y + s * 0.3f, x + s * 0.32f, y + s * 0.28f, x + s * 0.46f, y + s * 0.42f, x + s * 0.5f, y + s * 0.55f);
                g.DrawBezier(pen, x + s * 0.52f, y + s * 0.3f, x + s * 0.62f, y + s * 0.12f, x + s * 0.82f, y + s * 0.05f, x + s * 0.92f, y + s * 0.12f);
                g.DrawBezier(pen, x + s * 0.92f, y + s * 0.12f, x + s * 0.88f, y + s * 0.3f, x + s * 0.7f, y + s * 0.38f, x + s * 0.52f, y + s * 0.3f);
            }
        }

        public static void Ring(Graphics g, RectangleF bounds, float width, Color track, Color fill, float progress)
        {
            var r = RectangleF.Inflate(bounds, -width / 2, -width / 2);
            using (var pen = new Pen(track, width)) g.DrawEllipse(pen, r);
            if (360 * progress < 0.5f) return;
            using (var pen = new Pen(fill, width) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
            {
                if (progress >= 1) g.DrawEllipse(pen, r);
                else g.DrawArc(pen, r, -90, 360 * progress);
            }
        }
    }

    sealed class FlatButton : Button
    {
        bool hover;
        bool down;

        public FlatButton(ButtonKind kind)
        {
            Kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = Theme.SemiBold(10f);
        }

        public ButtonKind Kind { get; set; }
        public bool Uppercase { get; set; }

        public override Size GetPreferredSize(Size proposedSize)
        {
            string label = Uppercase ? Text.ToUpperInvariant() : Text;
            int text = TextRenderer.MeasureText(label, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int pad = (int)Math.Round(Font.Height * 1.1f);
            return new Size(Math.Max(MinimumSize.Width, text + pad * 2), Math.Max(MinimumSize.Height, Height));
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Math.Max(4f, Height * 0.14f);
            Color text;
            using (var path = Draw.Rounded(r, radius))
            {
                if (Kind == ButtonKind.Primary)
                {
                    var fill = !Enabled ? Theme.Track : down ? Theme.Muted : hover ? Theme.InkHover : Theme.Ink;
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                    text = Enabled ? Theme.OnInk : Theme.Muted;
                }
                else
                {
                    if (hover || down) using (var brush = new SolidBrush(down ? Theme.Track : Theme.Tile)) g.FillPath(brush, path);
                    using (var pen = new Pen(Theme.Border, 1f)) g.DrawPath(pen, path);
                    text = Enabled ? Theme.Ink : Theme.Muted;
                }
            }
            if (Glyph != null)
            {
                Glyph(g, ClientRectangle, text);
                return;
            }
            string label = Uppercase ? Text.ToUpperInvariant() : Text;
            if (Icon != null)
            {
                int textWidth = TextRenderer.MeasureText(g, label, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
                int icon = (int)Math.Round(Font.Height * 0.95f), gap = (int)Math.Round(Font.Height * 0.4f);
                int left = (Width - (icon + gap + textWidth)) / 2;
                Icon(g, new Rectangle(left, (Height - icon) / 2, icon, icon), text);
                Draw.Text(g, label, Font, text, new Rectangle(left + icon + gap, 0, textWidth + 2, Height), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                return;
            }
            Draw.Text(g, label, Font, text, ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public Action<Graphics, Rectangle, Color> Glyph { get; set; }
        public Action<Graphics, Rectangle, Color> Icon { get; set; }
    }

    sealed class ToggleSwitch : Control
    {
        bool on;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Size = new Size(34, 18);
        }

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get => on;
            set
            {
                if (on == value) return;
                on = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = Draw.Rounded(r, r.Height / 2))
            using (var brush = new SolidBrush(on ? Theme.Accent : Theme.Border))
                g.FillPath(brush, path);
            float knob = Height - Height * 0.34f;
            float x = on ? Width - knob - Height * 0.17f : Height * 0.17f;
            using (var brush = new SolidBrush(Color.White)) g.FillEllipse(brush, x, (Height - knob) / 2, knob, knob);
        }
    }

    sealed class ListRow
    {
        public string Left = "";
        public string Middle = "";
        public string Right = "";
        public bool Header;

        public bool Same(ListRow other) =>
            Left == other.Left && Middle == other.Middle && Right == other.Right && Header == other.Header;
    }

    sealed class SessionList : Control
    {
        List<ListRow> rows = new List<ListRow>();
        int offset;
        readonly Font bold = Theme.SemiBold(9.5f);
        readonly Glide glide;

        public SessionList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Font = Theme.Regular(9.5f);
            glide = new Glide(v =>
            {
                int next = (int)Math.Round(v);
                if (next == offset) return;
                offset = next;
                Invalidate();
            });
        }

        public string EmptyText { get; set; } = "Sessions you count show up here.";

        public void SetRows(List<ListRow> next, bool toTop = false)
        {
            bool same = next.Count == rows.Count;
            for (int i = 0; same && i < next.Count; i++) same = next[i].Same(rows[i]);
            if (toTop) glide.Jump(0);
            if (same && !toTop) return;
            rows = next;
            glide.Jump(Math.Min(glide.Target, MaxOffset()));
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { bold.Dispose(); glide.Dispose(); }
            base.Dispose(disposing);
        }

        int RowHeight => (int)(Font.Height * 1.9f);

        int Pad => RowHeight / 2;

        Rectangle Inner => new Rectangle(Pad, Pad / 2, Math.Max(0, Width - Pad * 2), Math.Max(0, Height - Pad));

        int MaxOffset() => Math.Max(0, rows.Count * RowHeight - Inner.Height);

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Focus();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            glide.To(Math.Max(0, Math.Min(MaxOffset(), glide.Target - e.Delta / 120f * RowHeight * 2.5f)));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (glide != null && glide.Target > MaxOffset()) glide.Jump(MaxOffset());
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float radius = Font.Height * 0.6f;
            using (var card = Draw.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), radius))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var edge = new Pen(Theme.Track))
            {
                g.FillPath(fill, card);
                g.DrawPath(edge, card);
            }
            g.SmoothingMode = SmoothingMode.None;
            var inner = Inner;
            int h = RowHeight;
            g.SetClip(inner);
            if (rows.Count == 0)
            {
                Draw.Text(g, EmptyText, Font, Theme.Muted, new Rectangle(inner.Left, inner.Top, inner.Width, h), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                return;
            }
            int gap = h / 2;
            int left = inner.Left;
            int right = inner.Right;
            int indent = rows[0].Header ? Font.Height / 2 : 0;
            int lengthWidth = TextRenderer.MeasureText(g, "00h 00m", bold, Size.Empty, TextFormatFlags.NoPadding).Width;
            int middleWidth = TextRenderer.MeasureText(g, "00 sessions", Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            using (var line = new Pen(Theme.Line))
            using (var strong = new Pen(Theme.Border))
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    int y = inner.Top + i * h - offset;
                    if (y + h < inner.Top) continue;
                    if (y > inner.Bottom) break;
                    var row = rows[i];
                    if (i > 0) g.DrawLine(row.Header ? strong : line, left, y, right, y);
                    var font = row.Header ? bold : Font;
                    int x = row.Header ? left : left + indent;
                    int leftWidth = right - x - lengthWidth - gap - (row.Middle.Length > 0 ? middleWidth + gap : 0);
                    Draw.Text(g, row.Left, font, Theme.Ink, new Rectangle(x, y, leftWidth, h), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    if (row.Middle.Length > 0)
                        Draw.Text(g, row.Middle, Font, Theme.Muted, new Rectangle(right - lengthWidth - gap - middleWidth, y, middleWidth, h), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                    Draw.Text(g, row.Right, font, row.Header ? Theme.AccentText : Theme.Ink, new Rectangle(right - lengthWidth, y, lengthWidth, h), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
            }
            g.ResetClip();
            int max = MaxOffset();
            if (max > 0)
            {
                float visible = (float)inner.Height / (rows.Count * h);
                float barH = Math.Max(h, inner.Height * visible);
                float barY = inner.Top + (inner.Height - barH) * offset / max;
                using (var brush = new SolidBrush(Theme.Border)) g.FillRectangle(brush, Width - Pad / 2 - 2, barY, 3, barH);
            }
        }
    }
}

