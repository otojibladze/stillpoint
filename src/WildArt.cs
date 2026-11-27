using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    static class WildArt
    {
        static readonly Color NightBlue = Hex(0x3A3E52);

        static Color Dim(int rgb, float night, float k = 0.4f) => Mix(Hex(rgb), NightBlue, night * k);

        static float L(float a, float b, float k) => a + (b - a) * k;

        static void Ellipse(Graphics g, Color c, float x, float y, float rx, float ry, float angle)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle);
            FillEllipse(g, c, 0, 0, rx, ry);
            g.Restore(st);
        }

        public static void DrawRabbit(Graphics g, float x, float y, float t, float facing, CritterPose p, float night)
        {
            var fur = Dim(0xA08A74, night);
            var shade = Dim(0x7F6C5B, night);
            var light = Dim(0xD9C8B2, night);
            var belly = Dim(0xECE2D2, night);
            var inner = Dim(0xD8A79B, night);
            var tail = Dim(0xF6F3EE, night, 0.3f);
            float s = Clamp(p.Sit, 0, 1), r = Clamp(p.Run, 0, 1), f = Smooth(0, 1, p.Hide), gr = Clamp(p.Curl, 0, 1);
            float B(float loaf, float sit, float hop, float flop) => L(L(L(loaf, sit, s), hop, r), flop, f);
            var st = g.Save();
            g.TranslateTransform(x, y);
            if (facing < 0) g.ScaleTransform(-1, 1);
            FillEllipse(g, Color.FromArgb((int)(45 * (1 - r * 0.6f)), 60, 45, 35), 0, 0.3f, B(8.5f, 6.5f, 9, 10), 1.4f);
            Ellipse(g, shade, B(-3.4f, -2.8f, -9.4f, -9.6f), B(-0.9f, -0.9f, -2.8f, -1.4f), B(4.2f, 3.8f, 4, 3.8f), 1.15f, B(0, 0, 18, 6));
            Ellipse(g, fur, B(-3.6f, -2.6f, -4.6f, -4.2f), B(-5, -5.2f, -5.6f, -3.8f), B(5.2f, 5, 4.6f, 5.2f), B(4.8f, 5, 4.2f, 3.6f), 0);
            Ellipse(g, fur, B(0, 0.6f, 0.8f, 0), B(-5.2f, -9.2f, -5.8f, -3.6f), B(7.8f, 4.4f, 9.2f, 8.8f), B(4.9f, 6.6f, 4, 3.6f), B(0, -8, -10, 0));
            Ellipse(g, belly, B(3.6f, 2.6f, 4.6f, 2), B(-3.4f, -8.4f, -4, -2.2f), B(3.2f, 2.2f, 3.4f, 4.4f), B(2.4f, 3.8f, 2, 1.4f), B(0, -8, -10, 0));
            float paw = gr * Sin(t * 10) * 0.6f;
            var head = new PointF(B(6.6f, 3.8f, 8.2f, 7.6f), B(-8.4f, -16.2f, -9.2f, -3.8f));
            float nib = Clamp(p.Tilt, 0, 1) * (1 - s) * (1 - f);
            head = new PointF(head.X - gr * 1.2f + nib * 1.2f, head.Y + gr * 1.6f + nib * 4.2f);
            for (int i = 0; i < 2; i++)
            {
                float px = B(5.4f + i * 1.5f, 3.4f + i * 0.9f, 8.6f + i * 1.2f, 7 + i * 1.6f), py = B(-0.8f, -8.8f + i * 0.4f, -2.4f, -1.2f);
                px = L(px, head.X + 1.6f + i * 0.8f, gr);
                py = L(py, head.Y + 2.6f + paw * (i == 0 ? 1 : -1), gr);
                FillEllipse(g, light, px, py, B(1.5f, 1.1f, 1.6f, 1.5f), B(0.9f, 0.8f, 0.8f, 0.8f));
            }
            FillEllipse(g, tail, B(-8.4f, -6.4f, -8.8f, -8.6f), B(-6, -4.2f, -7.6f, -3.8f), 2, 1.9f);
            var hs = g.Save();
            g.TranslateTransform(head.X, head.Y);
            g.RotateTransform(B(0, -6, 8, 22) + gr * 24 + nib * 30);
            float earBack = Clamp(p.Ear, 0, 1), swivel = Clamp(p.Turn, -1, 1);
            float earAngle = L(L(-14 + swivel * 14, -60, f), -78, earBack);
            foreach (int e in new[] { 0, 1 })
            {
                var es = g.Save();
                g.TranslateTransform(-1 + e * 1.3f, -2.4f);
                g.RotateTransform(earAngle + (e == 0 ? -7 : 4));
                FillEllipse(g, e == 0 ? shade : fur, 0, -4.4f, 1.6f, 4.8f);
                if (e == 1) FillEllipse(g, inner, 0.2f, -4.2f, 0.75f, 3.6f);
                g.Restore(es);
            }
            FillEllipse(g, fur, 0, 0, 3.7f, 3.3f);
            float chew = p.Yawn * Sin(t * 16) * 0.3f, twitch = p.Flick * Sin(t * 24) * 0.3f;
            FillEllipse(g, fur, 1.4f, 1.1f + chew * 0.5f, 2.5f, 2.1f);
            FillEllipse(g, light, 2.7f, 1 + chew, 1.4f, 1.15f);
            FillEllipse(g, Dim(0xD08A86, night), 3.8f, 0.4f + twitch, 0.55f, 0.45f);
            float open = 1 - Clamp(Math.Max(p.Blink, f), 0, 1);
            if (open > 0.15f)
            {
                FillEllipse(g, Ink, 1, -1, 0.95f, 0.95f * open);
                FillEllipse(g, Alpha(Color.White, 0.85f), 1.3f, -1.35f, 0.32f, 0.32f * open);
            }
            else using (var lid = RoundPen(Mix(fur, Ink, 0.5f), 0.45f)) g.DrawArc(lid, 0.2f, -1.6f, 1.6f, 1.1f, 20, 140);
            g.Restore(hs);
            g.Restore(st);
        }

        public static void DrawRabbitPrints(Graphics g, IEnumerable<PointF> prints, float alpha, float night)
        {
            if (alpha < 0.01f) return;
            var c = Alpha(Mix(Hex(0x8C98AE), Hex(0x4A5066), night), 0.55f * alpha);
            foreach (var q in prints)
            {
                FillEllipse(g, c, q.X - 1.6f, q.Y - 0.6f, 1.5f, 0.6f);
                FillEllipse(g, c, q.X - 1.4f, q.Y + 0.7f, 1.5f, 0.6f);
                FillEllipse(g, c, q.X + 1.8f, q.Y, 0.6f, 0.5f);
                FillEllipse(g, c, q.X + 3.2f, q.Y + 0.3f, 0.6f, 0.5f);
            }
        }

        public static void DrawMouse(Graphics g, float x, float y, float t, float facing, CritterPose p, float night)
        {
            var fur = Dim(0x9A8E84, night, 0.12f);
            var shade = Dim(0x766B62, night, 0.12f);
            var belly = Dim(0xE2D8CC, night, 0.12f);
            var pink = Dim(0xE6B4AE, night, 0.12f);
            float s = Clamp(p.Sit, 0, 1), r = Clamp(p.Run, 0, 1);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(facing < 0 ? -1.3f : 1.3f, 1.3f);
            FillEllipse(g, Color.FromArgb(40, 40, 32, 28), 0, 0.2f, 5, 0.9f);
            float wag = Sin(t * 3.4f) * 0.8f * (1 - r);
            using (var tail = RoundPen(pink, 0.55f))
            using (var path = new GraphicsPath())
            {
                path.AddBezier(new PointF(L(-4.2f, -2.4f, s), L(-1.6f, -0.6f, s)), new PointF(L(-8, -6, s), L(-0.4f, 0, s)), new PointF(L(-11.4f, -9, s), L(-3.6f + wag, -0.2f, s)), new PointF(L(-14.4f, -11.6f, s), L(-2.2f + wag * 1.4f, -1.4f, s)));
                g.DrawPath(tail, path);
            }
            float legs = r * Sin(p.Gait) * 1.2f;
            FillEllipse(g, pink, -2.6f + legs, -0.3f, 1, 0.45f);
            FillEllipse(g, pink, 2.2f - legs, -0.3f, 0.9f, 0.45f);
            Ellipse(g, fur, L(-0.6f, 0, s), L(-2.6f, -4.2f, s), L(4.4f + r * 0.8f, 2.7f, s), L(2.7f - r * 0.3f, 3.7f, s), L(0, -12, s));
            Ellipse(g, belly, L(1.4f, 1.4f, s), L(-1.6f, -3.6f, s), L(2.4f, 1.4f, s), L(1.2f, 2.6f, s), L(0, -12, s));
            var head = new PointF(L(3.4f, 1.8f, s), L(-3.2f, -8.6f, s));
            float sniff = p.Flick * Sin(t * 30) * 0.25f;
            var hs = g.Save();
            g.TranslateTransform(head.X, head.Y);
            g.RotateTransform(L(0, -18, s));
            FillEllipse(g, shade, -1.1f, -2.1f, 1.5f, 1.5f);
            FillEllipse(g, fur, 0, 0, 2.6f, 2.1f);
            FillPolygon(g, fur, new PointF(1.2f, -1.1f), new PointF(3.6f + sniff, 0.3f), new PointF(1.4f, 1.4f));
            FillEllipse(g, Dim(0xD9928E, night, 0.3f), 3.6f + sniff, 0.3f, 0.45f, 0.42f);
            FillEllipse(g, fur, -0.4f, -1.9f, 1.75f, 1.75f);
            FillEllipse(g, pink, -0.3f, -1.8f, 1, 1);
            float open = 1 - Clamp(p.Blink, 0, 1);
            FillEllipse(g, Ink, 1.3f, -0.5f, 0.55f, 0.55f * Math.Max(0.15f, open));
            if (open > 0.5f) FillEllipse(g, Alpha(Color.White, 0.85f), 1.45f, -0.7f, 0.18f, 0.18f);
            using (var whisker = new Pen(Alpha(Mix(belly, Color.White, 0.3f), 0.75f), 0.22f))
                for (int i = -1; i <= 1; i++)
                    g.DrawLine(whisker, 3.1f, 0.4f, 5.4f + sniff * 2, 0.4f + i * 0.9f + sniff * i);
            g.Restore(hs);
            if (s > 0.3f)
            {
                var paws = new PointF(head.X + 0.8f, head.Y + 3);
                if (p.Acorn) FillEllipse(g, Dim(0xC9A46A, night, 0.3f), paws.X + 0.5f, paws.Y - 0.3f, 0.95f, 0.7f);
                FillEllipse(g, pink, paws.X, paws.Y + 0.3f, 0.6f, 0.45f);
                FillEllipse(g, pink, paws.X + 1, paws.Y + 0.4f, 0.6f, 0.45f);
            }
            g.Restore(st);
        }

        public static void DrawBat(Graphics g, float x, float y, float flap, float facing, float scale, float night)
        {
            var body = Mix(Hex(0x3A3340), Hex(0x221F28), night);
            var wing = Alpha(Mix(Hex(0x4A4150), Hex(0x2C2833), night), 0.96f);
            var bone = Mix(Hex(0x2A252F), Hex(0x18161C), night);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(scale * (facing < 0 ? -1 : 1), scale);
            float f = Clamp(flap, -1, 1);
            foreach (int s in new[] { -1, 1 })
            {
                var pts = new[]
                {
                    new PointF(s * 0.9f, -1), new PointF(s * 4.6f, -2.6f - 2.8f * f), new PointF(s * 9.6f, -1.8f - 4.6f * f),
                    new PointF(s * 8.2f, 0.8f - 2.6f * f), new PointF(s * 6.6f, 0 - 1.8f * f), new PointF(s * 5.2f, 1.7f - 1.3f * f),
                    new PointF(s * 3.6f, 0.6f - 0.7f * f), new PointF(s * 1.2f, 1.6f),
                };
                FillPolygon(g, wing, pts);
                using (var pen = new Pen(bone, 0.35f))
                {
                    g.DrawLine(pen, pts[0], pts[2]);
                    g.DrawLine(pen, pts[1], pts[3]);
                    g.DrawLine(pen, pts[1], pts[5]);
                }
            }
            FillEllipse(g, body, 0, 0.2f, 1.6f, 2.3f);
            FillEllipse(g, body, 0, -2.2f, 1.35f, 1.25f);
            FillPolygon(g, body, new PointF(-1.1f, -2.6f), new PointF(-1.3f, -4.4f), new PointF(-0.2f, -3.2f));
            FillPolygon(g, body, new PointF(1.1f, -2.6f), new PointF(1.3f, -4.4f), new PointF(0.2f, -3.2f));
            g.Restore(st);
        }

        public static void DrawWorm(Graphics g, float x, float y, float facing, float t, float outK, float crawl, float night)
        {
            FillEllipse(g, Alpha(Mix(Hex(0x5E4A38), Hex(0x1E1F28), night), 0.85f), x, y + 0.3f, 2.3f, 0.85f);
            if (outK < 0.02f) return;
            var body = Dim(0xC98A7C, night, 0.5f);
            var ring = Alpha(Dim(0x9E6458, night, 0.5f), 0.55f);
            var band = Dim(0xDDA08E, night, 0.5f);
            int n = 10;
            float len = 15 * outK, seg = len / n;
            var pts = new List<PointF> { new PointF(x, y) };
            float c = Smooth(0, 1, crawl);
            for (int i = 1; i <= n; i++)
            {
                float u = i / (float)n;
                float probe = -Pi / 2 + Sin(t * 1.7f + u * 2.4f) * 0.35f * u + 0.25f * u * facing;
                float lie = (facing > 0 ? 0 : Pi) + Sin(t * 2.2f - u * 6) * 0.22f - (i == 1 ? 0.9f * facing : 0);
                float a = probe + (lie - probe) * c;
                var prev = pts[pts.Count - 1];
                pts.Add(new PointF(prev.X + Cos(a) * seg, prev.Y + Sin(a) * seg));
            }
            for (int i = 1; i < pts.Count; i++)
                if (pts[i].Y > y - 0.6f) pts[i] = new PointF(pts[i].X, y - 0.6f - 0.25f * Math.Abs(Sin(t * 3 + i)));
            using (var pen = RoundPen(body, 1.7f)) g.DrawLines(pen, pts.ToArray());
            int bandAt = Math.Max(1, (int)(pts.Count * 0.68f));
            if (bandAt < pts.Count)
                using (var pen = RoundPen(band, 1.9f)) g.DrawLine(pen, pts[bandAt - 1], pts[bandAt]);
            using (var pen = new Pen(ring, 0.3f))
                for (int i = 1; i < pts.Count - 1; i++)
                {
                    float dx = pts[i + 1].X - pts[i - 1].X, dy = pts[i + 1].Y - pts[i - 1].Y, d = (float)Math.Sqrt(dx * dx + dy * dy) + 0.001f;
                    float nx = -dy / d * 0.8f, ny = dx / d * 0.8f;
                    g.DrawLine(pen, pts[i].X - nx, pts[i].Y - ny, pts[i].X + nx, pts[i].Y + ny);
                }
            var tip = pts[pts.Count - 1];
            FillEllipse(g, Alpha(Color.White, 0.25f * (1 - night)), tip.X, tip.Y - 0.3f, 0.5f, 0.3f);
        }

        public static void DrawCastings(Graphics g, float x, float y, float alpha, float night)
        {
            if (alpha < 0.02f) return;
            var c = Alpha(Mix(Hex(0x8F7B62), Hex(0x2E2F3A), night), 0.8f * alpha);
            FillEllipse(g, c, x + 2.6f, y - 0.1f, 1.7f, 0.8f);
            FillEllipse(g, c, x + 2.9f, y - 0.8f, 1.2f, 0.6f);
            FillEllipse(g, c, x + 2.4f, y - 1.3f, 0.7f, 0.45f);
            FillEllipse(g, Alpha(Mix(Hex(0x5E4A38), Hex(0x1E1F28), night), 0.6f * alpha), x, y + 0.3f, 1.6f, 0.6f);
        }

        public static void DrawCub(Graphics g, float x, float y, float t, float facing, CritterPose p, float age, float scale, float night)
        {
            var fur = Mix(Dim(0x7F6A5E, night, 0.3f), Dim(0xC9743B, night, 0.3f), age);
            var deep = Mix(Dim(0x5E4C44, night, 0.3f), Dim(0xA45A2C, night, 0.3f), age);
            var cream = Mix(Dim(0xB9A696, night, 0.3f), Dim(0xF0E2CB, night, 0.35f), age);
            var dark = Dim(0x463631, night, 0.2f);
            float s = Clamp(p.Sit, 0, 1);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(scale * (facing < 0 ? -1 : 1), scale);
            FillEllipse(g, Color.FromArgb(40, 60, 45, 35), 0, 0.3f, 7, 1.2f);
            if (Math.Abs(p.Turn) > 0.001f || p.Tilt != 0)
            {
                g.TranslateTransform(0, -5);
                g.RotateTransform((p.Turn + p.Tilt) * Deg);
                g.TranslateTransform(0, 5);
            }
            float swing = Clamp(p.Run, 0, 1) * Sin(p.Gait) * 1.8f;
            using (var tail = RoundPen(deep, 3.6f))
            using (var path = new GraphicsPath())
            {
                float wag = Sin(t * 5 + x) * 1.2f;
                path.AddBezier(new PointF(-5, L(-5.6f, -2.6f, s)), new PointF(-8.5f, L(-6.4f, -1.4f, s)), new PointF(-10.5f, L(-4.4f + wag, -0.8f, s)), new PointF(-12, L(-6 + wag, -1, s)));
                g.DrawPath(tail, path);
            }
            if (age > 0.5f) FillEllipse(g, cream, -12, L(-6, -1, s) + Sin(t * 5 + x) * 1.2f * (1 - s), 1.4f, 1.2f);
            using (var leg = RoundPen(dark, 1.8f))
            {
                g.DrawLine(leg, -3.4f, -3.6f, -3.4f - swing, -0.2f);
                g.DrawLine(leg, -1.6f, -3.6f, -1.6f + swing, -0.2f);
                g.DrawLine(leg, L(3, 2.4f, s), L(-3.6f, -6, s), L(3 + swing, 2.6f, s), -0.2f);
                g.DrawLine(leg, L(4.6f, 3.8f, s), L(-3.6f, -6, s), L(4.6f - swing, 3.8f, s), -0.2f);
            }
            Ellipse(g, fur, L(0, -0.6f, s), L(-5.4f, -5.6f, s), L(6, 4, s), L(3.8f, 5, s), L(0, -30, s));
            Ellipse(g, cream, L(2.6f, 1, s), L(-4, -6, s), L(2.6f, 1.6f, s), L(1.8f, 3, s), L(0, -30, s));
            var head = new PointF(L(6, 3.4f, s), L(-9.6f, -13.8f, s));
            var hs = g.Save();
            g.TranslateTransform(head.X, head.Y);
            g.RotateTransform(p.Puff * 25);
            FillPolygon(g, deep, new PointF(-2.4f, -2.2f), new PointF(-2.2f, -7.4f), new PointF(0.4f, -2.8f));
            FillPolygon(g, fur, new PointF(0.4f, -2.8f), new PointF(2.6f, -7.2f), new PointF(3.2f, -1.6f));
            FillPolygon(g, dark, new PointF(0.9f, -3.2f), new PointF(2.4f, -6), new PointF(2.6f, -2.2f));
            FillEllipse(g, fur, 0, 0, 4, 3.6f);
            FillEllipse(g, cream, 2.6f, 1.1f, 2.2f, 1.3f);
            FillEllipse(g, dark, 4.4f, 0.6f, 0.65f, 0.55f);
            float open = 1 - Clamp(p.Blink, 0, 1);
            if (open > 0.15f)
            {
                FillEllipse(g, Ink, 1.4f, -0.9f, 0.85f, 0.9f * open);
                FillEllipse(g, Alpha(Color.White, 0.85f), 1.7f, -1.25f, 0.3f, 0.3f * open);
            }
            else using (var lid = RoundPen(Ink, 0.45f)) g.DrawArc(lid, 0.6f, -1.6f, 1.6f, 1.1f, 20, 140);
            if (p.Yawn > 0.05f) FillEllipse(g, Dim(0x5A2E2A, night, 0.2f), 2.8f, 2.1f, 1.1f * p.Yawn, 0.9f * p.Yawn);
            g.Restore(hs);
            g.Restore(st);
        }

        static readonly PointF[] Eggs = BuildEggs();

        static PointF[] BuildEggs()
        {
            var rnd = new Random(5);
            var list = new PointF[18];
            for (int i = 0; i < list.Length; i++)
            {
                float a = (float)rnd.NextDouble() * Pi * 2, r = (float)Math.Sqrt(rnd.NextDouble());
                list[i] = new PointF(Cos(a) * r * 5.6f, Sin(a) * r * 1.8f);
            }
            return list;
        }

        public static void DrawSpawn(Graphics g, float x, float y, float t, float progress, float night)
        {
            var jelly = Alpha(Mix(Hex(0xDCE6E4), Hex(0x6C7488), night), 0.55f);
            var rim = Alpha(Mix(Hex(0xF4F8F6), Hex(0x8A92A6), night), 0.5f);
            var dot = Alpha(Mix(Hex(0x26261E), Hex(0x15161C), night), 0.9f);
            for (int i = 0; i < Eggs.Length; i++)
            {
                float ex = x + Eggs[i].X, ey = y + Eggs[i].Y + Sin(t * 1.1f + i) * 0.15f;
                FillEllipse(g, jelly, ex, ey, 1.25f, 1.15f);
                FillEllipse(g, rim, ex - 0.4f, ey - 0.45f, 0.4f, 0.3f);
                Ellipse(g, dot, ex + 0.1f, ey, 0.42f + 0.35f * progress, 0.42f, (i * 47) % 180);
            }
        }

        public static void DrawTadpole(Graphics g, float x, float y, float angle, float t, float size, float legs, float night)
        {
            var body = Alpha(Mix(Hex(0x3E3A2C), Hex(0x1A1B22), night), 0.82f);
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle * Deg);
            g.ScaleTransform(size, size);
            var tail = new PointF[5];
            for (int i = 0; i < tail.Length; i++)
            {
                float u = i / 4f;
                tail[i] = new PointF(-1.2f - u * 3.4f, Sin(t * 14 - u * 4) * 0.7f * u);
            }
            using (var pen = new Pen(Alpha(body, 0.6f), 0.7f)) g.DrawLines(pen, tail);
            if (legs > 0.05f)
                using (var leg = new Pen(body, 0.45f))
                {
                    g.DrawLine(leg, -1, 0.5f, -1.8f, 0.6f + 1.1f * legs);
                    g.DrawLine(leg, -1, -0.5f, -1.8f, -0.6f - 1.1f * legs);
                }
            FillEllipse(g, body, 0, 0, 1.5f, 1.1f);
            g.Restore(st);
        }

        public static void DrawSeedling(Graphics g, float x, float y, float growth, float night, float wind)
        {
            float h = 2.5f + 6 * growth;
            var green = Mix(Hex(0x7FA35F), Hex(0x3D4B48), night * 0.8f);
            FillEllipse(g, Alpha(Mix(Hex(0x9C8668), Hex(0x2C2E3A), night), 0.3f), x, y + 0.4f, 3, 0.8f);
            if (growth < 0.5f) CritterArt.DrawAcorn(g, x - 1.6f, y - 0.6f, 0.6f, night);
            float tip = x + 0.5f + wind * 0.7f;
            using (var stem = RoundPen(Mix(Hex(0x6E5A42), Hex(0x36423F), night * 0.8f), 0.8f)) g.DrawLine(stem, x, y, tip, y - h);
            int pairs = growth > 0.55f ? 2 : 1;
            for (int k = 0; k < pairs; k++)
            {
                float ly = y - h + k * h * 0.4f, size = (1 - k * 0.25f) * (0.8f + 0.6f * growth);
                foreach (int s in new[] { -1, 1 })
                {
                    var st = g.Save();
                    g.TranslateTransform(tip - (tip - x) * k * 0.4f, ly);
                    g.RotateTransform(s * 55 + wind * 8);
                    FillEllipse(g, green, 0, -1.6f * size, 1.05f * size, 1.9f * size);
                    g.Restore(st);
                }
            }
        }
    }
}
