using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class PageStack : Panel
    {
        const float Seconds = 0.38f;
        readonly Dictionary<string, (Control Page, int Order)> pages = new Dictionary<string, (Control, int)>();
        readonly Curtain curtain = new Curtain { Visible = false };
        readonly Frames frames;
        Control shown;
        bool curtainUp;
        float elapsed;
        int direction;

        public PageStack()
        {
            DoubleBuffered = true;
            frames = new Frames(Step);
            Controls.Add(curtain);
        }

        public string Current { get; private set; }

        public void Show(string key, Control page, int order)
        {
            if (!pages.ContainsKey(key))
            {
                pages[key] = (page, order);
                page.Visible = false;
                Controls.Add(page);
            }
            if (key == Current) return;
            Finish();
            var from = Current != null ? pages[Current] : (null, 0);
            Current = key;
            page.Bounds = ClientRectangle;
            var before = from.Item1 != null && IsHandleCreated && Visible ? Snapshot(from.Item1) : null;
            if (before != null)
            {
                curtain.Bounds = ClientRectangle;
                curtain.Set(before, null, 0, 0);
                curtain.Visible = true;
                curtainUp = true;
                curtain.BringToFront();
                curtain.Update();
            }
            if (from.Item1 != null) from.Item1.Visible = false;
            page.Visible = true;
            shown = page;
            if (before == null) return;
            var after = Snapshot(page);
            if (after == null)
            {
                Finish();
                return;
            }
            direction = order > from.Item2 ? 1 : -1;
            curtain.Set(before, after, direction, 0);
            elapsed = 0;
            frames.Start();
        }

        Bitmap Snapshot(Control page)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w <= 0 || h <= 0) return null;
            try
            {
                using (var raw = new Bitmap(w, h))
                {
                    page.DrawToBitmap(raw, new Rectangle(0, 0, w, h));
                    var fast = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(fast)) g.DrawImageUnscaled(raw, 0, 0);
                    return fast;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        bool Step(float dt)
        {
            elapsed += dt;
            float u = Math.Min(1, elapsed / Seconds);
            float k = u < 0.5f ? 4 * u * u * u : 1 - (float)Math.Pow(-2 * u + 2, 3) / 2;
            curtain.Shift(direction * Height * k);
            if (u < 1) return true;
            Finish();
            return false;
        }

        void Finish()
        {
            frames.Stop();
            if (!curtainUp) return;
            curtainUp = false;
            curtain.Visible = false;
            curtain.Set(null, null, 0, 0);
            if (shown != null) shown.Bounds = ClientRectangle;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Finish();
            if (shown != null) shown.Bounds = ClientRectangle;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                frames.Dispose();
                curtain.Set(null, null, 0, 0);
            }
            base.Dispose(disposing);
        }

        sealed class Curtain : Control
        {
            Bitmap from, to;
            int direction, offset;

            public Curtain()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            }

            public void Set(Bitmap before, Bitmap after, int dir, float shift)
            {
                if (from != null && from != before) from.Dispose();
                if (to != null && to != after && to != before) to.Dispose();
                from = before;
                to = after;
                direction = dir;
                offset = (int)Math.Round(shift);
                Invalidate();
            }

            public void Shift(float shift)
            {
                int next = (int)Math.Round(shift);
                if (next == offset) return;
                offset = next;
                Invalidate();
                Update();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                if (from == null) { g.Clear(Parent?.BackColor ?? Theme.Background); return; }
                g.DrawImageUnscaled(from, 0, -offset);
                if (to != null) g.DrawImageUnscaled(to, 0, direction * Height - offset);
            }
        }
    }
}
