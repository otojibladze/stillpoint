using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stillpoint
{
    enum Eyes { Open, Happy, Closed, Squeeze, Dizzy }
    enum Mouth { Dot, Smile, O, Flat, Wavy }
    enum Bit { Puff, Spark, Heart, Note, Dot, Leaf, Rain, Splash, Snow, Petal, Autumn, Ripple }
    enum MiniMood { Yawn, Look, Happy }

    sealed class SpiritPose
    {
        public float Sx = 1, Sy = 1, Tilt, Dy, EyeOpen = 1, LookX, LookY, Yawn, Blush = 0.35f, Sprout = 1, Zzz, Night, Bloom, Snow, EyeScale = 1;
        public Eyes Eyes = Eyes.Open;
        public Mouth Mouth = Mouth.Dot;
        public bool Cushion, Bare;
        public int Leaves = 1, Stem;

        public SpiritPose Copy() => (SpiritPose)MemberwiseClone();
    }

    struct SpiritPoints
    {
        public PointF Tip, Head;
    }

    sealed class Particle
    {
        public Bit Kind;
        public float X, Y, Vx, Vy, Age, Life = 1, Rot, Vr, Size = 4, Orbit = float.NaN, Target, Night;
        public int Hue;
    }

    static class SpiritArt
    {
        public static readonly Color Moss = Hex(0x6F8F58);
        static readonly Color StoneLight = Hex(0xDCD4CA), StoneMid = Hex(0xADA398), StoneDark = Hex(0x776D63);
        static readonly Color Stem = Hex(0x56763F), LeafTop = Hex(0x86A96B), LeafTop2 = Hex(0x7A9E60), LeafLow = Hex(0x76985C), LeafLow2 = Hex(0x6A8C52);
        static readonly Color Petal = Hex(0xE58A72), PetalCenter = Hex(0xF0C35A), MouthInside = Hex(0x4A3530);
        static readonly Color[] AutumnHues = { Hex(0xD9823F), Hex(0xC4552F), Hex(0xE3B04B), Hex(0xB86B3A) };

        public static Color Hex(int rgb) => Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        public static Color Alpha(Color c, float a) => Color.FromArgb((int)Math.Round(Clamp(a, 0, 1) * 255), c.R, c.G, c.B);
        public static Color Mix(Color a, Color b, float k)
        {
            k = Clamp(k, 0, 1);
            return Color.FromArgb((int)(a.R + (b.R - a.R) * k), (int)(a.G + (b.G - a.G) * k), (int)(a.B + (b.B - a.B) * k));
        }
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float Smooth(float a, float b, float v) { float x = Clamp((v - a) / (b - a), 0, 1); return x * x * (3 - 2 * x); }
        public static float Sin(float x) => (float)Math.Sin(x);
        public static float Cos(float x) => (float)Math.Cos(x);
        public const float Pi = (float)Math.PI;
        public const float Deg = 180f / Pi;

        public static void FillEllipse(Graphics g, Color c, float cx, float cy, float rx, float ry)
        {
            if (rx <= 0 || ry <= 0 || c.A == 0) return;
            using (var b = new SolidBrush(c)) g.FillEllipse(b, cx - rx, cy - ry, rx * 2, ry * 2);
        }

        public static void FillRadial(Graphics g, float cx, float cy, float rx, float ry, Color center, Color edge, RectangleF? area = null)
        {
            if (rx <= 0.5f || ry <= 0.5f) return;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(cx - rx, cy - ry, rx * 2, ry * 2);
                using (var b = new PathGradientBrush(path) { CenterColor = center, SurroundColors = new[] { edge }, CenterPoint = new PointF(cx, cy) })
                {
                    if (area.HasValue) g.FillRectangle(b, area.Value);
                    else g.FillPath(b, path);
                }
            }
        }

        public static void FillPolygon(Graphics g, Color c, params PointF[] points)
        {
            using (var b = new SolidBrush(c)) g.FillPolygon(b, points);
        }

        public static void Arc(Graphics g, Pen pen, float cx, float cy, float r, float a0, float a1)
        {
            if (r <= 0) return;
            g.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, a0 * Deg, (a1 - a0) * Deg);
        }

        public static void Quad(GraphicsPath path, PointF p0, PointF q, PointF p1) =>
            path.AddBezier(p0,
                new PointF(p0.X + 2f / 3 * (q.X - p0.X), p0.Y + 2f / 3 * (q.Y - p0.Y)),
                new PointF(p1.X + 2f / 3 * (q.X - p1.X), p1.Y + 2f / 3 * (q.Y - p1.Y)), p1);

        public static Pen RoundPen(Color c, float width) =>
            new Pen(c, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

        static GraphicsPath Pebble(float rx, float ry)
        {
            var pts = new PointF[80];
            for (int i = 0; i < pts.Length; i++)
            {
                float a = i / 80f * Pi * 2, c = Cos(a), s = Sin(a);
                float y = s > 0 ? (float)Math.Pow(s, 0.72) * 0.94f : s;
                float x = Math.Sign(c) * (float)Math.Pow(Math.Abs(c), 0.88) + 0.05f * s;
                pts[i] = new PointF(x * rx, y * ry);
            }
            var path = new GraphicsPath();
            path.AddPolygon(pts);
            return path;
        }

        static void Leaf(Graphics g, Color color, PointF at, float angle, float size)
        {
            var state = g.Save();
            g.TranslateTransform(at.X, at.Y);
            g.RotateTransform(angle * Deg);
            using (var path = new GraphicsPath())
            using (var brush = new SolidBrush(color))
            {
                Quad(path, PointF.Empty, new PointF(size * 0.7f, -size * 0.6f), new PointF(0, -size * 1.9f));
                Quad(path, new PointF(0, -size * 1.9f), new PointF(-size * 0.7f, -size * 0.6f), PointF.Empty);
                g.FillPath(brush, path);
            }
            g.Restore(state);
        }

        public static SpiritPoints DrawSpirit(Graphics g, float cx, float ground, float R, SpiritPose p, float t, bool shadow = true)
        {
            float rx = R, ry = R * 0.84f, lift = -p.Dy;
            var outer = g.Save();
            if (shadow)
            {
                float shadowW = rx * 1.05f * p.Sx * (1 - Clamp(lift / 60, 0, 0.45f));
                FillRadial(g, cx, ground + 2, shadowW, R * 0.14f, Color.FromArgb((int)(255 * (0.32f - Clamp(lift / 200, 0, 0.15f))), 60, 45, 35), Color.FromArgb(0, 60, 45, 35));
            }

            if (p.Cushion)
            {
                FillEllipse(g, Hex(0x8E5A4B), cx, ground - 4, R * 1.05f, R * 0.15f);
                FillEllipse(g, Hex(0xA86F5D), cx, ground - 8, R * 0.98f, R * 0.12f);
                ground -= 10;
            }

            var baseMatrix = g.Transform;
            g.TranslateTransform(cx, ground + p.Dy);
            g.RotateTransform(p.Tilt * Deg);
            g.ScaleTransform(p.Sx, p.Sy);
            g.TranslateTransform(0, -ry * 0.94f);

            var points = new[] { new PointF(0, -ry * 1.2f), new PointF(-rx * 0.32f, -ry * 0.93f) };
            if (!p.Bare)
            {
                float sway = Sin(t * 2.1f) * 0.06f;
                float lean = 0.15f + (1 - p.Sprout) * 1.15f + sway;
                float bx = rx * 0.08f, by = -ry * 0.97f, len = R * (0.4f + 0.07f * p.Stem);
                var P0 = new PointF(bx, by);
                var P1 = new PointF(bx + Sin(lean) * len, by - Cos(lean) * len);
                var Q = new PointF(bx + Sin(lean * 0.4f) * len * 0.6f, by - Cos(lean * 0.4f) * len * 0.6f);
                Func<float, PointF> at = u => new PointF(
                    (1 - u) * (1 - u) * P0.X + 2 * (1 - u) * u * Q.X + u * u * P1.X,
                    (1 - u) * (1 - u) * P0.Y + 2 * (1 - u) * u * Q.Y + u * u * P1.Y);
                Func<float, float> dirAt = u =>
                {
                    float tx = 2 * (1 - u) * (Q.X - P0.X) + 2 * u * (P1.X - Q.X), ty = 2 * (1 - u) * (Q.Y - P0.Y) + 2 * u * (P1.Y - Q.Y);
                    return (float)Math.Atan2(tx, -ty);
                };
                using (var pen = RoundPen(Stem, R * 0.05f))
                using (var stem = new GraphicsPath())
                {
                    Quad(stem, P0, Q, P1);
                    g.DrawPath(pen, stem);
                }
                float[] spots = { 1f, 0.62f, 0.36f };
                for (int i = 0; i < Math.Min(spots.Length, p.Leaves); i++)
                {
                    var pt = at(spots[i]);
                    float ang = dirAt(spots[i]), size = R * (i == 0 ? 0.085f : 0.07f);
                    float open = i == 0 ? 0.75f : 1.05f + (1 - p.Sprout) * 0.4f;
                    Leaf(g, i == 0 ? LeafTop : LeafLow, pt, ang - open, size);
                    Leaf(g, i == 0 ? LeafTop2 : LeafLow2, pt, ang + open, size * 0.92f);
                }
                if (p.Bloom > 0)
                {
                    float k = p.Bloom, fx = P1.X, fy = P1.Y - R * 0.05f;
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Pi * 2 + t * 0.2f;
                        FillEllipse(g, Petal, fx + Cos(a) * R * 0.075f * k, fy + Sin(a) * R * 0.075f * k, R * 0.06f * k, R * 0.06f * k);
                    }
                    FillEllipse(g, PetalCenter, fx, fy, R * 0.045f * k, R * 0.045f * k);
                }
                points[0] = new PointF(P1.X, P1.Y - (p.Bloom > 0 ? R * 0.12f : 0));
            }
            using (var local = g.Transform)
            {
                baseMatrix.Invert();
                local.Multiply(baseMatrix, MatrixOrder.Append);
                local.TransformPoints(points);
            }
            baseMatrix.Dispose();

            using (var body = Pebble(rx, ry))
            {
                using (var shape = new GraphicsPath())
                {
                    float gr = R * 1.45f, gx = -rx * 0.05f, gy = -ry * 0.05f;
                    shape.AddEllipse(gx - gr, gy - gr, gr * 2, gr * 2);
                    using (var fill = new PathGradientBrush(shape) { CenterPoint = new PointF(-rx * 0.35f, -ry * 0.55f) })
                    {
                        fill.InterpolationColors = new ColorBlend { Colors = new[] { StoneDark, StoneMid, StoneLight }, Positions = new[] { 0f, 0.5f, 1f } };
                        g.FillPath(fill, body);
                    }
                }
                var clip = g.Save();
                g.SetClip(body, CombineMode.Intersect);
                using (var band = new GraphicsPath())
                using (var pen = new Pen(Color.FromArgb(77, 246, 240, 230), R * 0.05f))
                {
                    Quad(band, new PointF(rx * 0.35f, -ry * 1.1f), new PointF(rx * 0.75f, ry * 0.1f), new PointF(rx * 0.55f, ry * 1.1f));
                    g.DrawPath(pen, band);
                }
                using (var band = new GraphicsPath())
                using (var pen = new Pen(Color.FromArgb(46, 246, 240, 230), R * 0.018f))
                {
                    Quad(band, new PointF(rx * 0.5f, -ry * 1.1f), new PointF(rx * 0.9f, ry * 0.1f), new PointF(rx * 0.72f, ry * 1.1f));
                    g.DrawPath(pen, band);
                }
                foreach (var s in Specks)
                    FillEllipse(g, s.Light ? Color.FromArgb((int)(255 * Clamp(s.O + 0.15f, 0, 1)), 250, 245, 236) : Color.FromArgb((int)(255 * s.O), 60, 50, 44), s.X * rx, s.Y * ry - ry * 0.1f, s.S * R, s.S * R);
                var all = new RectangleF(-rx * 1.2f, -ry * 1.2f, rx * 2.4f, ry * 2.4f);
                FillRadial(g, 0, ry * 1.15f, rx * 1.15f, rx * 1.15f, Color.FromArgb(107, 45, 35, 28), Color.FromArgb(0, 45, 35, 28), all);
                var rimRect = new RectangleF(rx * 0.55f, -ry, rx * 0.6f, ry * 1.6f);
                using (var rim = new LinearGradientBrush(RectangleF.Inflate(rimRect, 1, 1), Color.FromArgb(0, 255, 236, 214), Color.FromArgb(89, 255, 236, 214), LinearGradientMode.Horizontal))
                    g.FillRectangle(rim, rimRect);
                var hl = g.Save();
                g.TranslateTransform(-rx * 0.4f, -ry * 0.55f);
                g.RotateTransform(-0.55f * Deg);
                FillEllipse(g, Color.FromArgb(77, 255, 251, 245), 0, 0, rx * 0.26f, ry * 0.12f);
                g.Restore(hl);
                if (p.Snow > 0)
                {
                    var shade = Alpha(Hex(0xC3CEDA), p.Snow);
                    var white = Alpha(Hex(0xF8FAFC), p.Snow);
                    FillEllipse(g, shade, -rx * 0.1f, -ry * 0.85f, rx * 0.55f, ry * 0.19f);
                    FillEllipse(g, shade, rx * 0.36f, -ry * 0.77f, rx * 0.21f, ry * 0.12f);
                    FillEllipse(g, white, -rx * 0.24f, -ry * 0.93f, rx * 0.42f, ry * 0.17f);
                    FillEllipse(g, white, rx * 0.12f, -ry * 0.95f, rx * 0.4f, ry * 0.15f);
                    FillEllipse(g, white, rx * 0.38f, -ry * 0.84f, rx * 0.19f, ry * 0.1f);
                    FillEllipse(g, Alpha(Color.White, p.Snow), -rx * 0.32f, -ry * 0.99f, rx * 0.2f, ry * 0.05f);
                }
                if (p.Night > 0)
                    using (var tint = new SolidBrush(Color.FromArgb((int)(255 * 0.38f * p.Night), 24, 28, 50))) g.FillRectangle(tint, all);
                g.Restore(clip);
            }

            float es = p.EyeScale;
            float ex = R * 0.33f, ey = -R * 0.04f, lx = p.LookX * R * 0.07f, ly = p.LookY * R * 0.05f;
            var blush = Color.FromArgb((int)(255 * Clamp(p.Blush, 0, 1)), 226, 132, 112);
            FillEllipse(g, blush, -R * 0.5f + lx * 0.4f, ey + R * 0.17f, R * 0.11f, R * 0.06f);
            FillEllipse(g, blush, R * 0.5f + lx * 0.4f, ey + R * 0.17f, R * 0.11f, R * 0.06f);

            using (var pen = RoundPen(Theme.Ink, R * 0.045f * es))
            {
                foreach (int s in new[] { -1, 1 })
                {
                    float x = s * ex + lx, y = ey + ly;
                    switch (p.Eyes)
                    {
                        case Eyes.Happy: Arc(g, pen, x, y + R * 0.05f * es, R * 0.075f * es, Pi * 1.15f, Pi * 1.85f); break;
                        case Eyes.Closed: Arc(g, pen, x, y - R * 0.03f * es, R * 0.07f * es, Pi * 0.18f, Pi * 0.82f); break;
                        case Eyes.Squeeze:
                            g.DrawLines(pen, new[] { new PointF(x - s * R * 0.07f * es, y - R * 0.06f * es), new PointF(x + s * R * 0.04f * es, y), new PointF(x - s * R * 0.07f * es, y + R * 0.06f * es) });
                            break;
                        case Eyes.Dizzy:
                            var spiral = new List<PointF>();
                            for (float a = 0; a < Pi * 4; a += 0.25f)
                            {
                                float rr = R * 0.012f * a * es, aa = a + t * 8 * s;
                                spiral.Add(new PointF(x + Cos(aa) * rr, y + Sin(aa) * rr));
                            }
                            using (var thin = RoundPen(Theme.Ink, R * 0.03f * es)) g.DrawLines(thin, spiral.ToArray());
                            break;
                        default:
                            float open = Clamp(p.EyeOpen, 0, 1);
                            if (open < 0.14f)
                            {
                                using (var lid = new GraphicsPath())
                                {
                                    Quad(lid, new PointF(x - R * 0.07f * es, y + R * 0.02f), new PointF(x, y + R * 0.06f * es), new PointF(x + R * 0.07f * es, y + R * 0.02f));
                                    g.DrawPath(pen, lid);
                                }
                                break;
                            }
                            float h = R * 0.1f * open * es, cy = y + R * 0.1f * es * (1 - open) * 0.5f;
                            FillEllipse(g, Theme.Ink, x, cy, R * 0.072f * es, h);
                            if (open > 0.5f && es <= 1.3f)
                            {
                                FillEllipse(g, Color.FromArgb(230, 255, 255, 255), x + R * 0.025f * es - lx * 0.15f, cy - h * 0.42f, R * 0.024f * es, R * 0.024f * es);
                                FillEllipse(g, Color.FromArgb(230, 255, 255, 255), x - R * 0.025f * es - lx * 0.15f, cy + h * 0.35f, R * 0.011f * es, R * 0.011f * es);
                            }
                            if (open < 0.9f)
                                using (var lidPen = RoundPen(Color.FromArgb(140, 60, 50, 44), R * 0.03f * es))
                                    g.DrawLine(lidPen, x - R * 0.085f * es, cy - h, x + R * 0.085f * es, cy - h);
                            break;
                    }
                }

                float my = R * 0.2f + ly * 0.6f, mx = lx * 0.6f;
                switch (p.Mouth)
                {
                    case Mouth.Smile: Arc(g, pen, mx, my - R * 0.06f, R * 0.09f, Pi * 0.2f, Pi * 0.8f); break;
                    case Mouth.O: FillEllipse(g, MouthInside, mx, my, R * (0.035f + 0.05f * p.Yawn), R * (0.03f + 0.08f * p.Yawn)); break;
                    case Mouth.Flat: g.DrawLine(pen, mx - R * 0.05f, my, mx + R * 0.05f, my); break;
                    case Mouth.Wavy:
                        var wave = new PointF[9];
                        for (int i = 0; i < wave.Length; i++) wave[i] = new PointF(mx - R * 0.1f + i * R * 0.025f, my + (i % 2 == 1 ? -1 : 1) * R * 0.02f);
                        using (var thin = RoundPen(Theme.Ink, R * 0.035f)) g.DrawLines(thin, wave);
                        break;
                    default: FillEllipse(g, Theme.Ink, mx, my, R * 0.03f * es, R * 0.022f * es); break;
                }
            }
            g.Restore(outer);

            if (p.Zzz > 0.05f)
            {
                var zColor = p.Night > 0.5f ? Hex(0xC9CCDA) : Theme.Muted;
                for (int i = 0; i < 3; i++)
                {
                    float ph = (t * 0.4f + i / 3f) % 1;
                    float s = (10 + ph * 9) * (R / 52) * 0.32f;
                    float zx = cx + R * 0.75f + ph * R * 0.4f, zy = ground - R * 1.5f - ph * R * 0.6f;
                    using (var pen = RoundPen(Alpha(zColor, Sin(ph * Pi) * p.Zzz), Math.Max(1f, s * 0.35f)))
                        g.DrawLines(pen, new[] { new PointF(zx - s, zy - s), new PointF(zx + s, zy - s), new PointF(zx - s, zy + s), new PointF(zx + s, zy + s) });
                }
            }
            return new SpiritPoints { Tip = points[0], Head = points[1] };
        }

        struct Speck { public float X, Y, S, O; public bool Light; }

        static readonly Speck[] Specks;

        static SpiritArt()
        {
            var rnd = new Random(7);
            Specks = new Speck[22];
            for (int i = 0; i < Specks.Length; i++)
            {
                float a = (float)rnd.NextDouble() * Pi * 2, r = (float)Math.Sqrt(rnd.NextDouble()) * 0.85f;
                Specks[i] = new Speck { X = Cos(a) * r, Y = Sin(a) * r * 0.85f, S = 0.008f + (float)rnd.NextDouble() * 0.02f, O = 0.08f + (float)rnd.NextDouble() * 0.2f, Light = rnd.NextDouble() < 0.3 };
            }
        }

        public static void DrawParticles(Graphics g, List<Particle> bits)
        {
            foreach (var q in bits)
            {
                float k = q.Age / q.Life;
                float a;
                switch (q.Kind)
                {
                    case Bit.Dot: a = Sin(k * Pi) * 0.8f; break;
                    case Bit.Leaf:
                    case Bit.Autumn:
                    case Bit.Petal: a = k < 0.8f ? 1 : (1 - k) * 5; break;
                    case Bit.Rain:
                    case Bit.Snow: a = Math.Min(1, (1 - k) * 4); break;
                    default: a = 1 - k; break;
                }
                a = Clamp(a, 0, 1);
                if (q.Kind == Bit.Rain)
                {
                    using (var pen = new Pen(Alpha(Mix(Hex(0x7D8796), Hex(0xAEB8CC), q.Night), a * 0.6f), 1.1f))
                        g.DrawLine(pen, q.X, q.Y, q.X - q.Vx * 0.03f, q.Y - q.Vy * 0.03f);
                    continue;
                }
                var state = g.Save();
                g.TranslateTransform(q.X, q.Y);
                g.RotateTransform(q.Rot * Deg);
                switch (q.Kind)
                {
                    case Bit.Puff: FillEllipse(g, Alpha(Hex(0x968A7D), a * 0.55f), 0, 0, q.Size * (1 + k * 1.5f), q.Size * (1 + k * 1.5f)); break;
                    case Bit.Spark:
                        var star = new PointF[8];
                        for (int i = 0; i < 8; i++) { float r = i % 2 == 1 ? q.Size * 0.32f : q.Size, an = i * Pi / 4; star[i] = new PointF(Cos(an) * r, Sin(an) * r); }
                        FillPolygon(g, Alpha(Hex(0xE3AE4F), a), star);
                        break;
                    case Bit.Heart:
                        using (var path = new GraphicsPath())
                        using (var b = new SolidBrush(Alpha(Hex(0xD9776A), a)))
                        {
                            float s = q.Size;
                            path.AddBezier(new PointF(0, s * 0.35f), new PointF(-s * 1.1f, -s * 0.35f), new PointF(-s * 0.5f, -s * 1.15f), new PointF(0, -s * 0.45f));
                            path.AddBezier(new PointF(0, -s * 0.45f), new PointF(s * 0.5f, -s * 1.15f), new PointF(s * 1.1f, -s * 0.35f), new PointF(0, s * 0.35f));
                            g.FillPath(b, path);
                        }
                        break;
                    case Bit.Note:
                        var c = Alpha(Hex(0x7A6F66), a);
                        float z = q.Size;
                        var head = g.Save();
                        g.RotateTransform(-20);
                        FillEllipse(g, c, 0, 0, z * 0.3f, z * 0.22f);
                        g.Restore(head);
                        using (var pen = RoundPen(c, Math.Max(1f, z * 0.1f)))
                            g.DrawLines(pen, new[] { new PointF(z * 0.27f, -z * 0.05f), new PointF(z * 0.27f, -z * 0.95f), new PointF(z * 0.6f, -z * 0.65f) });
                        break;
                    case Bit.Dot: FillEllipse(g, Alpha(Moss, a), 0, 0, q.Size, q.Size); break;
                    case Bit.Leaf: FillEllipse(g, Alpha(Hex(0x8AA56E), a), 0, 0, q.Size * 0.5f, q.Size); break;
                    case Bit.Autumn: FillEllipse(g, Alpha(AutumnHues[q.Hue % AutumnHues.Length], a), 0, 0, q.Size * 0.55f, q.Size); break;
                    case Bit.Petal: FillEllipse(g, Alpha(Hex(0xEFB3B8), a), 0, 0, q.Size * 0.6f, q.Size * 0.9f); break;
                    case Bit.Snow:
                        FillEllipse(g, Alpha(Hex(0xAEBBCB), a * 0.45f), 0, 0.3f, q.Size + 0.5f, q.Size + 0.5f);
                        FillEllipse(g, Alpha(Color.White, a), 0, 0, q.Size, q.Size);
                        break;
                    case Bit.Splash:
                        using (var pen = new Pen(Alpha(Mix(Hex(0x8C96A6), Hex(0xAEB8CC), q.Night), a * 0.7f), 0.9f))
                            Arc(g, pen, 0, 0, 1.5f + k * 3, Pi * 1.1f, Pi * 1.9f);
                        break;
                    case Bit.Ripple:
                        using (var pen = new Pen(Alpha(Hex(0xE8EEF2), a * 0.8f), 0.8f))
                            g.DrawEllipse(pen, -(2 + k * 7), -(0.6f + k * 1.8f), (2 + k * 7) * 2, (0.6f + k * 1.8f) * 2);
                        break;
                }
                g.Restore(state);
            }
        }

        public static void DrawButterfly(Graphics g, float x, float y, float t, bool landed)
        {
            float flap = landed ? 0.55f + 0.45f * Sin(t * 2.2f) : 0.25f + 0.75f * Math.Abs(Sin(t * 15));
            var state = g.Save();
            g.TranslateTransform(x, y);
            if (!landed) g.RotateTransform(Sin(t * 3) * 0.2f * Deg);
            foreach (int s in new[] { -1, 1 })
            {
                var wing = g.Save();
                g.ScaleTransform(s * Math.Max(0.05f, flap), 1);
                var w1 = g.Save(); g.TranslateTransform(5.5f, -3.5f); g.RotateTransform(-0.5f * Deg); FillEllipse(g, Hex(0xE39A58), 0, 0, 6, 5); g.Restore(w1);
                var w2 = g.Save(); g.TranslateTransform(4.2f, 3f); g.RotateTransform(0.4f * Deg); FillEllipse(g, Hex(0xC9714A), 0, 0, 3.8f, 3.2f); g.Restore(w2);
                FillEllipse(g, Color.FromArgb(230, 255, 246, 230), 7.5f, -4.5f, 1.2f, 1.2f);
                g.Restore(wing);
            }
            using (var body = RoundPen(Theme.Ink, 1.6f)) g.DrawLine(body, 0, -5, 0, 5);
            using (var antenna = new Pen(Theme.Ink, 0.8f))
            using (var path = new GraphicsPath())
            {
                Quad(path, new PointF(0, -5), new PointF(-2, -9), new PointF(-3.5f, -10));
                path.StartFigure();
                Quad(path, new PointF(0, -5), new PointF(2, -9), new PointF(3.5f, -10));
                g.DrawPath(antenna, path);
            }
            g.Restore(state);
        }

        public static void DrawBird(Graphics g, float x, float y, float t, float facing, bool flying)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(facing < 0 ? -1 : 1, 1);
            var brown = Hex(0x8A6A55);
            FillPolygon(g, Hex(0x6E5444), new PointF(-5, -3), new PointF(-12, -6.5f), new PointF(-11.5f, -2.5f));
            FillEllipse(g, brown, 0, -4, 7, 5);
            FillEllipse(g, Hex(0xD9774E), 2.6f, -3, 4, 3.6f);
            FillEllipse(g, brown, 5, -9, 3.7f, 3.5f);
            FillEllipse(g, Theme.Ink, 6.2f, -9.6f, 0.85f, 0.85f);
            FillPolygon(g, Hex(0xE0A040), new PointF(8.2f, -9.6f), new PointF(11.2f, -8.8f), new PointF(8.2f, -8.1f));
            float flap = flying ? Sin(t * 26) : 0.15f;
            var wing = g.Save();
            g.TranslateTransform(-1, -5.5f);
            g.RotateTransform((-0.35f - flap * 0.7f) * Deg);
            FillEllipse(g, Hex(0x6E5444), -1, 0, 6, 2.6f);
            g.Restore(wing);
            if (!flying)
                using (var legs = new Pen(Hex(0x5A4636), 0.8f))
                {
                    g.DrawLine(legs, -0.5f, 0.5f, -1, 3);
                    g.DrawLine(legs, 1.5f, 0.5f, 1.5f, 3);
                }
            g.Restore(state);
        }

        public static void DrawSnail(Graphics g, float x, float y, float t, float facing)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(facing < 0 ? -1 : 1, 1);
            float stretch = 1 + 0.06f * Sin(t * 2.4f);
            var foot = Hex(0xB8B193);
            FillEllipse(g, foot, 0, -1.4f, 10.5f * stretch, 2.4f);
            FillEllipse(g, foot, 8.6f * stretch, -3.6f, 2.6f, 3.1f);
            float wiggle = Sin(t * 3.1f) * 0.6f;
            using (var stalk = RoundPen(foot, 1.1f))
            {
                g.DrawLine(stalk, 9.2f * stretch, -5.5f, 10.8f * stretch + wiggle, -10.2f);
                g.DrawLine(stalk, 7.8f * stretch, -5.8f, 8.1f * stretch - wiggle, -10.6f);
            }
            FillEllipse(g, Theme.Ink, 10.8f * stretch + wiggle, -10.4f, 0.9f, 0.9f);
            FillEllipse(g, Theme.Ink, 8.1f * stretch - wiggle, -10.8f, 0.9f, 0.9f);
            FillRadial(g, -1.5f, -7, 6.4f, 6.4f, Hex(0xC08A5A), Hex(0x7E5232));
            using (var spiral = new Pen(Hex(0x6A4428), 0.9f))
            {
                Arc(g, spiral, -1.5f, -7, 4.2f, Pi * 0.9f, Pi * 2.6f);
                Arc(g, spiral, -1.0f, -7.3f, 2.2f, Pi * 1.4f, Pi * 3.1f);
            }
            g.Restore(state);
        }

        public static void DrawFrog(Graphics g, float x, float y, float t, float croak)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            var green = Hex(0x7FA060);
            FillEllipse(g, Hex(0x6C8E50), -6.2f, -1.2f, 3.2f, 1.8f);
            FillEllipse(g, Hex(0x6C8E50), 6.2f, -1.2f, 3.2f, 1.8f);
            FillEllipse(g, green, 0, -4.4f, 6.8f, 4.6f);
            FillEllipse(g, Hex(0xBACF92), 0, -3.2f, 4.2f, 2.8f);
            FillEllipse(g, green, -3.2f, -8.2f, 2.4f, 2.3f);
            FillEllipse(g, green, 3.2f, -8.2f, 2.4f, 2.3f);
            FillEllipse(g, Color.FromArgb(240, 250, 248, 236), -3.2f, -8.4f, 1.5f, 1.4f);
            FillEllipse(g, Color.FromArgb(240, 250, 248, 236), 3.2f, -8.4f, 1.5f, 1.4f);
            FillEllipse(g, Theme.Ink, -3.1f, -8.4f, 0.8f, 0.8f);
            FillEllipse(g, Theme.Ink, 3.3f, -8.4f, 0.8f, 0.8f);
            using (var mouth = new Pen(Hex(0x4D6B37), 0.7f)) Arc(g, mouth, 0, -6.4f, 2.6f, Pi * 0.25f, Pi * 0.75f);
            if (croak > 0.02f) FillEllipse(g, Alpha(Hex(0xE4ECC8), 0.9f), 0, -2.2f, 3.4f * croak, 2.6f * croak);
            g.Restore(state);
        }

        public static void DrawCat(Graphics g, float x, float y, float t)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            var fur = Hex(0x8C8691);
            float breath = 1 + 0.035f * Sin(t * 1.4f);
            using (var tail = RoundPen(Hex(0x807A86), 3.2f))
            using (var path = new GraphicsPath())
            {
                path.AddBezier(new PointF(14, -4), new PointF(20, 3), new PointF(-4, 4.5f), new PointF(-12, 0.5f));
                g.DrawPath(tail, path);
            }
            FillEllipse(g, fur, 1, -6.5f, 15.5f, 7.4f * breath);
            FillEllipse(g, Hex(0x9D97A2), 3, -9.5f, 9, 2.6f);
            FillPolygon(g, fur, new PointF(-15.5f, -12), new PointF(-14.2f, -19), new PointF(-10, -14));
            FillPolygon(g, fur, new PointF(-9.5f, -14.2f), new PointF(-6, -18.6f), new PointF(-5.2f, -12));
            FillPolygon(g, Hex(0xC9959A), new PointF(-14.4f, -13.2f), new PointF(-13.8f, -17), new PointF(-11.6f, -14));
            FillEllipse(g, fur, -11, -9, 6.2f, 5.6f);
            using (var eye = RoundPen(Hex(0x2E2A31), 0.8f))
            {
                Arc(g, eye, -13.4f, -10.2f, 1.4f, Pi * 0.15f, Pi * 0.85f);
                Arc(g, eye, -8.8f, -10.2f, 1.4f, Pi * 0.15f, Pi * 0.85f);
            }
            FillEllipse(g, Hex(0xC9959A), -11.1f, -7.6f, 0.9f, 0.6f);
            g.Restore(state);
        }
    }

    enum StoneMood { Awake, Sleepy, Asleep }

    static class StoneFaces
    {
        public static Bitmap Face(StoneMood mood, int size)
        {
            var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                var pose = new SpiritPose { Bare = true, Blush = 0.3f, EyeScale = size <= 20 ? 2.1f : size <= 32 ? 1.7f : size <= 64 ? 1.4f : 1.2f };
                if (mood == StoneMood.Sleepy) { pose.EyeOpen = 0.4f; pose.Mouth = Mouth.Flat; }
                else if (mood == StoneMood.Asleep) { pose.Eyes = Eyes.Closed; pose.Night = 0.35f; pose.Blush = 0.15f; }
                else if (size > 32) pose.Mouth = Mouth.Smile;
                SpiritArt.DrawSpirit(g, size / 2f, size * 0.93f, size * 0.47f, pose, 0, shadow: false);
            }
            return bitmap;
        }

        public static Icon Icon(StoneMood mood, int size)
        {
            using (var bitmap = Face(mood, size)) return Icons.FromBitmap(bitmap);
        }

        public static byte[] AppIco() => Icons.BuildIco(size => Face(StoneMood.Awake, size));

        public static Icon AppIcon() => new Icon(new System.IO.MemoryStream(AppIco()));
    }

    sealed class MiniSpirit : Control
    {
        readonly Stopwatch watch = Stopwatch.StartNew();
        readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer { Interval = 15 };

        public MiniSpirit()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            anim.Tick += (s, e) => Invalidate();
            anim.Start();
        }

        public MiniMood Mood { get; set; } = MiniMood.Yawn;
        public bool Snow { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float t = (float)watch.Elapsed.TotalSeconds;
            float size = Math.Min(Width, Height);
            g.TranslateTransform((Width - size) / 2, (Height - size) / 2);
            g.ScaleTransform(size / 76f, size / 76f);
            SpiritArt.DrawSpirit(g, 38, 66, 26, PoseAt(t), t);
        }

        SpiritPose PoseAt(float t)
        {
            var p = new SpiritPose { Snow = Snow ? 1 : 0 };
            switch (Mood)
            {
                case MiniMood.Look:
                    p.LookX = SpiritArt.Sin(t * 1.1f);
                    p.LookY = -0.2f;
                    p.Mouth = Mouth.O;
                    p.Yawn = 0.1f;
                    p.Tilt = SpiritArt.Sin(t * 1.1f) * 0.06f;
                    p.Sprout = 0.8f;
                    break;
                case MiniMood.Happy:
                    float hop = Math.Max(0, SpiritArt.Sin(t * 3.2f));
                    p.Dy = -6 * hop * hop;
                    p.Eyes = (t % 4) < 1.2f ? Eyes.Happy : Eyes.Open;
                    p.Mouth = Mouth.Smile;
                    p.Blush = 0.55f;
                    p.Leaves = 2;
                    break;
                default:
                    float k = SpiritArt.Sin((t % 3) / 3 * SpiritArt.Pi);
                    p.Sx = 1 + 0.03f * k; p.Sy = 0.95f + 0.08f * k; p.Tilt = -0.05f * k;
                    p.EyeOpen = 0.35f * (1 - k); p.Mouth = Mouth.O; p.Yawn = k; p.Blush = 0.2f; p.Sprout = 0.15f;
                    break;
            }
            float blink = t % 4.1f;
            if (blink < 0.16f && p.Eyes == Eyes.Open && Mood != MiniMood.Yawn) p.EyeOpen = Math.Abs(blink - 0.08f) / 0.08f;
            return p;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) anim.Dispose();
            base.Dispose(disposing);
        }
    }
}
