using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class NavRail : Control
    {
        sealed class Item
        {
            public string Key, Tip;
            public Action<Graphics, Rectangle, Color> Glyph;
            public bool Bottom, Hidden;
            public Rectangle Bounds;
        }

        readonly List<Item> items = new List<Item>();
        readonly ToolTip tip = new ToolTip();
        string active, hover;

        public NavRail()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public event Action<string> Picked;

        public void Add(string key, string tipText, Action<Graphics, Rectangle, Color> glyph, bool bottom = false)
        {
            items.Add(new Item { Key = key, Tip = tipText, Glyph = glyph, Bottom = bottom });
            Invalidate();
        }

        public string Active
        {
            get => active;
            set
            {
                if (active == value) return;
                active = value;
                Invalidate();
            }
        }

        public void SetHidden(string key, bool hidden)
        {
            foreach (var item in items)
                if (item.Key == key) item.Hidden = hidden;
            Invalidate();
        }

        void Arrange()
        {
            int size = (int)Math.Round(Width * 0.72f), gap = (int)Math.Round(Width * 0.1f), x = (Width - size) / 2;
            int top = gap + gap / 2, bottom = Height - gap - gap / 2 - size;
            foreach (var item in items)
            {
                if (item.Hidden) continue;
                if (item.Bottom)
                {
                    item.Bounds = new Rectangle(x, bottom, size, size);
                    bottom -= size + gap;
                }
                else
                {
                    item.Bounds = new Rectangle(x, top, size, size);
                    top += size + gap;
                }
            }
        }

        Item At(Point p)
        {
            foreach (var item in items)
                if (!item.Hidden && item.Bounds.Contains(p)) return item;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var item = At(e.Location);
            string key = item?.Key;
            Cursor = item != null ? Cursors.Hand : Cursors.Default;
            if (key == hover) return;
            hover = key;
            tip.SetToolTip(this, item?.Tip ?? "");
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = null;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var item = At(e.Location);
            if (e.Button == MouseButtons.Left && item != null) Picked?.Invoke(item.Key);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Tile);
            Arrange();
            using (var line = new Pen(Theme.Track)) g.DrawLine(line, Width - 1, 0, Width - 1, Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var item in items)
            {
                if (item.Hidden) continue;
                var r = item.Bounds;
                bool on = item.Key == active;
                if (on || item.Key == hover)
                    using (var path = Draw.Rounded(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), r.Width * 0.24f))
                    using (var fill = new SolidBrush(on ? Theme.Surface : Theme.Line))
                        g.FillPath(fill, path);
                if (on)
                {
                    float bar = Math.Max(2f, Width * 0.05f);
                    using (var path = Draw.Rounded(new RectangleF(0, r.Y + r.Height * 0.26f, bar, r.Height * 0.48f), bar / 2))
                    using (var fill = new SolidBrush(Theme.Accent))
                        g.FillPath(fill, path);
                }
                int glyph = (int)Math.Round(r.Width * 0.46f);
                item.Glyph(g, new Rectangle(r.X + (r.Width - glyph) / 2, r.Y + (r.Height - glyph) / 2, glyph, glyph), on ? Theme.Ink : Theme.Muted);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) tip.Dispose();
            base.Dispose(disposing);
        }
    }
}
