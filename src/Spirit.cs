using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Stillpoint
{
    enum Eyes { Open, Happy, Closed, Squeeze, Dizzy }
    enum Mouth { Dot, Smile, O, Flat, Wavy }
    enum Bit { Puff, Spark, Heart, Note, Dot, Leaf }

    sealed class SpiritPose
    {
        public float Sx = 1, Sy = 1, Tilt, Dy, EyeOpen = 1, LookX, LookY, Yawn, Blush = 0.35f, Sprout = 1, Zzz, Night, Bloom;
        public Eyes Eyes = Eyes.Open;
        public Mouth Mouth = Mouth.Dot;
        public bool Cushion;
        public int Leaves = 1, Stem;
    }

    sealed class Particle
    {
        public Bit Kind;
        public float X, Y, Vx, Vy, Age, Life = 1, Rot, Vr, Size = 4, Orbit = float.NaN;
    }

    static class SpiritArt
    {
        public static readonly Color Moss = Hex(0x6F8F58);
        static readonly Color StoneLight = Hex(0xDCD4CA), StoneMid = Hex(0xADA398), StoneDark = Hex(0x776D63);
        static readonly Color Stem = Hex(0x56763F), LeafTop = Hex(0x86A96B), LeafTop2 = Hex(0x7A9E60), LeafLow = Hex(0x76985C), LeafLow2 = Hex(0x6A8C52);
        static readonly Color Petal = Hex(0xE58A72), PetalCenter = Hex(0xF0C35A), MouthInside = Hex(0x4A3530);

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
        const float Pi = (float)Math.PI;
        const float Deg = 180f / Pi;

        static void FillEllipse(Graphics g, Color c, float cx, float cy, float rx, float ry)
        {
            if (rx <= 0 || ry <= 0) return;
            using (var b = new SolidBrush(c)) g.FillEllipse(b, cx - rx, cy - ry, rx * 2, ry * 2);
        }

        static void FillRadial(Graphics g, float cx, float cy, float rx, float ry, Color center, Color edge, RectangleF? area = null)
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

        static void Arc(Graphics g, Pen pen, float cx, float cy, float r, float a0, float a1)
        {
            if (r <= 0) return;
            g.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, a0 * Deg, (a1 - a0) * Deg);
        }

        static void Quad(GraphicsPath path, PointF p0, PointF q, PointF p1) =>
            path.AddBezier(p0,
                new PointF(p0.X + 2f / 3 * (q.X - p0.X), p0.Y + 2f / 3 * (q.Y - p0.Y)),
                new PointF(p1.X + 2f / 3 * (q.X - p1.X), p1.Y + 2f / 3 * (q.Y - p1.Y)), p1);

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

        public static PointF DrawSpirit(Graphics g, float cx, float ground, float R, SpiritPose p, float t)
        {
            float rx = R, ry = R * 0.84f, lift = -p.Dy;
            var outer = g.Save();
            float shadowW = rx * 1.05f * p.Sx * (1 - Clamp(lift / 60, 0, 0.45f));
            FillRadial(g, cx, ground + 2, shadowW, R * 0.14f, Color.FromArgb((int)(255 * (0.32f - Clamp(lift / 200, 0, 0.15f))), 60, 45, 35), Color.FromArgb(0, 60, 45, 35));

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
            using (var pen = new Pen(Stem, R * 0.05f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
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
            var tip = new[] { new PointF(P1.X, P1.Y - (p.Bloom > 0 ? R * 0.12f : 0)) };
            using (var local = g.Transform)
            {
                baseMatrix.Invert();
                local.Multiply(baseMatrix, MatrixOrder.Append);
                local.TransformPoints(tip);
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
                if (p.Night > 0)
                    using (var tint = new SolidBrush(Color.FromArgb((int)(255 * 0.38f * p.Night), 24, 28, 50))) g.FillRectangle(tint, all);
                g.Restore(clip);
            }

            float ex = R * 0.33f, ey = -R * 0.04f, lx = p.LookX * R * 0.07f, ly = p.LookY * R * 0.05f;
            var blush = Color.FromArgb((int)(255 * Clamp(p.Blush, 0, 1)), 226, 132, 112);
            FillEllipse(g, blush, -R * 0.5f + lx * 0.4f, ey + R * 0.17f, R * 0.11f, R * 0.06f);
            FillEllipse(g, blush, R * 0.5f + lx * 0.4f, ey + R * 0.17f, R * 0.11f, R * 0.06f);

            using (var pen = new Pen(Theme.Ink, R * 0.045f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                foreach (int s in new[] { -1, 1 })
                {
                    float x = s * ex + lx, y = ey + ly;
                    switch (p.Eyes)
                    {
                        case Eyes.Happy: Arc(g, pen, x, y + R * 0.05f, R * 0.075f, Pi * 1.15f, Pi * 1.85f); break;
                        case Eyes.Closed: Arc(g, pen, x, y - R * 0.03f, R * 0.07f, Pi * 0.18f, Pi * 0.82f); break;
                        case Eyes.Squeeze:
                            g.DrawLines(pen, new[] { new PointF(x - s * R * 0.07f, y - R * 0.06f), new PointF(x + s * R * 0.04f, y), new PointF(x - s * R * 0.07f, y + R * 0.06f) });
                            break;
                        case Eyes.Dizzy:
                            var spiral = new List<PointF>();
                            for (float a = 0; a < Pi * 4; a += 0.25f)
                            {
                                float rr = R * 0.012f * a, aa = a + t * 8 * s;
                                spiral.Add(new PointF(x + Cos(aa) * rr, y + Sin(aa) * rr));
                            }
                            using (var thin = new Pen(Theme.Ink, R * 0.03f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLines(thin, spiral.ToArray());
                            break;
                        default:
                            float open = Clamp(p.EyeOpen, 0, 1);
                            if (open < 0.14f)
                            {
                                using (var lid = new GraphicsPath())
                                {
                                    Quad(lid, new PointF(x - R * 0.07f, y + R * 0.02f), new PointF(x, y + R * 0.06f), new PointF(x + R * 0.07f, y + R * 0.02f));
                                    g.DrawPath(pen, lid);
                                }
                                break;
                            }
                            float h = R * 0.1f * open, cy = y + R * 0.1f * (1 - open) * 0.5f;
                            FillEllipse(g, Theme.Ink, x, cy, R * 0.072f, h);
                            if (open > 0.5f)
                            {
                                FillEllipse(g, Color.FromArgb(230, 255, 255, 255), x + R * 0.025f - lx * 0.15f, cy - h * 0.42f, R * 0.024f, R * 0.024f);
                                FillEllipse(g, Color.FromArgb(230, 255, 255, 255), x - R * 0.025f - lx * 0.15f, cy + h * 0.35f, R * 0.011f, R * 0.011f);
                            }
                            if (open < 0.9f)
                                using (var lidPen = new Pen(Color.FromArgb(140, 60, 50, 44), R * 0.03f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                                    g.DrawLine(lidPen, x - R * 0.085f, cy - h, x + R * 0.085f, cy - h);
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
                        using (var thin = new Pen(Theme.Ink, R * 0.035f) { LineJoin = LineJoin.Round }) g.DrawLines(thin, wave);
                        break;
                    default: FillEllipse(g, Theme.Ink, mx, my, R * 0.03f, R * 0.022f); break;
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
                    using (var pen = new Pen(Alpha(zColor, Sin(ph * Pi) * p.Zzz), Math.Max(1f, s * 0.35f)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLines(pen, new[] { new PointF(zx - s, zy - s), new PointF(zx + s, zy - s), new PointF(zx - s, zy + s), new PointF(zx + s, zy + s) });
                }
            }
            return tip[0];
        }

        struct Speck { public float X, Y, S, O; public bool Light; }
        struct Star { public float X, Y, S, Ph; }

        static readonly Speck[] Specks;
        static readonly Star[] Stars;
        static readonly float[] FlyPhase, FlySpeed;
        static readonly float[][] Garden = { new[] { 62f, 207f, 7f }, new[] { 74f, 212f, 4.5f }, new[] { 238f, 205f, 5.5f } };

        static SpiritArt()
        {
            var rnd = new Random(7);
            Specks = new Speck[22];
            for (int i = 0; i < Specks.Length; i++)
            {
                float a = (float)rnd.NextDouble() * Pi * 2, r = (float)Math.Sqrt(rnd.NextDouble()) * 0.85f;
                Specks[i] = new Speck { X = Cos(a) * r, Y = Sin(a) * r * 0.85f, S = 0.008f + (float)rnd.NextDouble() * 0.02f, O = 0.08f + (float)rnd.NextDouble() * 0.2f, Light = rnd.NextDouble() < 0.3 };
            }
            var stars = new List<Star>();
            for (int i = 0; i < 18; i++)
            {
                float x = 40 + (float)rnd.NextDouble() * 220, y = 24 + (float)rnd.NextDouble() * 120;
                if (Math.Sqrt((x - 150) * (x - 150) + (y - 150) * (y - 150)) < 128)
                    stars.Add(new Star { X = x, Y = y, S = 0.6f + (float)rnd.NextDouble() * 1.1f, Ph = (float)rnd.NextDouble() * 6 });
            }
            Stars = stars.ToArray();
            FlyPhase = new float[5];
            FlySpeed = new float[5];
            for (int i = 0; i < 5; i++) { FlyPhase[i] = i * 1.3f + (float)rnd.NextDouble(); FlySpeed[i] = 0.6f + (float)rnd.NextDouble() * 0.5f; }
        }

        public static void DrawStars(Graphics g, float t, float night)
        {
            if (night <= 0.01f) return;
            foreach (var s in Stars) FillEllipse(g, Alpha(Hex(0xF0ECDC), night * (0.45f + 0.45f * Sin(t * 1.6f + s.Ph))), s.X, s.Y, s.S, s.S);
        }

        public static void DrawScene(Graphics g, float night, int rakeLines, float newestProgress, float stoneX, float stoneGround)
        {
            float n = night;
            using (var floor = new SolidBrush(Mix(Hex(0xEDE6DD), Hex(0x3A3E52), n))) g.FillRectangle(floor, 0, 0, 300, 300);
            var skyRect = new RectangleF(0, 0, 300, 181);
            using (var sky = new LinearGradientBrush(skyRect, Mix(Hex(0xF4EFE8), Hex(0x1F2433), n), Mix(Hex(0xEDE6DD), Hex(0x3A3E52), n), LinearGradientMode.Vertical))
                g.FillRectangle(sky, 0, 0, 300, 180);

            if (n < 0.99f) FillRadial(g, 92, 64, 90, 90, Color.FromArgb((int)(140 * (1 - n)), 250, 214, 170), Color.FromArgb(0, 250, 214, 170));
            if (n > 0.01f)
            {
                FillEllipse(g, Alpha(Hex(0xF4ECD6), n), 206, 66, 13, 13);
                FillEllipse(g, Mix(Hex(0xF4EFE8), Hex(0x232838), n), 212, 61, 11.5f, 11.5f);
            }

            const float top = 174;
            using (var groundPath = new GraphicsPath())
            {
                groundPath.AddEllipse(150 - 220, top, 440, 260);
                using (var sand = new SolidBrush(Mix(Hex(0xE2D8C9), Hex(0x45475A), n))) g.FillPath(sand, groundPath);
                var state = g.Save();
                g.SetClip(groundPath, CombineMode.Intersect);
                using (var dark = new Pen(n > 0.5f ? Color.FromArgb(89, 20, 22, 34) : Color.FromArgb(82, 140, 120, 96), 1.4f))
                using (var light = new Pen(n > 0.5f ? Color.FromArgb(64, 120, 124, 150) : Color.FromArgb(140, 255, 250, 240), 1f))
                {
                    for (int i = 0; i < rakeLines; i++)
                    {
                        float rx = 74 + i * 15, ry = rx * 0.2f;
                        float prog = i == rakeLines - 1 ? newestProgress : 1;
                        if (prog <= 0) continue;
                        float sweep = 360 * Math.Min(1, prog);
                        g.DrawArc(dark, stoneX - rx, stoneGround - 2 - ry, rx * 2, ry * 2, 90, sweep);
                        g.DrawArc(light, stoneX - rx - 2, stoneGround - 1 - ry - 0.6f, rx * 2 + 4, ry * 2 + 1.2f, 90, sweep);
                    }
                }
                g.Restore(state);
            }
            foreach (var s in Garden)
            {
                float x = s[0], y = s[1], r = s[2];
                FillEllipse(g, Color.FromArgb(46, 60, 45, 35), x, y + r * 0.55f, r * 1.1f, r * 0.3f);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(x - r, y - r * 0.72f, r * 2, r * 1.44f);
                    using (var b = new PathGradientBrush(path) { CenterPoint = new PointF(x - r * 0.4f, y - r * 0.4f), CenterColor = Mix(Hex(0xA9A097), Hex(0x5A5D6E), n), SurroundColors = new[] { Mix(Hex(0x6E655D), Hex(0x33364A), n) } })
                        g.FillPath(b, path);
                }
            }
        }

        public static void DrawFireflies(Graphics g, float t, float night)
        {
            if (night < 0.02f) return;
            for (int i = 0; i < FlyPhase.Length; i++)
            {
                float ph = FlyPhase[i], sp = FlySpeed[i];
                float x = 150 + Cos(t * 0.35f * sp + ph) * 95 + Sin(t * 0.9f * sp + ph) * 12;
                float y = 120 + Sin(t * 0.5f * sp + ph * 2) * 50;
                float a = night * (0.35f + 0.65f * Math.Max(0, Sin(t * 2.4f * sp + ph * 3)));
                FillRadial(g, x, y, 9, 9, Alpha(Hex(0xF0E28C), a), Color.FromArgb(0, 240, 226, 140));
                FillEllipse(g, Alpha(Hex(0xFFF8C8), a), x, y, 1.6f, 1.6f);
            }
        }

        public static void DrawParticles(Graphics g, List<Particle> bits)
        {
            foreach (var q in bits)
            {
                float k = q.Age / q.Life;
                float a = q.Kind == Bit.Dot ? Sin(k * Pi) * 0.8f : q.Kind == Bit.Leaf ? (k < 0.8f ? 1 : (1 - k) * 5) : 1 - k;
                a = Clamp(a, 0, 1);
                var state = g.Save();
                g.TranslateTransform(q.X, q.Y);
                g.RotateTransform(q.Rot * Deg);
                switch (q.Kind)
                {
                    case Bit.Puff: FillEllipse(g, Alpha(Hex(0x968A7D), a * 0.55f), 0, 0, q.Size * (1 + k * 1.5f), q.Size * (1 + k * 1.5f)); break;
                    case Bit.Spark:
                        var star = new PointF[8];
                        for (int i = 0; i < 8; i++) { float r = i % 2 == 1 ? q.Size * 0.32f : q.Size, an = i * Pi / 4; star[i] = new PointF(Cos(an) * r, Sin(an) * r); }
                        using (var b = new SolidBrush(Alpha(Hex(0xE3AE4F), a))) g.FillPolygon(b, star);
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
                        using (var pen = new Pen(c, Math.Max(1f, z * 0.1f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            g.DrawLines(pen, new[] { new PointF(z * 0.27f, -z * 0.05f), new PointF(z * 0.27f, -z * 0.95f), new PointF(z * 0.6f, -z * 0.65f) });
                        break;
                    case Bit.Dot: FillEllipse(g, Alpha(Moss, a), 0, 0, q.Size, q.Size); break;
                    case Bit.Leaf: FillEllipse(g, Alpha(Hex(0x8AA56E), a), 0, 0, q.Size * 0.5f, q.Size); break;
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
            using (var body = new Pen(Theme.Ink, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(body, 0, -5, 0, 5);
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
    }

    sealed class SpiritRing : Control
    {
        const float SX = 150, Ground = 194, R = 56, EyeY = 146;
        static readonly string[] ThoughtsFree = { "Do clouds ever sit down?", "I've sat here for 3000 years. You don't have to.", "Water. Have you had some today?", "I like this sand.", "Roll your shoulders. I'll wait.", "Nothing to do. Perfect." };
        static readonly string[] ThoughtsSitting = { "Blink a few times. Screens are dry.", "Still here. Still sitting.", "Unclench your jaw.", "How's your back?" };

        readonly Random rng = new Random();
        readonly Stopwatch watch = Stopwatch.StartNew();
        readonly System.Windows.Forms.Timer wake = new System.Windows.Forms.Timer { Interval = 250 };
        readonly List<Particle> bits = new List<Particle>();
        readonly List<float> pokes = new List<float>();
        SpiritPose pose;
        float last;

        bool known, running, paused, hasLast;
        float drowsy, progress;
        string time = "0:00:00", caption = "";
        int breaks = -1;
        DateTime breaksDate, lastEnd;

        float stretchT = -99, pokeT = -99, dizzyT = -99, wakeT = -99, yawnT = -99, rakeT = -99, bloomT = -99, breatheT = -1, nextAct = 8, bfAt = -1, petSaidT = -99, heartT, noteT, humT = -99;
        bool sparked = true, landed = true, petting;
        float rub, nightK = -1;
        PointF tip = new PointF(SX, 90);
        PointF pointer;
        bool pointerIn;
        float pointerT = -99;

        string bubble, pending;
        float bubbleT0, bubbleEnd, pendingAt;
        bool bubbleThought;

        int bfState;
        float bfT0, bfX, bfY, bfSide = 1;
        PointF bfFrom, bfTo;

        Font timeFont, captionFont, bubbleFont;
        int fontKey;

        public SpiritRing()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            wake.Tick += (s, e) => { };
            wake.Start();
            Application.Idle += OnIdle;
        }

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
        public event EventHandler BreathingChanged;
        public bool Breathing => breatheT >= 0;
        public float Now => (float)watch.Elapsed.TotalSeconds;

        bool NightHour(int from) { int h = Clock().Hour; return h >= from || h < 6; }
        bool Sleeping => paused || (!running && NightHour(22));

        public void SetSession(bool running, bool paused, float drowsy, float progress, string time, string caption, int breaksToday, DateTime lastEnd)
        {
            float t = Now;
            var today = Clock().Date;
            if (breaksDate != today || breaks < 0)
            {
                breaks = breaksToday;
                breaksDate = today;
            }
            else if (breaksToday > breaks)
            {
                rakeT = t + Math.Max(0, 1.9f - (t - stretchT));
                if (breaks < 4 && breaksToday >= 4)
                {
                    bloomT = rakeT + 0.8f;
                    Later("I grew a flower!", rakeT + 1f);
                }
                breaks = breaksToday;
            }

            if (known && running != this.running)
            {
                if (running) Started(t);
                else Stopped(t);
            }
            known = true;
            this.running = running;
            this.paused = paused;
            this.drowsy = drowsy;
            this.progress = progress;
            this.time = time;
            this.caption = caption;
            hasLast = lastEnd != default(DateTime);
            if (!running) this.lastEnd = lastEnd;
            if (nightK < 0) nightK = Sleeping || NightHour(21) ? 1 : 0;
            Invalidate();
        }

        void Started(float t)
        {
            if (lastEnd != default(DateTime))
            {
                var gap = Clock() - lastEnd;
                if (gap.TotalMinutes < 1) Say("Back already?");
                else if (gap.TotalHours < 1) Say($"Welcome back. {(int)Math.Round(gap.TotalMinutes)} min break.", 3.2f);
                else if (gap.TotalHours < 10) Say($"Welcome back. {Fmt.Hm(gap.TotalSeconds)} break.", 3.2f);
                else Say("Good to see you again.", 3.2f);
            }
            else Say("Let's sit. I'll keep count.");
            wakeT = t;
            humT = -99;
            bfAt = -1;
            Shoo(t);
        }

        void Stopped(float t)
        {
            stretchT = t;
            sparked = landed = false;
            humT = -99;
            bfAt = t + 5;
            Later(Pick(new[] { "Ahh. That's better.", "Good stretch.", "Mmm, legs again." }), t + 1.9f);
        }

        public void Reminder()
        {
            yawnT = Now;
            Emit(new Particle { Kind = Bit.Leaf, X = tip.X, Y = tip.Y, Vx = 8, Vy = 4, Vr = 2, Life = 4, Size = 6 });
        }

        public void ToggleBreathing()
        {
            float t = Now;
            if (Breathing) { breatheT = -1; Say("We can try later."); }
            else { breatheT = t; humT = -99; Say("Follow me. In through the nose.", 3f); }
            BreathingChanged?.Invoke(this, EventArgs.Empty);
        }

        string Pick(string[] list) => list[rng.Next(list.Length)];

        void Say(string text, float seconds = 2.6f, bool thought = false)
        {
            float t = Now;
            bubble = text;
            bubbleThought = thought;
            bubbleT0 = t;
            bubbleEnd = t + seconds;
        }

        void Later(string text, float at)
        {
            pending = text;
            pendingAt = at;
        }

        void Emit(Particle p)
        {
            if (bits.Count < 140) bits.Add(p);
        }

        float Drowsiness => running ? SpiritArt.Clamp(drowsy, 0, 1.2f) : 0;

        bool Animating
        {
            get
            {
                if (IsDisposed || !IsHandleCreated || !Visible) return false;
                var form = FindForm();
                return form != null && form.Visible && form.WindowState != FormWindowState.Minimized;
            }
        }

        IntPtr frameTimer;
        double nextFrame;

        void OnIdle(object sender, EventArgs e)
        {
            while (Animating && !PeekMessage(out _, IntPtr.Zero, 0, 0, 0))
            {
                float t = Now;
                float dt = Math.Min(0.1f, t - last);
                last = t;
                Step(t, dt);
                Refresh();
                double period = FindForm()?.ContainsFocus == true ? 1 / 60.0 : 1 / 30.0;
                double now = watch.Elapsed.TotalSeconds;
                nextFrame = now - nextFrame > period ? now + period : nextFrame + period;
                WaitUntil(nextFrame);
            }
        }

        void WaitUntil(double target)
        {
            double wait = target - watch.Elapsed.TotalSeconds;
            if (wait <= 0) return;
            if (frameTimer == IntPtr.Zero) frameTimer = CreateWaitableTimerExW(IntPtr.Zero, null, 2, 0x1F0003);
            long due = -(long)(wait * 10000000);
            if (frameTimer != IntPtr.Zero && SetWaitableTimer(frameTimer, ref due, 0, IntPtr.Zero, IntPtr.Zero, false))
                WaitForSingleObject(frameTimer, 100);
            else
                System.Threading.Thread.Sleep(Math.Max(1, (int)(wait * 1000)));
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern IntPtr CreateWaitableTimerExW(IntPtr attributes, string name, uint flags, uint access);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool SetWaitableTimer(IntPtr timer, ref long dueTime, int period, IntPtr completion, IntPtr arg, bool resume);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public Point Location;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);


        public void Step(float t, float dt)
        {
            if (nightK < 0) nightK = 0;
            bool nightSky = Sleeping || NightHour(21);
            nightK += ((nightSky ? 1 : 0) - nightK) * (1 - (float)Math.Exp(-dt * 2.5));
            if (pending != null && t >= pendingAt) { Say(pending, 3f); pending = null; }

            rub *= (float)Math.Exp(-dt * 1.6);
            bool was = petting;
            petting = rub > (was ? 70 : 150);
            if (petting && !Sleeping)
            {
                if (!was && t - petSaidT > 6) { petSaidT = t; Say(Pick(new[] { "Mmm. That's nice.", "Purr... wait, I'm a rock." }), 2.2f); }
                if (running) wakeT = t;
                if (t > heartT)
                {
                    heartT = t + 0.38f;
                    Emit(new Particle { Kind = Bit.Heart, X = SX + ((float)rng.NextDouble() - 0.5f) * 50, Y = 108, Vx = ((float)rng.NextDouble() - 0.5f) * 12, Vy = -32, Vr = ((float)rng.NextDouble() - 0.5f) * 0.6f, Life = 1.4f, Size = 5 + (float)rng.NextDouble() * 2 });
                }
            }

            if (Breathing && !Sleeping)
            {
                float c = (t - breatheT) % 12;
                if (rng.NextDouble() < dt * 7)
                {
                    float a = (float)rng.NextDouble() * (float)Math.PI * 2, ca = SpiritArt.Cos(a), sa = SpiritArt.Sin(a);
                    if (c < 4) Emit(new Particle { Kind = Bit.Dot, X = SX + ca * 118, Y = 148 + sa * 100, Vx = -ca * 30, Vy = -sa * 26, Life = 2, Size = 1.8f });
                    else if (c >= 8) Emit(new Particle { Kind = Bit.Dot, X = SX + ca * 52, Y = 148 + sa * 44, Vx = ca * 32, Vy = sa * 28, Life = 2, Size = 1.8f });
                }
            }

            bool calm = !Breathing && t - humT > 3.8f && !petting && bfState == 0 && t - stretchT > 3 && t - pokeT > 2 && t - dizzyT > 3;
            Schedule(t, calm);
            StepButterfly(t);
            StepBits(dt, t);
            pose = Pose(t);
        }

        void Schedule(float t, bool calm)
        {
            if (Sleeping) return;
            if (bfAt > 0 && t > bfAt && calm && !running) { bfAt = -1; StartButterfly(t); nextAct = t + 14; return; }
            if (t < nextAct || !calm) return;
            nextAct = t + 11 + (float)rng.NextDouble() * 8;
            if (running && Drowsiness > 0.45f) return;
            double r = rng.NextDouble();
            if (!running && hasLast && r < 0.35) StartButterfly(t);
            else if (r < 0.65) humT = t;
            else Say(Pick(running ? ThoughtsSitting : ThoughtsFree), 3.4f, thought: true);
        }

        void StartButterfly(float t)
        {
            if (bfState != 0) return;
            bfSide = rng.NextDouble() < 0.5 ? -1 : 1;
            bfState = 1;
            bfT0 = t;
            bfFrom = new PointF(SX + bfSide * 180, 50 + (float)rng.NextDouble() * 50);
        }

        void Shoo(float t)
        {
            if (bfState != 1 && bfState != 2) return;
            bfState = 3;
            bfT0 = t;
            bfFrom = new PointF(bfX, bfY);
            bfTo = new PointF(SX - bfSide * 190, -10);
        }

        void StepButterfly(float t)
        {
            if (bfState == 1)
            {
                float u = SpiritArt.Clamp((t - bfT0) / 3.4f, 0, 1), e = SpiritArt.Smooth(0, 1, u);
                bfX = bfFrom.X + (tip.X - bfFrom.X) * e + SpiritArt.Sin(u * 13) * 16 * (1 - u);
                bfY = bfFrom.Y + (tip.Y - 5 - bfFrom.Y) * e + SpiritArt.Cos(u * 9) * 12 * (1 - u);
                if (u >= 1) { bfState = 2; bfT0 = t; Say(Pick(new[] { "Oh. Hello there.", "A visitor!", "Shh. Don't move." }), 2.4f); }
            }
            else if (bfState == 2)
            {
                bfX = tip.X;
                bfY = tip.Y - 5;
                if (t - bfT0 > 4.5f || running || Sleeping) { bfState = 3; bfT0 = t; bfFrom = new PointF(bfX, bfY); bfTo = new PointF(SX - bfSide * 190, -10); }
            }
            else if (bfState == 3)
            {
                float u = SpiritArt.Clamp((t - bfT0) / 3, 0, 1), e = u * u;
                bfX = bfFrom.X + (bfTo.X - bfFrom.X) * e + SpiritArt.Sin(u * 11) * 14;
                bfY = bfFrom.Y + (bfTo.Y - bfFrom.Y) * e + SpiritArt.Cos(u * 8) * 8;
                if (u >= 1) bfState = 0;
            }
        }

        void StepBits(float dt, float t)
        {
            for (int i = bits.Count - 1; i >= 0; i--)
            {
                var q = bits[i];
                q.Age += dt;
                if (q.Age >= q.Life) { bits.RemoveAt(i); continue; }
                if (!float.IsNaN(q.Orbit))
                {
                    float a = q.Orbit + t * 3;
                    q.X = SX + SpiritArt.Cos(a) * 42;
                    q.Y = 92 + SpiritArt.Sin(a) * 9;
                }
                else
                {
                    q.X += q.Vx * dt;
                    q.Y += q.Vy * dt;
                }
                q.Rot += q.Vr * dt;
                if (q.Kind == Bit.Leaf)
                {
                    q.Vy = Math.Min(q.Vy + 18 * dt, 26);
                    q.X += SpiritArt.Sin(q.Age * 2.6f) * 22 * dt;
                    if (q.Y > Ground - 2) { q.Vy = 0; q.Vx = 0; q.Vr = 0; q.Y = Ground - 2; }
                }
                else if (q.Kind == Bit.Puff) { q.Vx *= 0.94f; q.Vy *= 0.94f; }
            }
        }

        SpiritPose BasePose(float t)
        {
            var p = new SpiritPose
            {
                Night = Math.Max(0, nightK),
                Leaves = Math.Min(3, 1 + Math.Max(0, breaks)),
                Stem = Math.Min(4, Math.Max(0, breaks)),
                Bloom = breaks >= 4 ? SpiritArt.Clamp((t - bloomT) / 1.5f, 0, 1) : 0,
            };
            float b = SpiritArt.Sin(t * 1.7f) * 0.014f;
            p.Sy += b;
            p.Sx -= b * 0.5f;
            float blink = t % 3.9f;
            if (blink < 0.16f) p.EyeOpen = Math.Abs(blink - 0.08f) / 0.08f;

            bool target = false;
            float tx = 0, ty = 0;
            if (bfState != 0) { tx = bfX; ty = bfY; target = true; }
            else if (pointerIn && t - pointerT < 4) { tx = pointer.X; ty = pointer.Y; target = true; }
            if (target)
            {
                p.LookX = SpiritArt.Clamp((tx - SX) / 70, -1, 1);
                p.LookY = SpiritArt.Clamp((ty - EyeY) / 70, -1, 1);
            }
            else
            {
                float glance = SpiritArt.Sin(t * 0.23f);
                p.LookX = glance > 0.55f ? 1 : glance < -0.6f ? -1 : 0;
            }

            if (Sleeping)
            {
                p.Cushion = true; p.Eyes = Eyes.Closed; p.Zzz = 1; p.Sprout = 0.35f; p.Blush = 0.2f; p.LookX = 0; p.LookY = 0;
                float s = SpiritArt.Sin(t * 0.9f) * 0.03f;
                p.Sy = 1 + s;
                p.Sx = 1 - s * 0.4f;
                return p;
            }

            if (Breathing)
            {
                float bt = t - breatheT;
                if (bt >= 60)
                {
                    breatheT = -1;
                    pokeT = t;
                    Say("Nice. Five slow breaths.");
                    BreathingChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    float c = bt % 12;
                    float k = c < 4 ? SpiritArt.Smooth(0, 4, c) : c < 8 ? 1 : 1 - SpiritArt.Smooth(8, 12, c);
                    p.Sx = 1 + 0.09f * k; p.Sy = 1 + 0.12f * k;
                    p.Eyes = Eyes.Closed; p.LookX = 0; p.LookY = 0; p.Sprout = 0.6f + 0.4f * k; p.Blush = 0.3f + 0.15f * k;
                    return p;
                }
            }

            if (running)
            {
                float d = Drowsiness;
                float dd = SpiritArt.Smooth(0.2f, 1, d);
                float wake = SpiritArt.Clamp(1 - (t - wakeT) / 4, 0, 1);
                dd *= 1 - wake * 0.85f;
                p.EyeOpen *= 1 - 0.62f * dd;
                p.Sy -= 0.07f * dd; p.Sx += 0.05f * dd;
                p.Tilt = 0.07f * dd * SpiritArt.Sin(t * 0.4f);
                p.Sprout = 1 - 0.85f * dd;
                p.Blush = 0.35f - 0.2f * dd;
                p.LookX *= 1 - dd;
                p.LookY = p.LookY * (1 - dd) + 0.5f * dd;
                if (dd > 0.5f) p.Mouth = Mouth.Flat;
                if (d > 0.75f && wake == 0)
                {
                    float u = (t % 7) / 7;
                    if (u > 0.45f && u < 0.86f)
                    {
                        float k = SpiritArt.Smooth(0.45f, 0.8f, u);
                        p.EyeOpen *= 1 - k; p.Dy += 4 * k; p.Tilt += 0.12f * k; p.Zzz = k;
                    }
                    else if (u >= 0.86f && u < 0.93f)
                    {
                        float k = SpiritArt.Sin((u - 0.86f) / 0.07f * (float)Math.PI);
                        p.EyeOpen = 1; p.Dy -= 6 * k; p.Mouth = Mouth.O; p.Yawn = 0.3f; p.LookY = 0;
                    }
                }
                float yt = t - yawnT;
                if (dd > 0.35f && (t % 19 < 2.2f || yt < 2.2f))
                {
                    float ph = yt < 2.2f ? yt : t % 19;
                    float k = SpiritArt.Sin(ph / 2.2f * (float)Math.PI);
                    p.Mouth = Mouth.O; p.Yawn = k; p.EyeOpen *= 1 - k; p.Sy += 0.05f * k; p.Tilt -= 0.05f * k;
                }
            }
            else if (hasLast)
            {
                p.Mouth = Mouth.Smile;
                p.Blush = 0.45f;
                float since = t - stretchT;
                if (since < 8) p.Dy = -Math.Abs(SpiritArt.Sin(t * 4.2f)) * 4 * (1 - since / 8);
            }

            if (t - humT < 3.8f)
            {
                p.Eyes = Eyes.Happy; p.Mouth = Mouth.O; p.Yawn = 0.08f + 0.06f * SpiritArt.Sin(t * 9); p.Tilt += SpiritArt.Sin(t * 3.2f) * 0.08f; p.LookX = 0; p.LookY = 0;
                if (t > noteT)
                {
                    noteT = t + 0.55f;
                    Emit(new Particle { Kind = Bit.Note, X = SX + 34, Y = 112, Vx = 10 + (float)rng.NextDouble() * 10, Vy = -26, Vr = 0.3f, Life = 1.8f, Size = 12 });
                }
            }
            if (bfState == 2) { p.Mouth = Mouth.O; p.Yawn = 0.15f; p.Blush = 0.55f; }
            return p;
        }

        SpiritPose Pose(float t)
        {
            var p = BasePose(t);
            float st = t - stretchT;
            if (st >= 0 && st < 1.9f)
            {
                float s = st / 1.9f;
                if (s < 0.6f)
                {
                    float k = SpiritArt.Sin(s / 0.6f * (float)Math.PI);
                    p.Sy = 1 + 0.22f * k; p.Sx = 1 - 0.12f * k; p.Eyes = Eyes.Squeeze; p.Mouth = Mouth.O; p.Yawn = 0.7f * k; p.Sprout = 1; p.LookX = 0; p.LookY = 0;
                    if (!sparked && s > 0.28f)
                    {
                        sparked = true;
                        for (int i = 0; i < 8; i++)
                        {
                            float a = -(float)Math.PI / 2 + (i - 3.5f) * 0.35f;
                            Emit(new Particle { Kind = Bit.Spark, X = SX, Y = 110, Vx = SpiritArt.Cos(a) * 70, Vy = SpiritArt.Sin(a) * 70, Vr = 4, Life = 0.9f, Size = 4 + (float)rng.NextDouble() * 3 });
                        }
                    }
                }
                else
                {
                    float h = SpiritArt.Sin((s - 0.6f) / 0.4f * (float)Math.PI);
                    p.Dy = -26 * h; p.Sy = 1 + 0.06f * h; p.Sx = 1 - 0.03f * h; p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Sprout = 1;
                    if (!landed && s > 0.96f)
                    {
                        landed = true;
                        for (int i = 0; i < 6; i++)
                        {
                            int side = i % 2 == 1 ? 1 : -1;
                            Emit(new Particle { Kind = Bit.Puff, X = SX + side * (30 + (float)rng.NextDouble() * 14), Y = Ground - 3, Vx = side * (30 + (float)rng.NextDouble() * 30), Vy = -6 - (float)rng.NextDouble() * 8, Life = 0.7f, Size = 3 + (float)rng.NextDouble() * 2 });
                        }
                    }
                }
            }
            if (petting && !Sleeping)
            {
                p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Blush = 0.75f; p.Sy -= 0.03f; p.Sx += 0.02f;
                p.Tilt += SpiritArt.Clamp((pointer.X - SX) / 300, -0.12f, 0.12f);
            }
            float pt = t - pokeT;
            if (pt >= 0 && pt < 1)
            {
                float w = (float)Math.Exp(-pt * 4) * SpiritArt.Sin(pt * 26);
                p.Tilt += 0.22f * w; p.Sx += 0.06f * w; p.Sy -= 0.04f * w;
                if (!Sleeping) { p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Blush = 0.6f; }
                else if (pt < 0.5f) { p.Eyes = Eyes.Open; p.EyeOpen = 0.3f; }
            }
            float zt = t - dizzyT;
            if (zt >= 0 && zt < 2.8f)
            {
                float f = 1 - zt / 2.8f;
                p.Eyes = Eyes.Dizzy; p.Mouth = Mouth.Wavy; p.Tilt += 0.16f * SpiritArt.Sin(t * 7) * f; p.Sx += 0.03f * SpiritArt.Sin(t * 9) * f;
            }
            return p;
        }

        float SceneScale => Math.Min(Width, Height) / 300f;
        PointF Origin => new PointF((Width - Math.Min(Width, Height)) / 2, (Height - Math.Min(Width, Height)) / 2);

        PointF ToScene(Point p)
        {
            var o = Origin;
            float s = Math.Max(0.01f, SceneScale);
            return new PointF((p.X - o.X) / s, (p.Y - o.Y) / s);
        }

        static bool OnStone(PointF q) =>
            Math.Sqrt(((q.X - SX) / 1.1) * ((q.X - SX) / 1.1) + (q.Y - (Ground - R * 0.8)) * (q.Y - (Ground - R * 0.8))) < R * 1.05;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var q = ToScene(e.Location);
            if (pointerIn && OnStone(q) && OnStone(pointer))
            {
                float d = (float)Math.Sqrt((q.X - pointer.X) * (q.X - pointer.X) + (q.Y - pointer.Y) * (q.Y - pointer.Y));
                if (d < 40) rub += d;
            }
            pointer = q;
            pointerIn = true;
            pointerT = Now;
            Cursor = OnStone(q) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            pointerIn = false;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || !OnStone(ToScene(e.Location))) return;
            Poke(Now);
        }

        public void Poke(float t)
        {
            if (Sleeping) { pokeT = t; Say("Mm... five more minutes."); return; }
            pokes.RemoveAll(p => t - p >= 2);
            pokes.Add(t);
            if (pokes.Count >= 5)
            {
                pokes.Clear();
                dizzyT = t;
                for (int i = 0; i < 5; i++) Emit(new Particle { Kind = Bit.Spark, Vr = 3, Life = 2.6f, Size = 5, Orbit = i * 1.26f });
                Say("Whoa. The room is spinning.");
                return;
            }
            pokeT = t;
            if (running && Drowsiness > 0.5f) { wakeT = t; Say(Pick(new[] { "Huh? I'm awake. I'm awake.", "Was I snoring?" })); }
            else Say(Pick(new[] { "Hehe.", "That tickles.", "Hi!", "Boop." }), 1.6f);
        }

        void EnsureFonts(float size)
        {
            int key = (int)size * 1000 + DeviceDpi;
            if (key == fontKey && timeFont != null) return;
            fontKey = key;
            timeFont?.Dispose();
            captionFont?.Dispose();
            bubbleFont?.Dispose();
            float pt = 72f / Math.Max(1, DeviceDpi);
            timeFont = Theme.Light(Math.Max(6f, size * 0.1f * pt));
            captionFont = Theme.SemiBold(Math.Max(5f, size * 0.033f * pt));
            bubbleFont = Theme.Regular(Math.Max(6f, size * 0.046f * pt));
        }

        Bitmap sceneCache;
        long sceneKey = -1;

        Bitmap SceneImage(int px, float s, float night, int lines, float newest)
        {
            long key = ((((long)px * 64 + (int)Math.Round(night * 48)) * 16 + lines) * 64) + (int)Math.Round(newest * 60);
            if (sceneCache != null && key == sceneKey) return sceneCache;
            if (sceneCache == null || sceneCache.Width != px)
            {
                sceneCache?.Dispose();
                sceneCache = new Bitmap(px, px, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }
            sceneKey = key;
            using (var g = Graphics.FromImage(sceneCache))
            using (var disc = new GraphicsPath())
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(s, s);
                disc.AddEllipse(150 - 140, 150 - 140, 280, 280);
                g.SetClip(disc, CombineMode.Intersect);
                SpiritArt.DrawScene(g, night, lines, newest, SX, Ground);
            }
            return sceneCache;
        }

        protected override void OnPaint(PaintEventArgs e) => Render(e.Graphics, Now);

        public void Render(Graphics g, float t)
        {
            g.Clear(Parent?.BackColor ?? Theme.Background);
            if (pose == null) pose = Pose(t);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height), s = size / 300f;
            if (size < 20) return;
            var o = Origin;
            var state = g.Save();
            g.TranslateTransform(o.X, o.Y);
            g.ScaleTransform(s, s);
            float night = Math.Max(0, nightK);

            int lines = 2 + Math.Min(Math.Max(0, breaks), 8);
            float newest = breaks > 0 ? SpiritArt.Clamp((t - rakeT) / 1.4f, 0, 1) : 1;
            var scene = SceneImage((int)Math.Ceiling(size) + 1, s, night, lines, newest);
            g.ResetTransform();
            g.DrawImage(scene, new Rectangle((int)o.X, (int)o.Y, scene.Width, scene.Height));
            g.TranslateTransform(o.X, o.Y);
            g.ScaleTransform(s, s);
            SpiritArt.DrawStars(g, t, night);
            SpiritArt.DrawFireflies(g, t, night);
            tip = SpiritArt.DrawSpirit(g, SX, Ground, R, pose, t);
            SpiritArt.DrawParticles(g, bits);
            if (bfState != 0)
            {
                var clip = g.Save();
                using (var disc = new GraphicsPath())
                {
                    disc.AddEllipse(150 - 140, 150 - 140, 280, 280);
                    g.SetClip(disc, CombineMode.Intersect);
                }
                SpiritArt.DrawButterfly(g, bfX, bfY, t, bfState == 2);
                g.Restore(clip);
            }

            float ring = 0;
            var ringColor = Theme.Accent;
            if (Breathing)
            {
                float c = (t - breatheT) % 12;
                ring = c < 4 ? c / 4 : c < 8 ? 1 : 1 - (c - 8) / 4;
                ringColor = SpiritArt.Moss;
            }
            else if (running) ring = progress;
            Draw.Ring(g, new RectangleF(1, 1, 298, 298), 10, Theme.Track, ringColor, ring);
            g.Restore(state);

            EnsureFonts(size);
            bool dark = night > 0.5f;
            string cap = caption;
            if (Breathing)
            {
                float bt = t - breatheT, c = bt % 12;
                cap = (c < 4 ? "BREATHE IN" : c < 8 ? "HOLD" : "BREATHE OUT") + "  ·  " + Math.Min(5, (int)(bt / 12) + 1) + " OF 5";
            }
            var timeColor = dark ? SpiritArt.Hex(0xE6E2DA) : running ? Theme.Ink : Theme.Muted;
            var capColor = dark ? SpiritArt.Hex(0xB9BACB) : Theme.Muted;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            CenteredText(g, time, timeFont, timeColor, o.X + 150 * s, o.Y + 234 * s);
            CenteredText(g, cap, captionFont, capColor, o.X + 150 * s, o.Y + 257 * s);
            DrawBubble(g, t, o, s);
        }

        static readonly StringFormat Tight = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces };

        static SizeF Measure(Graphics g, string text, Font font) => g.MeasureString(text, font, PointF.Empty, Tight);

        static void CenteredText(Graphics g, string text, Font font, Color color, float cx, float cy)
        {
            if (string.IsNullOrEmpty(text)) return;
            var size = Measure(g, text, font);
            float emPx = font.SizeInPoints * g.DpiY / 72f;
            float ascent = emPx * font.FontFamily.GetCellAscent(font.Style) / font.FontFamily.GetEmHeight(font.Style);
            float top = cy + emPx * 0.36f - ascent;
            using (var brush = new SolidBrush(color))
                g.DrawString(text, font, brush, cx - size.Width / 2, top, Tight);
        }

        void DrawBubble(Graphics g, float t, PointF o, float s)
        {
            if (bubble == null || t >= bubbleEnd) return;
            float a = Math.Min(1, (t - bubbleT0) / 0.25f) * Math.Min(1, (bubbleEnd - t) / 0.25f);
            if (a <= 0) return;
            var size = Measure(g, bubble, bubbleFont);
            float padX = 12 * s, padY = 8 * s, lineH = bubbleFont.SizeInPoints * g.DpiY / 72f * 1.25f;
            float w = size.Width + padX * 2, h = lineH + padY * 2;
            float cx = o.X + 150 * s, top = o.Y + 26 * s + (1 - a) * 6 * s;
            var rect = new RectangleF(cx - w / 2, top, w, h);
            using (var path = Draw.Rounded(rect, 12 * s))
            {
                path.AddPolygon(new[] { new PointF(cx - 6 * s, rect.Bottom - 1), new PointF(cx + 6 * s, rect.Bottom - 1), new PointF(cx, rect.Bottom + 6 * s) });
                path.FillMode = FillMode.Winding;
                using (var fill = new SolidBrush(SpiritArt.Alpha(Theme.Surface, a))) g.FillPath(fill, path);
            }
            using (var path = Draw.Rounded(rect, 12 * s))
            using (var pen = new Pen(SpiritArt.Alpha(Theme.Border, a)))
                g.DrawPath(pen, path);
            var color = SpiritArt.Mix(Theme.Surface, bubbleThought ? Theme.Muted : Theme.Ink, a);
            CenteredText(g, bubble, bubbleFont, color, cx, rect.Y + h / 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.Idle -= OnIdle;
                wake.Dispose();
                if (frameTimer != IntPtr.Zero) CloseHandle(frameTimer);
                frameTimer = IntPtr.Zero;
                sceneCache?.Dispose();
                timeFont?.Dispose();
                captionFont?.Dispose();
                bubbleFont?.Dispose();
            }
            base.Dispose(disposing);
        }
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

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float t = (float)watch.Elapsed.TotalSeconds;
            float k = SpiritArt.Sin((t % 3) / 3 * (float)Math.PI);
            float size = Math.Min(Width, Height);
            g.TranslateTransform((Width - size) / 2, (Height - size) / 2);
            g.ScaleTransform(size / 76f, size / 76f);
            var p = new SpiritPose { Sx = 1 + 0.03f * k, Sy = 0.95f + 0.08f * k, Tilt = -0.05f * k, EyeOpen = 0.35f * (1 - k), Mouth = Mouth.O, Yawn = k, Blush = 0.2f, Sprout = 0.15f };
            SpiritArt.DrawSpirit(g, 38, 66, 26, p, t);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) anim.Dispose();
            base.Dispose(disposing);
        }
    }
}
