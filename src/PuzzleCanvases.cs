using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    abstract class PuzzleCanvas : Control
    {
        readonly Timer timer = new Timer { Interval = 30 };
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly string idle;
        string caption;
        double captionUntil;

        protected PuzzleCanvas(string idle)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.idle = idle;
            Size = new Size(284, 250);
            Anchor = AnchorStyles.None;
            Margin = new Padding(0);
            Font = Theme.Regular(9.5f);
            timer.Tick += (s, e) =>
            {
                Step();
                Invalidate();
            };
            timer.Start();
        }

        public bool Done { get; private set; }

        protected double Now => clock.Elapsed.TotalSeconds;

        protected float K => Width / 284f;

        protected void Succeed(string text)
        {
            Done = true;
            caption = text;
            captionUntil = double.MaxValue;
            Invalidate();
        }

        protected void Say(string text, double seconds = 2)
        {
            caption = text;
            captionUntil = Now + seconds;
            Invalidate();
        }

        protected virtual void Step()
        {
        }

        protected abstract void Render(Graphics g, RectangleF area);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Draw.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 10 * K))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var edge = new Pen(Theme.Track))
            {
                g.FillPath(fill, path);
                g.DrawPath(edge, path);
            }
            int captionHeight = Font.Height + (int)(14 * K);
            var area = new RectangleF(0, 0, Width, Height - captionHeight);
            var state = g.Save();
            g.SetClip(new RectangleF(1, 1, Width - 2, Height - 2));
            Render(g, area);
            g.Restore(state);
            string text = Now < captionUntil ? caption : idle;
            Draw.Text(g, text, Font, Done ? Theme.AccentText : Theme.Muted, new Rectangle(0, Height - captionHeight, Width, captionHeight - (int)(4 * K)), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        protected static void Fill(Graphics g, Color color, RectangleF r)
        {
            using (var brush = new SolidBrush(color)) g.FillEllipse(brush, r);
        }

        protected static RectangleF Around(PointF center, float width, float height) =>
            new RectangleF(center.X - width / 2, center.Y - height / 2, width, height);
    }

    sealed class BreathCanvas : PuzzleCanvas
    {
        const double Period = 6;
        const float Smallest = 0.38f;
        readonly bool biggest;
        float frozen = -1;

        public BreathCanvas(bool biggest)
            : base(biggest ? "Breathe in with it and click at the top" : "Breathe out with it and click at the bottom")
        {
            this.biggest = biggest;
            Cursor = Cursors.Hand;
        }

        public float CurrentScale => frozen >= 0 ? frozen : (float)(Smallest + (1 - Smallest) * (1 - Math.Cos(2 * Math.PI * (Now / Period + 0.25))) / 2);

        public bool OnTarget(float scale) => biggest ? scale >= 0.9f : scale <= 0.47f;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Done || e.Button != MouseButtons.Left) return;
            float scale = CurrentScale;
            if (OnTarget(scale))
            {
                frozen = scale;
                Succeed("Well breathed.");
            }
            else Say(biggest ? "Not yet. Wait until it is at its biggest." : "Not yet. Wait until it is at its smallest.");
        }

        protected override void Render(Graphics g, RectangleF area)
        {
            var center = new PointF(area.Width / 2, area.Height / 2 + 6 * K);
            float full = Math.Min(area.Width, area.Height) * 0.86f;
            using (var pen = new Pen(Theme.Border, 2 * K) { DashStyle = DashStyle.Dash })
            {
                g.DrawEllipse(pen, Around(center, full, full));
                if (!biggest) g.DrawEllipse(pen, Around(center, full * Smallest, full * Smallest));
            }
            float size = full * CurrentScale;
            Fill(g, Color.FromArgb(Done ? 255 : 215, Theme.Accent), Around(center, size, size));
        }
    }

    sealed class EnsoCanvas : PuzzleCanvas
    {
        readonly List<PointF> points = new List<PointF>();
        bool drawing;

        public EnsoCanvas()
            : base("Hold the mouse down and draw one circle")
        {
            Cursor = Cursors.Cross;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Done || e.Button != MouseButtons.Left) return;
            points.Clear();
            points.Add(e.Location);
            drawing = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!drawing) return;
            var last = points[points.Count - 1];
            if (Math.Abs(last.X - e.X) + Math.Abs(last.Y - e.Y) >= 3) points.Add(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!drawing) return;
            drawing = false;
            if (IsCircle(points, 22 * K, out string problem)) Succeed("A lovely circle.");
            else Say(problem, 2.5);
        }

        public static bool IsCircle(IReadOnlyList<PointF> stroke, float minRadius, out string problem)
        {
            problem = "Draw it in one go, all the way round.";
            if (stroke.Count < 12) return false;
            float cx = stroke.Average(p => p.X), cy = stroke.Average(p => p.Y);
            var radii = stroke.Select(p => Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy))).ToArray();
            double mean = radii.Average();
            if (mean < minRadius)
            {
                problem = "A bit bigger, please.";
                return false;
            }
            double spread = Math.Sqrt(radii.Average(r => (r - mean) * (r - mean))) / mean;
            if (spread > 0.28)
            {
                problem = "Not quite round. Try again.";
                return false;
            }
            double turned = 0;
            for (int i = 1; i < stroke.Count; i++)
            {
                double a = Math.Atan2(stroke[i - 1].Y - cy, stroke[i - 1].X - cx);
                double b = Math.Atan2(stroke[i].Y - cy, stroke[i].X - cx);
                double d = b - a;
                if (d > Math.PI) d -= 2 * Math.PI;
                if (d < -Math.PI) d += 2 * Math.PI;
                turned += d;
            }
            var first = stroke[0];
            var last = stroke[stroke.Count - 1];
            double gap = Math.Sqrt((first.X - last.X) * (first.X - last.X) + (first.Y - last.Y) * (first.Y - last.Y));
            if (Math.Abs(turned) < 2 * Math.PI * 0.88 || gap > mean * 0.9)
            {
                problem = "Close the circle, all the way round.";
                return false;
            }
            return true;
        }

        protected override void Render(Graphics g, RectangleF area)
        {
            if (points.Count < 2) return;
            using (var pen = new Pen(Theme.Ink, 9 * K) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, points.ToArray());
        }
    }

    sealed class CairnCanvas : PuzzleCanvas
    {
        static readonly Color[] StoneColors = { Color.FromArgb(143, 136, 130), Color.FromArgb(168, 162, 156), Color.FromArgb(134, 123, 117) };
        static readonly Color LooseColor = Color.FromArgb(193, 170, 155);
        readonly float shift;
        readonly bool startLeft;
        PointF? loose;
        PointF grab;
        bool dragging;

        public CairnCanvas(Random r)
            : base("Drag the loose stone onto the pile")
        {
            shift = r.Next(-24, 25);
            startLeft = r.Next(2) == 0;
            Cursor = Cursors.Hand;
        }

        SizeF LooseSize => new SizeF(66 * K, 26 * K);

        PointF Start => new PointF(startLeft ? 52 * K : Width - 52 * K, 32 * K);

        public PointF LoosePosition => loose ?? Start;

        public PointF Target
        {
            get
            {
                float x = Width / 2 + shift * K;
                float bottom = Height - (Font.Height + 14 * K) - 14 * K;
                float top = bottom - (40 + 34 + 30 - 8) * K;
                return new PointF(x, top - LooseSize.Height / 2 + 4 * K);
            }
        }

        public void DropAt(PointF center)
        {
            if (Done) return;
            var target = Target;
            if (Math.Abs(center.X - target.X) <= 20 * K && Math.Abs(center.Y - target.Y) <= 20 * K)
            {
                loose = target;
                Succeed("Balanced.");
            }
            else
            {
                loose = null;
                Say("It slid off. Try again.");
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Done || e.Button != MouseButtons.Left) return;
            var at = LoosePosition;
            var size = LooseSize;
            if (Math.Abs(e.X - at.X) > size.Width / 2 + 10 * K || Math.Abs(e.Y - at.Y) > size.Height / 2 + 12 * K) return;
            dragging = true;
            grab = new PointF(e.X - at.X, e.Y - at.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging) loose = new PointF(e.X - grab.X, e.Y - grab.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging) return;
            dragging = false;
            DropAt(LoosePosition);
        }

        protected override void Render(Graphics g, RectangleF area)
        {
            float x = Width / 2 + shift * K;
            float bottom = area.Bottom - 14 * K;
            float[] widths = { 150, 118, 88 };
            float[] heights = { 40, 34, 30 };
            float y = bottom;
            for (int i = 0; i < 3; i++)
            {
                float h = heights[i] * K;
                Fill(g, StoneColors[i], new RectangleF(x - widths[i] * K / 2, y - h, widths[i] * K, h));
                y -= h - 4 * K;
            }
            var size = LooseSize;
            if (!Done)
                using (var pen = new Pen(Theme.Accent, 2 * K) { DashStyle = DashStyle.Dash })
                    g.DrawEllipse(pen, Around(Target, size.Width, size.Height));
            var at = LoosePosition;
            if (!Done) Fill(g, Color.FromArgb(40, Theme.Ink), Around(new PointF(at.X, at.Y + 7 * K), size.Width * 0.9f, size.Height * 0.8f));
            Fill(g, LooseColor, Around(at, size.Width, size.Height));
        }
    }

    sealed class LotusCanvas : PuzzleCanvas
    {
        static readonly Color PetalLight = Color.FromArgb(232, 172, 168);
        static readonly Color PetalDark = Color.FromArgb(214, 146, 142);
        static readonly Color Pad = Color.FromArgb(140, 180, 146);
        const double OpenSeconds = 0.9;
        readonly Random random;
        double opensAt;

        public LotusCanvas(Random random)
            : base("Click it once it is fully open")
        {
            this.random = random;
            opensAt = Now + 3 + random.NextDouble() * 2.5;
            Cursor = Cursors.Hand;
        }

        public float Openness => (float)Math.Max(0, Math.Min(1, (Now - opensAt) / OpenSeconds));

        public void Tap()
        {
            if (Done) return;
            if (Openness >= 1)
            {
                Succeed("Beautiful. Thank you for waiting.");
                return;
            }
            opensAt = Now + 3 + random.NextDouble() * 2;
            Say("Too soon, it closed. Wait for it.", 2.4);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) Tap();
        }

        protected override void Render(Graphics g, RectangleF area)
        {
            float open = Openness;
            float ease = open * open * (3 - 2 * open);
            var root = new PointF(area.Width / 2, area.Bottom - 34 * K);
            Fill(g, Pad, Around(new PointF(root.X, root.Y + 6 * K), 176 * K, 32 * K));
            float w = 42 * K, h = 92 * K;
            Petal(g, root, -(16 + 48 * ease), w, h * 0.86f, PetalDark);
            Petal(g, root, 16 + 48 * ease, w, h * 0.86f, PetalDark);
            Petal(g, root, -(8 + 26 * ease), w, h * 0.96f, PetalDark);
            Petal(g, root, 8 + 26 * ease, w, h * 0.96f, PetalDark);
            Petal(g, root, 0, w, h, PetalLight);
            if (ease > 0.6f) Fill(g, Color.FromArgb((int)(255 * (ease - 0.6f) / 0.4f), Art.Sun), Around(new PointF(root.X, root.Y - h * 0.3f), 18 * K, 18 * K));
        }

        static void Petal(Graphics g, PointF root, float angle, float w, float h, Color color)
        {
            var state = g.Save();
            g.TranslateTransform(root.X, root.Y);
            g.RotateTransform(angle);
            Fill(g, color, new RectangleF(-w / 2, -h, w, h));
            g.Restore(state);
        }
    }

    sealed class TeaCanvas : PuzzleCanvas
    {
        static readonly Color TeaColor = Art.Tea;
        const double SecondsToFill = 2.6;
        public const float Band = 0.16f;
        bool holding;
        double lastStep;

        public TeaCanvas(Random r)
            : base("Hold the mouse down on the cup to pour")
        {
            Low = 0.42f + (float)r.NextDouble() * 0.2f;
            Cursor = Cursors.Hand;
        }

        public float Low { get; }
        public float Level { get; private set; }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Done || e.Button != MouseButtons.Left) return;
            holding = true;
            lastStep = Now;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Release();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Release();
        }

        protected override void Step()
        {
            if (!holding) return;
            double now = Now;
            Pour((float)((now - lastStep) / SecondsToFill));
            lastStep = now;
        }

        public void Pour(float amount)
        {
            if (Done) return;
            Level += amount;
            if (Level < 1) return;
            holding = false;
            Level = 0;
            Say("It overflowed. Pour again.");
        }

        public void Release()
        {
            if (!holding) return;
            holding = false;
            if (Level >= Low && Level <= Low + Band)
            {
                Succeed("A perfect cup.");
                return;
            }
            Say(Level < Low ? "A little more. Pour again." : "Too much. Pour again.");
            Level = 0;
        }

        protected override void Render(Graphics g, RectangleF area)
        {
            float w = 130 * K, h = 112 * K, d = 92 * K;
            float x = area.Width / 2 - w / 2 - 14 * K, y = area.Height / 2 - h / 2 + 8 * K;
            using (var outline = new GraphicsPath())
            {
                outline.AddLine(x, y, x, y + h - d / 2);
                outline.AddArc(x, y + h - d, d, d, 180, -90);
                outline.AddLine(x + d / 2, y + h, x + w - d / 2, y + h);
                outline.AddArc(x + w - d, y + h - d, d, d, 90, -90);
                outline.AddLine(x + w, y + h - d / 2, x + w, y);
                if (Level > 0)
                {
                    using (var inside = (GraphicsPath)outline.Clone())
                    {
                        inside.CloseFigure();
                        var state = g.Save();
                        g.SetClip(inside);
                        using (var tea = new SolidBrush(TeaColor)) g.FillRectangle(tea, x, y + h * (1 - Level), w, h * Level + 2);
                        g.Restore(state);
                    }
                }
                using (var pen = new Pen(Theme.Ink, 3 * K)) g.DrawPath(pen, outline);
            }
            using (var pen = new Pen(Theme.Ink, 3 * K)) g.DrawArc(pen, x + w - 6 * K, y + 18 * K, 36 * K, 46 * K, -90, 180);
            if (Done) return;
            using (var pen = new Pen(Theme.Accent, 2 * K) { DashStyle = DashStyle.Dash })
            {
                float top = y + h * (1 - Low - Band), low = y + h * (1 - Low);
                g.DrawLine(pen, x - 10 * K, top, x + w + 10 * K, top);
                g.DrawLine(pen, x - 10 * K, low, x + w + 10 * K, low);
            }
        }
    }
}
