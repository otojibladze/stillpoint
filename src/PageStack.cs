using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class PageStack : Panel
    {
        const double Seconds = 0.38;
        readonly Dictionary<string, (Control Page, int Order)> pages = new Dictionary<string, (Control, int)>();
        readonly Timer tick = new Timer { Interval = 10 };
        readonly Stopwatch clock = new Stopwatch();
        Control shown, leaving;
        int direction;

        public PageStack()
        {
            DoubleBuffered = true;
            tick.Tick += (s, e) => Step();
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
            if (from.Item1 == null || !IsHandleCreated || !Visible)
            {
                if (from.Item1 != null) from.Item1.Visible = false;
                page.Visible = true;
                shown = page;
                return;
            }
            leaving = from.Item1;
            shown = page;
            direction = order > from.Item2 ? 1 : -1;
            page.Top = direction * Height;
            page.Visible = true;
            clock.Restart();
            tick.Start();
        }

        void Step()
        {
            double u = Math.Min(1, clock.Elapsed.TotalSeconds / Seconds);
            double k = u < 0.5 ? 4 * u * u * u : 1 - Math.Pow(-2 * u + 2, 3) / 2;
            int offset = (int)Math.Round(direction * Height * k);
            leaving.Top = -offset;
            shown.Top = direction * Height - offset;
            if (u >= 1) Finish();
        }

        void Finish()
        {
            if (!tick.Enabled) return;
            tick.Stop();
            leaving.Visible = false;
            leaving.Bounds = ClientRectangle;
            shown.Bounds = ClientRectangle;
            leaving = null;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Finish();
            if (shown != null) shown.Bounds = ClientRectangle;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) tick.Dispose();
            base.Dispose(disposing);
        }
    }
}
