using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Stillpoint
{
    enum Thing { Tree, House, Mushroom, Candle, Cup, Mountain }

    static class Art
    {
        public static readonly Color Leaf = Color.FromArgb(96, 150, 108);
        public static readonly Color Bark = Color.FromArgb(140, 100, 72);
        public static readonly Color Wall = Color.FromArgb(176, 168, 160);
        public static readonly Color Cream = Color.FromArgb(238, 228, 208);
        public static readonly Color Flame = Color.FromArgb(232, 150, 56);
        public static readonly Color Tea = Color.FromArgb(172, 112, 62);
        public static readonly Color Ceramic = Color.FromArgb(104, 136, 170);
        public static readonly Color Rock = Color.FromArgb(118, 128, 148);
        public static readonly Color RockLight = Color.FromArgb(152, 162, 180);
        public static readonly Color Snow = Color.FromArgb(248, 248, 246);
        public static readonly Color Sun = Color.FromArgb(240, 186, 72);
        public static readonly Color Moon = Color.FromArgb(238, 216, 150);
        public static readonly Color Night = Color.FromArgb(38, 44, 68);
        public static readonly Color Sky = Color.FromArgb(206, 222, 236);
        public static readonly Color Grass = Color.FromArgb(132, 172, 128);
        public static readonly Color Soil = Color.FromArgb(150, 112, 84);
        public static readonly Color Petal = Color.FromArgb(222, 132, 140);
        public static readonly Color Stone = Color.FromArgb(150, 144, 138);

        public static readonly string[] ThingNames = { "tree", "house", "mushroom", "candle", "cup", "mountain" };

        public static Action<Graphics, RectangleF> Of(Thing thing, bool shadow) => (g, b) => DrawThing(g, thing, b, shadow);

        public static void DrawThing(Graphics g, Thing thing, RectangleF b, bool shadow)
        {
            Color C(Color c) => shadow ? Theme.Ink : c;
            switch (thing)
            {
                case Thing.Tree:
                    Rect(g, C(Bark), b, 0.44f, 0.55f, 0.12f, 0.43f);
                    Ellipse(g, C(Leaf), b, 0.18f, 0.02f, 0.64f, 0.62f);
                    break;
                case Thing.House:
                    Rect(g, C(Wall), b, 0.2f, 0.46f, 0.6f, 0.5f);
                    Poly(g, C(Theme.Accent), b, 0.06f, 0.5f, 0.5f, 0.08f, 0.94f, 0.5f);
                    Rect(g, C(Theme.Ink), b, 0.44f, 0.68f, 0.13f, 0.28f);
                    break;
                case Thing.Mushroom:
                    Rect(g, C(Cream), b, 0.38f, 0.42f, 0.24f, 0.54f);
                    Pie(g, C(Theme.Accent), Box(b, 0.06f, 0.1f, 0.88f, 0.7f), 180, 180);
                    if (!shadow)
                    {
                        Ellipse(g, Cream, b, 0.3f, 0.24f, 0.1f, 0.08f);
                        Ellipse(g, Cream, b, 0.56f, 0.18f, 0.12f, 0.09f);
                    }
                    break;
                case Thing.Candle:
                    Rect(g, C(Cream), b, 0.34f, 0.4f, 0.32f, 0.56f);
                    Rect(g, C(Theme.Ink), b, 0.49f, 0.32f, 0.02f, 0.09f);
                    Ellipse(g, C(Flame), b, 0.42f, 0.06f, 0.16f, 0.27f);
                    break;
                case Thing.Cup:
                    Pie(g, C(Ceramic), Box(b, 0.1f, 0.06f, 0.62f, 0.8f), 0, 180);
                    using (var pen = new Pen(C(Ceramic), b.Width * 0.07f)) g.DrawArc(pen, Box(b, 0.6f, 0.47f, 0.22f, 0.22f), -90, 180);
                    Rect(g, C(Ceramic), b, 0.04f, 0.88f, 0.74f, 0.06f);
                    break;
                default:
                    Poly(g, C(Rock), b, 0.02f, 0.94f, 0.42f, 0.12f, 0.82f, 0.94f);
                    Poly(g, C(RockLight), b, 0.4f, 0.94f, 0.69f, 0.4f, 0.98f, 0.94f);
                    Poly(g, C(Snow), b, 0.32f, 0.32f, 0.42f, 0.12f, 0.52f, 0.32f);
                    break;
            }
        }

        public static void MoonPhase(Graphics g, RectangleF b, int stage)
        {
            var disc = Box(b, 0.15f, 0.15f, 0.7f, 0.7f);
            Ellipse(g, Moon, disc);
            if (stage >= 2) return;
            var state = g.Save();
            using (var clip = new GraphicsPath())
            {
                clip.AddEllipse(disc);
                g.SetClip(clip);
                using (var shade = new SolidBrush(Night))
                {
                    if (stage == 0) g.FillEllipse(shade, disc.X - disc.Width * 0.22f, disc.Y, disc.Width, disc.Height);
                    else g.FillRectangle(shade, disc.X - 1, disc.Y - 1, disc.Width / 2 + 1, disc.Height + 2);
                }
            }
            g.Restore(state);
        }

        public static void SunHeight(Graphics g, RectangleF b, int stage)
        {
            float y = new[] { 0.62f, 0.36f, 0.08f }[stage];
            Ellipse(g, Sun, b, 0.32f, y, 0.36f, 0.36f);
            Rect(g, Grass, b, -0.2f, 0.82f, 1.4f, 0.36f);
        }

        public static void PlantStage(Graphics g, RectangleF b, int stage)
        {
            Pie(g, Soil, Box(b, 0.08f, 0.8f, 0.84f, 0.3f), 180, 180);
            if (stage == 0)
            {
                Ellipse(g, Bark, b, 0.42f, 0.7f, 0.16f, 0.11f);
                return;
            }
            float top = stage == 1 ? 0.48f : 0.3f;
            using (var pen = new Pen(Leaf, b.Width * 0.05f)) g.DrawLine(pen, P(b, 0.5f, 0.82f), P(b, 0.5f, top));
            Ellipse(g, Leaf, b, 0.28f, 0.56f, 0.22f, 0.1f);
            Ellipse(g, Leaf, b, 0.5f, 0.48f, 0.22f, 0.1f);
            if (stage < 2) return;
            for (int i = 0; i < 5; i++)
            {
                double a = (-90 + i * 72) * Math.PI / 180;
                float cx = 0.5f + 0.11f * (float)Math.Cos(a), cy = 0.22f + 0.11f * (float)Math.Sin(a);
                Ellipse(g, Petal, b, cx - 0.09f, cy - 0.09f, 0.18f, 0.18f);
            }
            Ellipse(g, Sun, b, 0.43f, 0.15f, 0.14f, 0.14f);
        }

        public static void CupLevel(Graphics g, RectangleF b, int stage)
        {
            float level = new[] { 0f, 0.45f, 0.92f }[stage];
            var bowl = Box(b, 0.08f, -0.05f, 0.66f, 0.9f);
            using (var path = new GraphicsPath())
            {
                path.AddPie(bowl.X, bowl.Y, bowl.Width, bowl.Height, 0, 180);
                if (level > 0)
                {
                    var state = g.Save();
                    g.SetClip(path);
                    float bottom = bowl.Y + bowl.Height, height = bowl.Height / 2;
                    using (var tea = new SolidBrush(Tea)) g.FillRectangle(tea, bowl.X, bottom - height * level, bowl.Width, height * level);
                    g.Restore(state);
                }
                using (var pen = new Pen(Theme.Ink, Math.Max(2f, b.Width * 0.04f))) g.DrawPath(pen, path);
            }
            using (var pen = new Pen(Theme.Ink, Math.Max(2f, b.Width * 0.04f))) g.DrawArc(pen, Box(b, 0.66f, 0.46f, 0.2f, 0.2f), -90, 180);
        }

        public static void StoneSize(Graphics g, RectangleF b, int stage)
        {
            float w = new[] { 0.32f, 0.56f, 0.86f }[stage];
            float h = w * 0.5f;
            Ellipse(g, Stone, b, 0.5f - w / 2, 0.9f - h, w, h);
        }

        static RectangleF Box(RectangleF b, float x, float y, float w, float h) =>
            new RectangleF(b.X + b.Width * x, b.Y + b.Height * y, b.Width * w, b.Height * h);

        static PointF P(RectangleF b, float x, float y) => new PointF(b.X + b.Width * x, b.Y + b.Height * y);

        static void Rect(Graphics g, Color c, RectangleF b, float x, float y, float w, float h)
        {
            using (var brush = new SolidBrush(c)) g.FillRectangle(brush, Box(b, x, y, w, h));
        }

        static void Ellipse(Graphics g, Color c, RectangleF b, float x, float y, float w, float h) => Ellipse(g, c, Box(b, x, y, w, h));

        static void Ellipse(Graphics g, Color c, RectangleF r)
        {
            using (var brush = new SolidBrush(c)) g.FillEllipse(brush, r);
        }

        static void Pie(Graphics g, Color c, RectangleF r, float start, float sweep)
        {
            using (var brush = new SolidBrush(c)) g.FillPie(brush, r.X, r.Y, r.Width, r.Height, start, sweep);
        }

        static void Poly(Graphics g, Color c, RectangleF b, params float[] xy)
        {
            var points = new PointF[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = P(b, xy[i * 2], xy[i * 2 + 1]);
            using (var brush = new SolidBrush(c)) g.FillPolygon(brush, points);
        }
    }
}
