using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed class CritterPose
    {
        public float Gait, Run, Sit, Curl, Dig, Flick, Blink, Yawn, Ear, Hide, Tilt, Turn, Puff, Spread, Flap, Open, Flip, Walk, Lit, Shadow;
        public int Paw;
        public bool Acorn;
    }

    static class CritterArt
    {
        static readonly Color NightBlue = Hex(0x3A3E52);

        static Color Dim(int rgb, float night, float k = 0.4f) => Mix(Hex(rgb), NightBlue, night * k);

        public static float Lerp(float a, float b, float k) => a + (b - a) * k;

        public static PointF Lerp(PointF a, PointF b, float k) => new PointF(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k);

        static PointF Bez(PointF p0, PointF p1, PointF p2, PointF p3, float u)
        {
            float v = 1 - u;
            return new PointF(v * v * v * p0.X + 3 * v * v * u * p1.X + 3 * v * u * u * p2.X + u * u * u * p3.X,
                v * v * v * p0.Y + 3 * v * v * u * p1.Y + 3 * v * u * u * p2.Y + u * u * u * p3.Y);
        }

        static void Mirror(Graphics g, float facing) => g.ScaleTransform(facing < 0 ? -1 : 1, 1);

        public static void DrawLadybug(Graphics g, float x, float y, float angle, float t, CritterPose p)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle * Deg);
            float flip = Cos(Clamp(p.Flip, 0, 1) * Pi);
            if (p.Flip > 0.001f)
            {
                g.TranslateTransform(0, -3.2f);
                g.ScaleTransform(1, flip >= 0 ? Math.Max(0.08f, flip) : Math.Min(-0.08f, flip));
                g.TranslateTransform(0, 3.2f);
            }
            bool belly = flip < 0;
            using (var leg = RoundPen(Ink, 0.7f))
                for (int i = 0; i < 3; i++)
                {
                    float lx = -2.6f + i * 2.6f;
                    float swing = Sin(t * 16 + i * 2.1f) * 1.1f * p.Walk + (belly ? Sin(t * 34 + i * 1.7f) * 0.9f : 0);
                    g.DrawLine(leg, lx, -1.2f, lx + 0.6f + swing, 0.7f);
                }
            FillEllipse(g, Ink, 4.9f, -1.9f, 1.9f, 1.7f);
            FillEllipse(g, Alpha(Color.White, 0.9f), 5.9f, -2.5f, 0.45f, 0.45f);
            using (var antenna = new Pen(Ink, 0.5f)) g.DrawLine(antenna, 5.6f, -3.2f, 7.4f, -5.2f);
            if (p.Open > 0.02f)
            {
                float flap = Sin(t * 55);
                var wing = Alpha(Hex(0xEDEAE2), 0.55f * p.Open);
                foreach (float f in new[] { flap, -flap * 0.6f })
                {
                    var w = g.Save();
                    g.TranslateTransform(1.5f, -4.4f);
                    g.RotateTransform((20 + f * 28) * p.Open);
                    FillEllipse(g, wing, -4.6f * p.Open, 0, 5.2f * p.Open, 1.6f);
                    g.Restore(w);
                }
            }
            var shell = g.Save();
            if (p.Open > 0.02f)
            {
                g.TranslateTransform(3.5f, -2.5f);
                g.RotateTransform(p.Open * 40);
                g.TranslateTransform(-3.5f, 2.5f);
            }
            using (var dome = new GraphicsPath())
            {
                dome.AddArc(-5, -5.6f, 10, 9.2f, 180, 180);
                dome.CloseFigure();
                using (var b = new SolidBrush(Hex(0xCF3F33))) g.FillPath(b, dome);
                var clip = g.Save();
                g.SetClip(dome, CombineMode.Intersect);
                FillEllipse(g, Ink, -2.6f, -2.9f, 1.05f, 1.05f);
                FillEllipse(g, Ink, 0.5f, -4.4f, 0.95f, 0.95f);
                FillEllipse(g, Ink, 2.8f, -2.4f, 0.9f, 0.9f);
                FillEllipse(g, Ink, -4.5f, -1.4f, 0.7f, 0.7f);
                FillEllipse(g, Alpha(Hex(0x7E241D), 0.55f), 0, -0.5f, 5.4f, 1.3f);
                FillEllipse(g, Alpha(Color.White, 0.5f), -1.6f, -4.4f, 1.8f, 0.7f);
                g.Restore(clip);
            }
            g.Restore(shell);
            g.Restore(st);
        }

        public static void DrawBee(Graphics g, float x, float y, float t, float facing, CritterPose p)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            Mirror(g, facing);
            g.RotateTransform(p.Tilt * Deg);
            var stripe = Hex(0x3B2F27);
            using (var leg = new Pen(stripe, 0.5f))
            {
                g.DrawLine(leg, -1, 2.8f, -1.6f, 4.6f);
                g.DrawLine(leg, 1.4f, 2.8f, 1.2f, 4.7f);
            }
            using (var body = new GraphicsPath())
            {
                body.AddEllipse(-4.6f, -3.1f, 9.2f, 6.2f);
                using (var b = new SolidBrush(Hex(0xE9B23C))) g.FillPath(b, body);
                var clip = g.Save();
                g.SetClip(body, CombineMode.Intersect);
                using (var band = new SolidBrush(stripe))
                {
                    g.FillRectangle(band, -2.3f, -4, 1.3f, 8);
                    g.FillRectangle(band, 0.5f, -4, 1.3f, 8);
                    g.FillRectangle(band, -5, -4, 1.2f, 8);
                }
                FillEllipse(g, Alpha(Color.White, 0.35f), 0.2f, -1.9f, 2.6f, 0.8f);
                g.Restore(clip);
            }
            FillPolygon(g, stripe, new PointF(-4.3f, -0.5f), new PointF(-6.6f, 0.4f), new PointF(-4.2f, 0.9f));
            FillEllipse(g, stripe, 4.7f, -0.6f, 2.1f, 2);
            FillEllipse(g, Alpha(Color.White, 0.85f), 5.5f, -1.2f, 0.5f, 0.5f);
            using (var antenna = RoundPen(stripe, 0.5f))
                g.DrawLines(antenna, new[] { new PointF(5.2f, -2.4f), new PointF(6.2f, -4.6f), new PointF(7.2f, -4.9f) });
            float flap = Sin(t * 62);
            var wing = Alpha(Hex(0xF4F8FA), 0.55f);
            foreach (float a in new[] { -32 + flap * 26, -12 + flap * 26 })
            {
                var w = g.Save();
                g.TranslateTransform(0.5f, -2.4f);
                g.RotateTransform(a);
                FillEllipse(g, wing, 0, -3.2f, 1.9f, 3.4f);
                g.Restore(w);
            }
            g.Restore(st);
        }

        public static void DrawAcorn(Graphics g, float x, float y, float s, float night)
        {
            FillEllipse(g, Dim(0xB27B45, night), x, y + 0.5f * s, 1.5f * s, 1.8f * s);
            FillEllipse(g, Alpha(Color.White, 0.3f), x - 0.5f * s, y + 0.2f * s, 0.4f * s, 0.7f * s);
            FillEllipse(g, Dim(0x6E4B2E, night), x, y - 0.9f * s, 1.7f * s, 0.9f * s);
            using (var stem = RoundPen(Dim(0x6E4B2E, night), 0.5f * s)) g.DrawLine(stem, x, y - 1.6f * s, x + 0.4f * s, y - 2.4f * s);
        }

        public static void DrawSquirrel(Graphics g, float x, float y, float t, float facing, CritterPose p, float night)
        {
            var fur = Dim(0xA6683E, night);
            var dark = Dim(0x7C4A2B, night);
            var belly = Dim(0xE7D0AA, night);
            var tailColor = Dim(0x9A5C35, night);
            var tailLight = Dim(0xC69264, night);
            var st = g.Save();
            g.TranslateTransform(x, y);
            Mirror(g, facing);
            float stretch = p.Run * Math.Abs(Sin(p.Gait));
            var tail = g.Save();
            g.TranslateTransform(-5, -6);
            g.RotateTransform(Sin(t * 28) * 11 * p.Flick + p.Dig * 10);
            var a0 = new PointF(0, 0);
            var a1 = Lerp(new PointF(-8, 0.5f), new PointF(-8, -3), p.Run);
            var a2 = Lerp(new PointF(-10.5f, -15), new PointF(-14, -7), p.Run);
            var a3 = Lerp(new PointF(-3.5f, -16), new PointF(-17, -5), p.Run);
            using (var path = new GraphicsPath())
            {
                path.AddBezier(a0, a1, a2, a3);
                using (var soft = RoundPen(Alpha(tailColor, 0.3f), 8.4f)) g.DrawPath(soft, path);
                using (var pen = RoundPen(tailColor, 6.8f)) g.DrawPath(pen, path);
                var hi = g.Save();
                g.TranslateTransform(-0.7f, -0.6f);
                using (var light = RoundPen(Alpha(tailLight, 0.35f), 3.2f)) g.DrawPath(light, path);
                g.Restore(hi);
            }
            g.Restore(tail);
            FillEllipse(g, dark, -3 - stretch * 3.2f, -0.8f - stretch, 2.8f, 0.95f);
            var body = g.Save();
            g.TranslateTransform(3, -2);
            g.RotateTransform(p.Dig * 24 - stretch * 6);
            g.ScaleTransform(1 + stretch * 0.14f, 1 - stretch * 0.06f);
            g.TranslateTransform(-3, 2);
            FillEllipse(g, fur, 0.5f, -5.6f, 6.4f, 4.1f);
            FillEllipse(g, belly, 2.8f, -3.5f, 3.4f, 2.1f);
            FillEllipse(g, Mix(fur, dark, 0.35f), -3, -4, 3.6f, 3.3f);
            float hx = 6.6f, hy = -9.2f;
            FillPolygon(g, fur, new PointF(hx - 1.6f, hy - 2.2f), new PointF(hx - 1.9f, hy - 6.2f), new PointF(hx + 0.6f, hy - 2.8f));
            FillPolygon(g, Dim(0xC98F7A, night), new PointF(hx - 1.2f, hy - 2.6f), new PointF(hx - 1.5f, hy - 5), new PointF(hx, hy - 2.9f));
            FillEllipse(g, fur, hx, hy, 3.6f, 3.2f);
            FillEllipse(g, fur, hx + 2.4f, hy + 0.6f, 2.2f, 1.8f);
            FillEllipse(g, belly, hx + 1.4f, hy + 1.6f, 2, 1.2f);
            FillEllipse(g, Ink, hx + 1.2f, hy - 0.9f, 0.85f, 0.85f);
            FillEllipse(g, Alpha(Color.White, 0.85f), hx + 1.45f, hy - 1.2f, 0.3f, 0.3f);
            FillEllipse(g, Dim(0x4A2E22, night), hx + 4.4f, hy + 0.2f, 0.6f, 0.5f);
            if (p.Acorn) DrawAcorn(g, hx + 4.4f, hy + 2.4f, 0.9f, night);
            g.Restore(body);
            float paw = p.Paw == 1 ? Sin(t * 26) * 1.4f : 0;
            float lift = p.Paw == 2 ? Math.Max(0, Sin(t * 16)) * 1.4f : 0;
            float reach = stretch * 3.6f + p.Dig * 1.4f;
            FillEllipse(g, dark, 4.6f + reach + paw, -0.8f - lift - stretch, 1.4f, 0.9f);
            FillEllipse(g, dark, 6 + reach - paw, -0.7f - stretch, 1.3f, 0.85f);
            g.Restore(st);
        }

        public static void DrawOwl(Graphics g, float x, float y, float t, CritterPose p, float night)
        {
            var body = Dim(0x8E7A64, night, 0.25f);
            var wing = Dim(0x705B48, night, 0.25f);
            var belly = Dim(0xD4C2A6, night, 0.25f);
            var face = Dim(0xE7DAC4, night, 0.2f);
            var beak = Hex(0xCDA15A);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(p.Tilt * Deg);
            g.ScaleTransform(1 + 0.07f * p.Puff, 1 + 0.04f * p.Puff);
            if (p.Spread > 0.02f)
                foreach (int s in new[] { -1, 1 })
                {
                    var w = g.Save();
                    g.TranslateTransform(s * 4.5f, -13);
                    g.RotateTransform(s * (-15 + p.Flap * 38));
                    g.ScaleTransform(s * p.Spread, 1);
                    FillEllipse(g, wing, 9, 0.5f, 10, 3.8f);
                    FillEllipse(g, Mix(wing, belly, 0.35f), 8, 1.8f, 7.5f, 1.6f);
                    for (int i = 0; i < 3; i++) FillEllipse(g, Mix(wing, Ink, 0.25f), 15 + i * 1.6f, 1.4f + i * 0.4f, 2.2f, 1.2f);
                    g.Restore(w);
                }
            FillEllipse(g, body, 0, -9.5f, 7, 9);
            FillEllipse(g, belly, 0, -7.6f, 4.6f, 6.2f);
            using (var mark = new Pen(Alpha(wing, 0.75f), 0.55f))
                foreach (var m in new[] { new PointF(-2.3f, -10.5f), new PointF(0, -10.5f), new PointF(2.3f, -10.5f), new PointF(-1.15f, -7.9f), new PointF(1.15f, -7.9f), new PointF(0, -5.3f) })
                    g.DrawLines(mark, new[] { new PointF(m.X - 0.7f, m.Y - 0.4f), new PointF(m.X, m.Y + 0.3f), new PointF(m.X + 0.7f, m.Y - 0.4f) });
            if (p.Spread < 0.98f)
                foreach (int s in new[] { -1, 1 })
                {
                    var w = g.Save();
                    g.TranslateTransform(s * 5.6f, -9.5f);
                    g.RotateTransform(s * -6);
                    FillEllipse(g, Alpha(wing, 1 - p.Spread), 0, 0, 2.7f, 6.8f);
                    g.Restore(w);
                }
            using (var claw = RoundPen(beak, 0.6f))
                foreach (int s in new[] { -1, 1 })
                    for (int i = -1; i <= 1; i++)
                        g.DrawLine(claw, s * 2.2f, -1.2f, s * 2.2f + i * 0.8f, 0.2f);
            float hy = -20.5f;
            FillPolygon(g, body, new PointF(-4.2f, hy - 4.2f), new PointF(-6.6f, hy - 8.6f), new PointF(-2.2f, hy - 5.2f));
            FillPolygon(g, body, new PointF(4.2f, hy - 4.2f), new PointF(6.6f, hy - 8.6f), new PointF(2.2f, hy - 5.2f));
            FillEllipse(g, body, 0, hy, 7.2f, 6.2f);
            float sq = Cos(p.Turn), fx = Sin(p.Turn) * 3.4f;
            if (sq > 0.1f)
            {
                foreach (int s in new[] { -1, 1 }) FillEllipse(g, face, fx + s * 2.9f * sq, hy + 0.2f, 3.3f * sq, 3.3f);
                float open = 1 - Clamp(p.Blink, 0, 1);
                foreach (int s in new[] { -1, 1 })
                {
                    float ex = fx + s * 2.9f * sq;
                    if (night > 0.3f) FillRadial(g, ex, hy, 3.6f * sq, 3.6f, Color.FromArgb((int)(80 * night * open), 240, 180, 80), Color.FromArgb(0, 240, 180, 80));
                    if (open > 0.12f)
                    {
                        FillEllipse(g, Hex(0xEDB24A), ex, hy, 1.95f * sq, 1.95f * open);
                        FillEllipse(g, Ink, ex + fx * 0.1f, hy, 1.1f * sq, 1.1f * open);
                        FillEllipse(g, Alpha(Color.White, 0.9f), ex + 0.5f * sq, hy - 0.6f * open, 0.4f * sq, 0.4f * open);
                    }
                    else using (var lid = RoundPen(Mix(body, Ink, 0.4f), 0.6f)) g.DrawLine(lid, ex - 1.6f * sq, hy, ex + 1.6f * sq, hy);
                }
                using (var brow = RoundPen(Mix(body, Ink, 0.3f), 0.7f))
                    g.DrawLines(brow, new[] { new PointF(fx - 2.6f * sq, hy - 3.2f), new PointF(fx, hy - 1.6f), new PointF(fx + 2.6f * sq, hy - 3.2f) });
                FillPolygon(g, beak, new PointF(fx - 0.9f * sq, hy + 1), new PointF(fx + 0.9f * sq, hy + 1), new PointF(fx, hy + 3));
            }
            else if (sq < -0.1f)
                using (var mark = RoundPen(Alpha(wing, 0.8f), 0.6f))
                    for (int i = 0; i < 3; i++)
                        g.DrawLines(mark, new[] { new PointF(-fx - 1.4f, hy - 2.4f + i * 1.8f), new PointF(-fx, hy - 1.4f + i * 1.8f), new PointF(-fx + 1.4f, hy - 2.4f + i * 1.8f) });
            g.Restore(st);
        }

        public static void DrawMoth(Graphics g, float x, float y, float t, CritterPose p, float scale)
        {
            float flap = 0.25f + 0.75f * Math.Abs(Sin(t * 28));
            var dark = Hex(0x262833);
            var wing = Mix(Mix(Hex(0x8F8C9C), Hex(0xEADCC0), p.Lit), dark, p.Shadow);
            var edge = Mix(Mix(Hex(0x6A6878), Hex(0xB9A88A), p.Lit), dark, p.Shadow);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(scale, scale);
            if (p.Lit > 0.3f) FillRadial(g, 0, 0, 7, 6, Color.FromArgb((int)(70 * p.Lit), 255, 220, 150), Color.FromArgb(0, 255, 220, 150));
            using (var fore = new SolidBrush(wing))
            using (var hind = new SolidBrush(Mix(wing, edge, 0.35f)))
                foreach (int s in new[] { -1, 1 })
                {
                    var w = g.Save();
                    g.ScaleTransform(s * flap, 1);
                    g.FillClosedCurve(hind, new[] { new PointF(0.4f, 0.3f), new PointF(3.8f, 1.2f), new PointF(2.6f, 3.6f), new PointF(0.4f, 2.4f) }, FillMode.Winding, 0.5f);
                    g.FillClosedCurve(fore, new[] { new PointF(0.4f, -1.4f), new PointF(5.2f, -3.2f), new PointF(5.6f, 0.2f), new PointF(0.6f, 0.9f) }, FillMode.Winding, 0.5f);
                    FillEllipse(g, edge, 3.5f, -1.1f, 0.7f, 0.7f);
                    g.Restore(w);
                }
            FillEllipse(g, Mix(Mix(Hex(0x5E5A66), Hex(0xA8957A), p.Lit), dark, p.Shadow), 0, 0.4f, 0.9f, 2.6f);
            using (var antenna = new Pen(edge, 0.45f))
            {
                g.DrawLine(antenna, -0.3f, -2.1f, -1.7f, -4.1f);
                g.DrawLine(antenna, 0.3f, -2.1f, 1.7f, -4.1f);
            }
            g.Restore(st);
        }

        public static void DrawFox(Graphics g, float x, float y, float t, float facing, CritterPose p, float night)
        {
            var orange = Dim(0xC9743B, night, 0.3f);
            var deep = Dim(0xA45A2C, night, 0.3f);
            var cream = Dim(0xF0E2CB, night, 0.35f);
            var dark = Dim(0x463631, night, 0.2f);
            float k = Smooth(0, 1, p.Sit), c = Smooth(0, 1, p.Curl), l = Smooth(0, 1, p.Hide);
            PointF P(PointF stand, PointF sit, PointF lie) => Lerp(Lerp(stand, sit, k), lie, l);
            float F(float stand, float sit, float lie) => Lerp(Lerp(stand, sit, k), lie, l);
            var st = g.Save();
            g.TranslateTransform(x, y);
            Mirror(g, facing);
            float swing = p.Run * Sin(p.Gait) * 2.6f, sway = Sin(t * 2.6f) * 0.9f * (1 - c) + p.Flick * Sin(t * 14) * 2.2f;
            var t0 = P(new PointF(-7.5f, -9.5f), new PointF(-4.5f, -2.6f), new PointF(-7, -2.4f));
            var t1 = Lerp(new PointF(-13, -11 + sway), new PointF(-11, 0.6f + sway * 0.3f), c);
            var t2 = Lerp(new PointF(-17.5f, -8 + sway), new PointF(-3, 2.8f), c);
            var t3 = Lerp(new PointF(-21, -4.5f + sway * 1.5f), Lerp(new PointF(7.4f, 0.9f), new PointF(11.5f, 0.2f), l), c);
            Action tail = () =>
            {
                using (var path = new GraphicsPath())
                {
                    path.AddBezier(t0, t1, t2, t3);
                    using (var pen = RoundPen(orange, 5.6f)) g.DrawPath(pen, path);
                    using (var shade = RoundPen(Alpha(deep, 0.5f), 2.2f)) g.DrawPath(shade, path);
                }
                using (var tip = RoundPen(cream, 4.8f))
                    g.DrawLines(tip, new[] { Bez(t0, t1, t2, t3, 0.84f), Bez(t0, t1, t2, t3, 0.92f), Bez(t0, t1, t2, t3, 0.99f) });
            };
            if (c < 0.5f) tail();
            using (var leg = RoundPen(dark, 1.7f))
            {
                g.DrawLine(leg, P(new PointF(-5.6f, -8), new PointF(-3, -3.5f), new PointF(-3, -1.6f)), P(new PointF(-5.6f - swing, 0), new PointF(-0.5f, -0.6f), new PointF(-0.5f, -0.6f)));
                g.DrawLine(leg, P(new PointF(-3.8f, -8), new PointF(-1.5f, -3.5f), new PointF(-1.5f, -1.6f)), P(new PointF(-3.8f + swing, 0), new PointF(1.2f, -0.6f), new PointF(1.2f, -0.6f)));
                var shoulder = P(new PointF(5.2f, -8), new PointF(3.8f, -8), new PointF(6.4f, -2.6f));
                g.DrawLine(leg, new PointF(shoulder.X - 0.8f, shoulder.Y), P(new PointF(5.2f + swing, 0), new PointF(2.9f, 0), new PointF(11.6f, -0.7f)));
                g.DrawLine(leg, new PointF(shoulder.X + 0.8f, shoulder.Y), P(new PointF(6.8f - swing, 0), new PointF(4.7f, 0), new PointF(12.4f, -0.4f)));
            }
            var center = P(new PointF(0, -9.6f), new PointF(1.4f, -10.2f), new PointF(0.4f, -4.4f));
            float breath = l * Sin(t * 1.6f) * 0.25f;
            var b = g.Save();
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(F(0, 14, -3));
            FillEllipse(g, orange, 0, 0, F(8.6f, 4.3f, 9.4f), F(3.9f, 7.4f, 4.3f) + breath);
            g.Restore(b);
            float haunch = Math.Max(k, l);
            if (haunch > 0.05f) FillEllipse(g, orange, Lerp(-2.4f, -5, l), Lerp(-4.6f, -4.2f, l), Lerp(5.2f, 4.8f, l) * haunch, Lerp(4.6f, 4.1f, l) * haunch + breath);
            FillEllipse(g, cream, F(6.2f, 4, 6.6f), F(-8.8f, -12.4f, -4.4f), F(2.5f, 1.9f, 2.3f), F(2.5f, 3.4f, 2.2f));
            var head = P(new PointF(9.8f, -13.4f), new PointF(5.4f, -20.2f), new PointF(10.4f, -5.6f));
            var h = g.Save();
            g.TranslateTransform(head.X, head.Y);
            g.RotateTransform((p.Tilt + l * 0.12f) * Deg);
            FillPolygon(g, deep, new PointF(0.6f, -2.2f), new PointF(2, -7 + l * 1.4f), new PointF(3, -1.8f));
            FillEllipse(g, orange, 0, 0, 3.6f, 3.1f);
            FillEllipse(g, orange, 3.2f, 0.9f, 2.8f, 1.35f);
            FillEllipse(g, cream, 2, 1.7f, 2.8f, 1.15f);
            if (p.Yawn > 0.02f) FillPolygon(g, Dim(0x5A2E2A, night, 0.2f), new PointF(1.6f, 1.5f), new PointF(5.2f, 1.4f), new PointF(3.8f, 1.6f + 2.2f * p.Yawn));
            FillEllipse(g, Ink, 5.9f, 0.6f, 0.7f, 0.6f);
            var ear = g.Save();
            g.TranslateTransform(-0.8f, -2.2f);
            g.RotateTransform(-p.Ear * 18 - l * 25);
            FillPolygon(g, orange, new PointF(-1.4f, 0), new PointF(-0.6f, -5.4f), new PointF(1.6f, -0.2f));
            FillPolygon(g, dark, new PointF(-0.8f, -0.4f), new PointF(-0.4f, -3.8f), new PointF(0.9f, -0.5f));
            g.Restore(ear);
            float open = 1 - Math.Max(Clamp(p.Blink, 0, 1), Clamp(p.Yawn * 1.4f, 0, 1));
            if (open > 0.15f)
            {
                FillEllipse(g, Ink, 1.6f, -0.7f, 0.75f, 0.72f * open);
                FillEllipse(g, Alpha(Color.White, 0.8f), 1.85f, -1, 0.25f, 0.25f * open);
            }
            else using (var lid = RoundPen(Ink, 0.5f)) g.DrawArc(lid, 0.8f, -1.4f, 1.8f, 1.2f, 20, 140);
            g.Restore(h);
            if (c >= 0.5f) tail();
            g.Restore(st);
        }
        static readonly float[][] Twigs = { new[] { -10f, -4.6f, 9f, -1.2f }, new[] { -9f, -1.8f, 8f, -5f }, new[] { -7f, -0.6f, 10f, -3.2f }, new[] { -10.4f, -3.2f, 3f, -0.4f }, new[] { -4f, -5.6f, 10.6f, -2.2f }, new[] { -8f, -5.2f, 6f, -0.8f }, new[] { -2f, -0.4f, 10.4f, -4.6f }, new[] { -10.6f, -5.4f, -1f, -0.2f }, new[] { 1f, -5.8f, 9.6f, -0.8f }, new[] { -6f, -3.6f, 7f, -5.6f }, new[] { -9.6f, -2.6f, 9.8f, -2.8f }, new[] { -5f, -0.2f, 4f, -5.8f }, new[] { -11f, -4f, 11f, -4.2f }, new[] { -3f, -2.4f, 8f, -0.2f } };

        public static void DrawNest(Graphics g, float x, float y, float angle, NestStage stage, float progress, float t, float beg, float flap, int chicks, float night, Action inside = null)
        {
            if (stage == NestStage.None) return;
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle * 0.35f * Deg);
            g.ScaleTransform(1.35f, 1.35f);
            float built = stage == NestStage.Building ? Smooth(0, 1, progress) : 1;
            g.ScaleTransform(0.55f + 0.45f * built, 0.4f + 0.6f * built);
            var dark = Dim(0x5C4027, night, 0.35f);
            var mid = Dim(0x7A5634, night, 0.35f);
            var light = Dim(0x9C7A50, night, 0.35f);
            FillEllipse(g, Alpha(dark, built), 0, -5.8f, 10.5f, 2.6f);
            if (stage == NestStage.Eggs)
                foreach (float ex in new[] { -4.2f, 4.2f, 0f })
                {
                    float wobble = Sin(t * 1.3f + ex) * 0.15f;
                    var egg = g.Save();
                    g.TranslateTransform(ex, -6.6f - (ex == 0 ? 0.6f : 0));
                    g.RotateTransform((ex * 4 + wobble * 10));
                    FillEllipse(g, Dim(0xBFDCEB, night, 0.3f), 0, 0, 2.1f, 2.7f);
                    FillEllipse(g, Alpha(Color.White, 0.5f * (1 - night)), -0.6f, -0.9f, 0.6f, 0.9f);
                    foreach (var (sx, sy) in new[] { (0.7f, 0.4f), (-0.5f, 1.1f), (0.2f, -0.7f), (0.9f, 1.4f) })
                        FillEllipse(g, Dim(0x7E9AAA, night, 0.3f), sx, sy, 0.32f, 0.32f);
                    g.Restore(egg);
                }
            else if (stage == NestStage.Chicks || stage == NestStage.Fledglings)
            {
                bool big = stage == NestStage.Fledglings;
                float grow = big ? 1 : 0.55f + 0.45f * progress;
                float[] spots = { -5f, 5f, 0f };
                for (int i = 0; i < Math.Min(chicks, 3); i++)
                {
                    float cx = spots[i] * (big ? 1.1f : 1), phase = t * 3 + i * 2.1f;
                    float lift = beg * (2.6f + Sin(phase * 2.2f) * 0.8f) + (big ? 1.4f : 0);
                    float hy = -7.2f - lift - (big ? 1.5f : 0);
                    if (big)
                    {
                        FillEllipse(g, Dim(0x8A6A55, night), cx, hy + 3.6f, 3.6f, 3f);
                        float w = flap * Sin(t * 22 + i * 1.7f);
                        foreach (int s in new[] { -1, 1 })
                        {
                            var wing = g.Save();
                            g.TranslateTransform(cx + s * 2.6f, hy + 2.6f);
                            g.RotateTransform(s * (20 + w * 50) * flap);
                            FillEllipse(g, Dim(0x6E5444, night), s * 1.6f, 0, 2.2f, 1.1f);
                            g.Restore(wing);
                        }
                        FillEllipse(g, Dim(0xD9A27E, night), cx, hy + 4.4f, 2.1f, 1.6f);
                    }
                    float hr = 2.5f * grow;
                    FillEllipse(g, Dim(big ? 0x8A6A55 : 0x9C958D, night), cx, hy, hr, hr * 0.95f);
                    if (!big)
                        for (int f = -1; f <= 1; f++)
                            using (var fuzz = RoundPen(Dim(0xB8B1A9, night), 0.5f))
                                g.DrawLine(fuzz, cx + f * 0.8f, hy - hr * 0.8f, cx + f * 1.2f, hy - hr * 1.25f);
                    FillEllipse(g, Ink, cx - hr * 0.42f, hy - hr * 0.15f, 0.48f, 0.48f);
                    FillEllipse(g, Ink, cx + hr * 0.42f, hy - hr * 0.15f, 0.48f, 0.48f);
                    float open = beg * (0.6f + 0.4f * Math.Abs(Sin(phase * 3)));
                    var beak = Hex(0xF2B33D);
                    FillPolygon(g, beak, new PointF(cx - 1.1f * grow, hy + 0.4f), new PointF(cx + 1.1f * grow, hy + 0.4f), new PointF(cx, hy - 1.2f * grow - open * 1.6f));
                    if (open > 0.1f)
                    {
                        FillPolygon(g, Hex(0xD9544A), new PointF(cx - 0.8f * grow, hy + 0.5f), new PointF(cx + 0.8f * grow, hy + 0.5f), new PointF(cx, hy + 0.5f + open * 1.2f));
                        FillPolygon(g, beak, new PointF(cx - 1.1f * grow, hy + 0.6f), new PointF(cx + 1.1f * grow, hy + 0.6f), new PointF(cx, hy + 1.2f + open * 1.8f));
                    }
                    else FillPolygon(g, beak, new PointF(cx - 1.1f * grow, hy + 0.4f), new PointF(cx + 1.1f * grow, hy + 0.4f), new PointF(cx, hy + 1.3f * grow));
                }
            }
            inside?.Invoke();
            using (var bowl = new GraphicsPath())
            {
                bowl.AddArc(-11, -10, 22, 10.5f, 0, 180);
                bowl.CloseFigure();
                using (var brush = new SolidBrush(Alpha(mid, built))) g.FillPath(brush, bowl);
                var clip = g.Save();
                g.SetClip(bowl, CombineMode.Intersect);
                int twigs = stage == NestStage.Building ? (int)Math.Round(Twigs.Length * built) : Twigs.Length;
                for (int i = 0; i < twigs; i++)
                    using (var pen = RoundPen(i % 2 == 0 ? light : dark, 0.7f))
                        g.DrawLine(pen, Twigs[i][0], Twigs[i][1], Twigs[i][2], Twigs[i][3]);
                FillEllipse(g, Alpha(Ink, 0.18f), 0, 0.6f, 11, 2);
                g.Restore(clip);
            }
            using (var rim = RoundPen(Alpha(light, built), 1.1f)) g.DrawArc(rim, -10.6f, -8.2f, 21.2f, 4.6f, 0, 180);
            using (var stray = RoundPen(Alpha(dark, built), 0.6f))
            {
                g.DrawLine(stray, 9.5f, -5.6f, 12.4f, -7.2f);
                g.DrawLine(stray, -9.8f, -4.6f, -12.6f, -5.4f);
            }
            g.Restore(st);
        }

        public static void DrawTurtle(Graphics g, float x, float y, float t, float facing, CritterPose p, float night)
        {
            var skin = Dim(0x9CA56B, night);
            var top = Dim(0x7E8A4F, night);
            var bottom = Dim(0x5B683A, night);
            var rim = Dim(0xB9A86E, night);
            var line = Dim(0x4C5832, night);
            var st = g.Save();
            g.TranslateTransform(x, y);
            Mirror(g, facing);
            float out1 = 1 - Smooth(0, 1, p.Hide);
            float a = Sin(p.Gait), b = Sin(p.Gait + Pi);
            foreach (var (lx, ph, far) in new[] { (-2.4f, b, true), (6, a, true), (-5, a, false), (3.4f, b, false) })
                FillEllipse(g, far ? Mix(skin, line, 0.3f) : skin, lx + ph * 0.9f * p.Walk * out1, -1.4f + (1 - out1) * 1.2f - Math.Max(0, ph) * 0.5f * p.Walk, 1.5f * out1 + 0.05f, 1.6f * out1 + 0.05f);
            if (out1 > 0.05f) FillPolygon(g, skin, new PointF(-7.4f, -2.6f), new PointF(-7.4f - 2.4f * out1, -1.6f), new PointF(-7.4f, -1.2f));
            float bob = Sin(t * 1.7f) * 0.35f + Sin(p.Gait * 2) * 0.25f * p.Walk;
            float hx = Lerp(5.5f, 10.2f, out1), hy = -3.6f + bob + p.Tilt;
            FillEllipse(g, skin, Lerp(5, 7.6f, out1), -2.6f + bob * 0.5f, 2.2f * out1 + 0.05f, 1.4f);
            FillEllipse(g, skin, hx, hy, 2.6f, 2.1f);
            if (out1 > 0.5f)
            {
                float blink = (t + 0.7f) % 4.3f < 0.15f ? 0.15f : 1;
                FillEllipse(g, Ink, hx + 1.1f, hy - 0.6f, 0.6f, 0.6f * blink);
                using (var mouth = new Pen(Mix(skin, Ink, 0.45f), 0.45f)) g.DrawArc(mouth, hx + 0.4f, hy - 0.4f, 2, 1.4f, 20, 100);
            }
            float sy = -1.6f + (1 - out1) * 1.1f;
            using (var dome = new GraphicsPath())
            {
                dome.AddArc(-8.6f, sy - 7.4f, 17.2f, 14.8f, 180, 180);
                dome.CloseFigure();
                using (var brush = new LinearGradientBrush(new RectangleF(-8.6f, sy - 8, 17.2f, 9), top, bottom, LinearGradientMode.Vertical)) g.FillPath(brush, dome);
                var clip = g.Save();
                g.SetClip(dome, CombineMode.Intersect);
                using (var pen = new Pen(line, 0.6f))
                {
                    var hex = new PointF[6];
                    for (int i = 0; i < 6; i++) hex[i] = new PointF(Cos(i * Pi / 3) * 2.8f, sy - 3.6f + Sin(i * Pi / 3) * 1.9f);
                    g.DrawPolygon(pen, hex);
                    foreach (var v in hex) g.DrawLine(pen, v.X, v.Y, v.X * 2.6f, sy - 3.6f + (v.Y - sy + 3.6f) * 2.6f);
                }
                FillEllipse(g, Alpha(Color.White, 0.25f * (1 - night)), -2, sy - 5.6f, 3.4f, 1.1f);
                g.Restore(clip);
            }
            FillEllipse(g, rim, 0, sy - 0.1f, 8.9f, 1.15f);
            g.Restore(st);
        }
    }
}
