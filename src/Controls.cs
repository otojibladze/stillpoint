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

        public static void Ring(Graphics g, RectangleF bounds, float width, Color track, Color fill, float progress)
        {
            var r = RectangleF.Inflate(bounds, -width / 2, -width / 2);
            using (var pen = new Pen(track, width)) g.DrawEllipse(pen, r);
            if (progress <= 0) return;
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
                    var fill = !Enabled ? Theme.Track : down ? Color.Black : hover ? Theme.InkHover : Theme.Ink;
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
            Draw.Text(g, label, Font, text, ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public Action<Graphics, Rectangle, Color> Glyph { get; set; }
    }

    sealed class SessionRing : Control
    {
        public SessionRing()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public float Progress { get; private set; }
        public string Caption { get; private set; } = "";
        public string Value { get; private set; } = "";
        public string Note { get; private set; } = "";

        public void Set(float progress, string caption, string value, string note)
        {
            if (Math.Abs(progress - Progress) < 0.0005f && caption == Caption && value == Value && note == Note) return;
            Progress = progress;
            Caption = caption;
            Value = value;
            Note = note;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height);
            var box = new RectangleF((Width - size) / 2, (Height - size) / 2, size, size);
            Draw.Ring(g, box, size * 0.04f, Theme.Track, Theme.Accent, Progress);

            float grow = size / (196f * g.DpiX / 96f);
            float small = Math.Min(grow, 1.25f);
            using (var captionFont = Theme.SemiBold(8f * small))
            using (var valueFont = Theme.Light(28f * grow))
            using (var noteFont = Theme.Regular(9f * small))
            {
                var lines = new[]
                {
                    (Caption, captionFont, Theme.Muted),
                    (Value, valueFont, Theme.Ink),
                    (Note, noteFont, Theme.AccentText),
                };
                int gap = (int)(size * 0.03f);
                int total = 0;
                var heights = new int[lines.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    heights[i] = TextRenderer.MeasureText(g, string.IsNullOrEmpty(lines[i].Item1) ? " " : lines[i].Item1, lines[i].Item2, Size.Empty, TextFormatFlags.NoPadding).Height;
                    total += heights[i] + (i > 0 ? gap : 0);
                }
                int y = (Height - total) / 2;
                for (int i = 0; i < lines.Length; i++)
                {
                    Draw.Text(g, lines[i].Item1, lines[i].Item2, lines[i].Item3, new Rectangle(0, y, Width, heights[i]), TextFormatFlags.HorizontalCenter);
                    y += heights[i] + gap;
                }
            }
        }
    }

    sealed class MiniRing : Control
    {
        public MiniRing()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.SemiBold(9f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height);
            Draw.Ring(g, new RectangleF(0, 0, size, size), size * 0.09f, Theme.Track, Theme.Accent, 1f);
            Draw.Text(g, Text, Font, Theme.Ink, new Rectangle(0, 0, (int)size, (int)size), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
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

    sealed class UnderlineBox : Panel
    {
        public UnderlineBox(float points, int width, HorizontalAlignment align)
        {
            Box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.Regular(points),
                TextAlign = align,
                BackColor = Theme.Background,
                ForeColor = Theme.Ink,
                ShortcutsEnabled = false,
            };
            Width = width;
            Height = Box.PreferredHeight + 8;
            Box.Location = new Point(0, 2);
            Box.Width = width;
            Box.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            Controls.Add(Box);
            Box.GotFocus += (s, e) => Invalidate();
            Box.LostFocus += (s, e) => Invalidate();
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public TextBox Box { get; }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            Box.BackColor = BackColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float width = Math.Max(1.5f, Height * 0.05f);
            using (var pen = new Pen(Box.Focused ? Theme.Accent : Theme.Ink, width))
                e.Graphics.DrawLine(pen, 0, Height - width / 2, Width, Height - width / 2);
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

        public SessionList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Font = Theme.Regular(9.5f);
        }

        public string EmptyText { get; set; } = "Sessions you count show up here.";

        public void SetRows(List<ListRow> next, bool toTop = false)
        {
            bool same = next.Count == rows.Count;
            for (int i = 0; same && i < next.Count; i++) same = next[i].Same(rows[i]);
            if (toTop) offset = 0;
            if (same && !toTop) return;
            rows = next;
            offset = Math.Min(offset, MaxOffset());
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) bold.Dispose();
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
            int next = Math.Max(0, Math.Min(MaxOffset(), offset - Math.Sign(e.Delta) * RowHeight * 2));
            if (next == offset) return;
            offset = next;
            Invalidate();
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
