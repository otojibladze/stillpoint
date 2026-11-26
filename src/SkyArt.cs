using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed class Meteor
    {
        public float T0, X, Y, Dx, Dy, Speed, Life, Length, Bright;
        public bool Fireball;
    }

    static class SkyArt
    {
        public static void DrawMeteor(Graphics g, Meteor m, float t)
        {
            float age = t - m.T0;
            if (age < 0 || age > m.Life + (m.Fireball ? 1.6f : 0)) return;
            float u = Math.Min(1, age / m.Life);
            float x = m.X + m.Dx * m.Speed * Math.Min(age, m.Life), y = m.Y + m.Dy * m.Speed * Math.Min(age, m.Life);
            float a = m.Bright * Sin(u * Pi) * (age > m.Life ? 0 : 1);
            if (m.Fireball)
            {
                float train = 1 - Smooth(m.Life, m.Life + 1.6f, age);
                using (var pen = new Pen(Alpha(Hex(0xBFE8D0), 0.22f * train * m.Bright), 1.6f))
                    g.DrawLine(pen, m.X + m.Dx * m.Speed * m.Life * 0.15f, m.Y + m.Dy * m.Speed * m.Life * 0.15f, x, y);
            }
            if (a <= 0.01f) return;
            int steps = 8;
            float seg = m.Length / steps;
            for (int i = 0; i < steps; i++)
                using (var pen = new Pen(Alpha(m.Fireball ? Mix(Hex(0xF4F0FF), Hex(0x9BF0C0), i / (float)steps) : Hex(0xF4F0FF), a * (1 - i / (float)steps) * 0.9f), (m.Fireball ? 2.2f : 1.3f) - i * 0.12f))
                    g.DrawLine(pen, x - m.Dx * seg * i, y - m.Dy * seg * i, x - m.Dx * seg * (i + 1), y - m.Dy * seg * (i + 1));
            if (m.Fireball) FillRadial(g, x, y, 6, 6, Alpha(Hex(0xC8FFD8), a * 0.7f), Color.FromArgb(0, 200, 255, 216));
            FillEllipse(g, Alpha(Color.White, a), x, y, m.Fireball ? 1.8f : 1.2f, m.Fireball ? 1.8f : 1.2f);
        }

        static readonly Color[] AuroraHues = { Hex(0x6EE7A8), Hex(0x5CD6C9), Hex(0x8CE69A) };

        public static void DrawAurora(Graphics g, float t, float level)
        {
            if (level < 0.01f) return;
            for (int band = 0; band < 3; band++)
            {
                var top = new PointF[42];
                var bottom = new PointF[42];
                for (int i = 0; i < top.Length; i++)
                {
                    float x = -10 + i * 8;
                    float y = 70 + band * 13 + 9 * Sin(x * 0.019f + t * 0.23f + band * 1.7f) + 4 * Sin(x * 0.051f - t * 0.41f + band);
                    float h = (34 + 16 * Sin(x * 0.031f + t * 0.37f + band * 2.3f)) * (0.75f + 0.25f * level);
                    bottom[i] = new PointF(x, y);
                    top[i] = new PointF(x, y - h);
                }
                using (var path = new GraphicsPath())
                {
                    path.AddCurve(bottom, 0.5f);
                    var up = (PointF[])top.Clone();
                    Array.Reverse(up);
                    path.AddCurve(up, 0.5f);
                    path.CloseFigure();
                    var bounds = path.GetBounds();
                    float a = level * (0.34f - band * 0.07f) * (0.8f + 0.2f * Sin(t * 0.9f + band));
                    using (var brush = new LinearGradientBrush(RectangleF.Inflate(bounds, 0, 1), Color.Transparent, Color.Transparent, LinearGradientMode.Vertical))
                    {
                        brush.InterpolationColors = new ColorBlend
                        {
                            Colors = new[] { Alpha(Hex(0xC78BE0), 0), Alpha(Hex(0xB98BE8), a * 0.45f), Alpha(AuroraHues[band], a), Alpha(AuroraHues[band], a * 0.2f) },
                            Positions = new[] { 0f, 0.35f, 0.8f, 1f },
                        };
                        g.FillPath(brush, path);
                    }
                }
            }
        }

        public static void DrawComet(Graphics g, float x, float y, float level, float t)
        {
            if (level < 0.01f) return;
            float dx = -0.86f, dy = -0.5f;
            DrawTail(g, x, y, dx, dy, 70 * (0.6f + 0.4f * level), 7, Alpha(Hex(0xFFF3D6), 0.38f * level), 0.18f);
            DrawTail(g, x, y, -0.95f, -0.31f, 85 * (0.6f + 0.4f * level), 2.2f, Alpha(Hex(0xA9D4FF), 0.34f * level), 0);
            FillRadial(g, x, y, 9, 9, Alpha(Hex(0xFFF6E0), 0.55f * level), Color.FromArgb(0, 255, 246, 224));
            FillEllipse(g, Alpha(Color.White, Math.Min(1, 0.6f + level * 0.4f) * (0.9f + 0.1f * Sin(t * 3))), x, y, 1.6f, 1.6f);
        }

        static void DrawTail(Graphics g, float x, float y, float dx, float dy, float length, float width, Color color, float curve)
        {
            float nx = -dy, ny = dx;
            var end = new PointF(x + dx * length + nx * length * curve, y + dy * length + ny * length * curve);
            using (var path = new GraphicsPath())
            {
                path.AddBezier(new PointF(x + nx * 1.2f, y + ny * 1.2f), new PointF(x + dx * length * 0.5f + nx * width, y + dy * length * 0.5f + ny * width), new PointF(end.X + nx * width * 1.6f, end.Y + ny * width * 1.6f), end);
                path.AddBezier(end, new PointF(end.X - nx * width * 0.6f, end.Y - ny * width * 0.6f), new PointF(x + dx * length * 0.5f - nx * width * 0.4f, y + dy * length * 0.5f - ny * width * 0.4f), new PointF(x - nx * 1.2f, y - ny * 1.2f));
                path.CloseFigure();
                using (var brush = new LinearGradientBrush(new PointF(x, y), new PointF(end.X + 0.01f, end.Y + 0.01f), color, Color.FromArgb(0, color)))
                    g.FillPath(brush, path);
            }
        }

        public static void DrawMoonShadow(Graphics g, float x, float y, float r, float shadow, float dim, float alpha)
        {
            if (alpha <= 0.01f) return;
            if (dim > 0.01f) FillEllipse(g, Alpha(Hex(0x3A2A2A), dim * alpha * 0.5f), x, y, r, r);
            if (shadow <= 0.01f) return;
            using (var moon = new GraphicsPath())
            {
                moon.AddEllipse(x - r, y - r, r * 2, r * 2);
                var state = g.Save();
                g.SetClip(moon, CombineMode.Intersect);
                float big = r * 2.4f, cx = x - r - big + shadow * (r * 2 + 0.2f * r);
                FillEllipse(g, Alpha(Mix(Hex(0x4A2018), Hex(0x9C3F22), shadow), alpha * (0.75f + 0.25f * shadow)), cx, y - r * 0.15f, big, big);
                if (shadow > 0.9f) FillRadial(g, x - r * 0.3f, y - r * 0.3f, r * 1.2f, r * 1.2f, Alpha(Hex(0xC8643A), alpha * 0.45f), Alpha(Hex(0x6A2416), 0));
                g.Restore(state);
            }
            if (shadow > 0.9f) FillRadial(g, x, y, r * 2.2f, r * 2.2f, Alpha(Hex(0xA04A2A), alpha * 0.18f), Color.FromArgb(0, 160, 74, 42));
        }

        public static void DrawSunEclipse(Graphics g, float x, float y, float r, float cover, bool annular, Color sky)
        {
            if (cover <= 0.01f) return;
            float appear = Smooth(0, 0.08f, cover);
            if (cover > 0.97f && !annular)
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Pi / 6 + 0.2f;
                    using (var ray = RoundPen(Alpha(Color.White, 0.18f * appear), 1.6f))
                        g.DrawLine(ray, x + Cos(a) * r * 1.05f, y + Sin(a) * r * 1.05f, x + Cos(a) * r * (2.2f + (i % 3) * 0.5f), y + Sin(a) * r * (2.2f + (i % 3) * 0.5f));
                }
                FillRadial(g, x, y, r * 2.4f, r * 2.4f, Alpha(Color.White, 0.5f * appear), Color.FromArgb(0, 255, 255, 255));
            }
            FillRadial(g, x, y, r * 1.8f, r * 1.8f, Alpha(Hex(0xFFF1C8), 0.5f * appear * (1 - cover * 0.6f)), Color.FromArgb(0, 255, 241, 200));
            FillEllipse(g, Alpha(Hex(0xFFF6DC), appear), x, y, r, r);
            float shift = (1 - cover) * r * 2.05f;
            float moonR = annular ? r * 0.9f : r * 1.02f;
            FillEllipse(g, Alpha(Mix(sky, Hex(0x2A2E3E), 0.12f + 0.5f * Smooth(0.6f, 1, cover)), appear), x + shift * 0.92f, y - shift * 0.35f, moonR, moonR);
        }
    }
}
