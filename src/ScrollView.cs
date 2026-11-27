using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class ScrollView : Panel
    {
        readonly Control content;
        readonly ScrollBarStrip bar;
        readonly Glide glide;
        int offset;
        bool fitting;

        public ScrollView(Control content)
        {
            glide = new Glide(v => Apply((int)Math.Round(v)));
            this.content = content;
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            Margin = new Padding(0);
            BackColor = content.BackColor;
            content.Dock = DockStyle.None;
            if (content is ScrollableControl scrollable) scrollable.AutoScroll = false;
            bar = new ScrollBarStrip(this) { Dock = DockStyle.Right, Width = 12, Visible = false };
            Controls.Add(content);
            Controls.Add(bar);
            content.Layout += (s, e) => Fit();
        }

        public bool FillViewport { get; set; }

        internal int ContentHeight => content.Height;
        internal int Offset => offset;
        internal int MaxOffset => Math.Max(0, content.Height - ClientSize.Height);

        void Fit()
        {
            if (fitting) return;
            fitting = true;
            int width = ClientSize.Width - (bar.Visible ? bar.Width : 0);
            int height = content.GetPreferredSize(new Size(width, 0)).Height;
            bool needBar = height > ClientSize.Height;
            if (needBar != bar.Visible)
            {
                bar.Visible = needBar;
                width = ClientSize.Width - (needBar ? bar.Width : 0);
                height = content.GetPreferredSize(new Size(width, 0)).Height;
            }
            if (FillViewport) height = Math.Max(height, ClientSize.Height);
            var bounds = new Rectangle(0, -Math.Min(offset, Math.Max(0, height - ClientSize.Height)), Math.Max(0, width), height);
            offset = -bounds.Y;
            if (content.Bounds != bounds) content.Bounds = bounds;
            if (glide.Target > MaxOffset) glide.Jump(MaxOffset);
            bar.Invalidate();
            fitting = false;
        }

        int Clamp(float value) => (int)Math.Max(0, Math.Min(MaxOffset, value));

        internal void ScrollTo(int value) => glide.Jump(Clamp(value));

        internal void GlideTo(float value) => glide.To(Clamp(value));

        internal void WheelBy(int delta) => GlideTo(glide.Target - delta / 120f * Font.Height * 4);

        void Apply(int next)
        {
            if (next == offset) return;
            offset = next;
            content.Top = -offset;
            bar.Invalidate();
            Update();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) glide.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Fit();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            WheelBy(e.Delta);
        }

        sealed class ScrollBarStrip : Control
        {
            readonly ScrollView view;
            bool hover, dragging;
            int dragStart, dragOffset;

            public ScrollBarStrip(ScrollView view)
            {
                this.view = view;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            RectangleF Thumb()
            {
                int total = Math.Max(1, view.ContentHeight), visible = view.ClientSize.Height;
                float pad = Width * 0.2f, track = Height - pad * 2;
                float length = Math.Max(Width * 2.5f, track * visible / total);
                float y = pad + (track - length) * (view.MaxOffset == 0 ? 0 : (float)view.Offset / view.MaxOffset);
                float w = hover || dragging ? Width * 0.5f : Width * 0.34f;
                return new RectangleF((Width - w) / 2, y, w, length);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(view.BackColor);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var thumb = Thumb();
                using (var path = Draw.Rounded(thumb, thumb.Width / 2))
                using (var brush = new SolidBrush(dragging ? Theme.Muted : hover ? Theme.Muted : Theme.Border))
                    g.FillPath(brush, path);
            }

            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) return;
                var thumb = Thumb();
                if (e.Y < thumb.Top || e.Y > thumb.Bottom)
                {
                    view.GlideTo(view.Offset + Math.Sign(e.Y - thumb.Top) * view.ClientSize.Height * 9 / 10);
                    return;
                }
                dragging = true;
                dragStart = e.Y;
                dragOffset = view.Offset;
                Capture = true;
                Invalidate();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (!dragging) return;
                var thumb = Thumb();
                float track = Height - Width * 0.4f - thumb.Height;
                if (track <= 0) return;
                view.ScrollTo(dragOffset + (int)Math.Round((e.Y - dragStart) * view.MaxOffset / track));
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                dragging = false;
                Capture = false;
                Invalidate();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                view.WheelBy(e.Delta);
            }
        }
    }
}
