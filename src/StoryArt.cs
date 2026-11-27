using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    static class StoryArt
    {
        public static readonly PointF PlaneRest = new PointF(112, 215), TravelerRest = new PointF(38, 217);
        public const float DuckY = 219, HedgehogY = 221, TravelerR = 15;
        public static readonly Color TravelerTint = Color.FromArgb(80, 74, 98, 132);

        public static void DrawCord(Graphics g, PointF from, PointF to, float sway, float night)
        {
            var mid = new PointF((from.X + to.X) / 2 + sway, (from.Y + to.Y) / 2);
            using (var path = new GraphicsPath())
            using (var pen = new Pen(Alpha(Mix(Hex(0x6E655D), Hex(0xA7A9BA), night), 0.85f), 0.6f))
            {
                Quad(path, from, mid, to);
                g.DrawPath(pen, path);
            }
        }

        public static void DrawBalloon(Graphics g, float x, float y, float tilt, float night)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(tilt * Deg);
            var red = Mix(Hex(0xD9534F), Hex(0x5A3A4A), night * 0.7f);
            var light = Mix(Hex(0xF08A80), Hex(0x7A5468), night * 0.7f);
            FillPolygon(g, Mix(red, Ink, 0.25f), new PointF(-1.4f, 10.6f), new PointF(1.4f, 10.6f), new PointF(0, 8.8f));
            FillRadial(g, 0, 0, 8.2f, 10.2f, light, red, new RectangleF(-9, -11, 18, 22));
            FillEllipse(g, Alpha(Color.White, 0.55f * (1 - night * 0.5f)), -3, -4.5f, 1.6f, 2.6f);
            g.Restore(st);
        }

        public static void DrawPlane(Graphics g, float x, float y, float angle, float facing, float night)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(facing < 0 ? -1 : 1, 1);
            g.RotateTransform(angle * Deg);
            var paper = Mix(Hex(0xF7F5F0), Hex(0x7D8196), night);
            var fold = Mix(Hex(0xD9D4CB), Hex(0x5D6074), night);
            FillPolygon(g, fold, new PointF(9, 0), new PointF(-5, 0.4f), new PointF(-7, 3.4f));
            FillPolygon(g, paper, new PointF(9, 0), new PointF(-8, -3.6f), new PointF(-5, 0.4f));
            using (var pen = new Pen(Mix(Hex(0xB8B2A7), Hex(0x4C4F62), night), 0.5f)) g.DrawLine(pen, 9, 0, -5, 0.4f);
            g.Restore(st);
        }

        public static void DrawDuck(Graphics g, float x, float y, float facing, float gait, float walk, float night, bool baby, float peep)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform((facing < 0 ? -1 : 1) * (baby ? 0.55f : 1), baby ? 0.55f : 1);
            g.RotateTransform(Sin(gait) * 7 * walk);
            var body = Mix(baby ? Hex(0xF4D35E) : Hex(0xF4F1EA), Hex(0x4E5266), night * 0.8f);
            var shade = Mix(baby ? Hex(0xDDB443) : Hex(0xD6CFC2), Hex(0x3C3F52), night * 0.8f);
            var orange = Mix(Hex(0xE8913A), Hex(0x6E5048), night * 0.7f);
            FillEllipse(g, Color.FromArgb(40, 60, 45, 35), 0, 0.3f, 8, 1.4f);
            float step = Sin(gait) * 1.6f * walk;
            using (var leg = RoundPen(orange, 1.1f))
            {
                g.DrawLine(leg, -1.5f, -3, -1.5f + step, 0);
                g.DrawLine(leg, 1.5f, -3, 1.5f - step, 0);
            }
            FillPolygon(g, shade, new PointF(-6.5f, -7), new PointF(-11, -10.5f), new PointF(-8, -4));
            FillEllipse(g, body, 0, -6.5f, 8.5f, 5.4f);
            FillEllipse(g, shade, -1.5f, -6.4f, 4.6f, 2.6f);
            float hr = baby ? 4.6f : 3.7f, hx = baby ? 5 : 6, hy = baby ? -12.5f : -13.5f;
            FillEllipse(g, body, hx - 1.5f, hy + 3.5f, 2.6f, 3.6f);
            FillEllipse(g, body, hx, hy, hr, hr);
            float open = Clamp(peep, 0, 1);
            FillPolygon(g, orange, new PointF(hx + hr - 0.8f, hy - 0.4f), new PointF(hx + hr + 3.4f, hy + 0.3f - open), new PointF(hx + hr - 0.8f, hy + 1.2f));
            if (open > 0.05f) FillPolygon(g, Mix(orange, Ink, 0.2f), new PointF(hx + hr - 0.8f, hy + 0.9f), new PointF(hx + hr + 3, hy + 1.2f + open), new PointF(hx + hr - 0.6f, hy + 1.8f));
            FillEllipse(g, Ink, hx + hr * 0.3f, hy - hr * 0.25f, 0.75f, 0.85f);
            FillEllipse(g, Alpha(Color.White, 0.8f), hx + hr * 0.3f + 0.25f, hy - hr * 0.25f - 0.3f, 0.25f, 0.25f);
            g.Restore(st);
        }

        public static void DrawHedgehog(Graphics g, float x, float y, float facing, float gait, float walk, float curl, float sniff, float night)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform((facing < 0 ? -1 : 1) * 1.2f, 1.2f);
            var spine = Mix(Hex(0x7A604C), Hex(0x4A4452), night * 0.5f);
            var tip = Mix(Hex(0xC2A684), Hex(0x8A8090), night * 0.5f);
            var face = Mix(Hex(0xE2C9A4), Hex(0x9A8E92), night * 0.5f);
            float c = Clamp(curl, 0, 1), rx = 9 - 2.2f * c, ry = 5.6f + 1.2f * c, cy = -5.2f - 1 * c;
            FillEllipse(g, Color.FromArgb(40, 60, 45, 35), 0, 0.3f, 9, 1.4f);
            if (c < 0.9f)
                using (var leg = RoundPen(Mix(face, spine, 0.4f), 1.2f))
                {
                    float step = Sin(gait) * 1.4f * (1 - c) * walk;
                    g.DrawLine(leg, -4, -2, -4 + step, 0);
                    g.DrawLine(leg, 4, -2, 4 - step, 0);
                }
            if (c < 0.95f)
            {
                float k = 1 - c;
                FillEllipse(g, face, rx * 0.72f + 2.6f * k, -3.6f, 4 * k, 2.6f * k);
                FillEllipse(g, Ink, rx * 0.72f + 6.4f * k + sniff * 0.5f, -3.9f + sniff * 0.3f, 1 * k, 0.9f * k);
                FillEllipse(g, Ink, rx * 0.72f + 2.2f * k, -4.9f, 0.6f * k, 0.6f * k);
            }
            var spikes = new List<PointF>();
            for (int i = 0; i <= 26; i++)
            {
                float a = Pi * 0.92f + i / 26f * Pi * (1.16f + 0.84f * c);
                float r = i % 2 == 0 ? 1.22f : 0.96f;
                spikes.Add(new PointF(Cos(a) * rx * r, cy + Sin(a) * ry * r));
            }
            if (c < 0.5f) spikes.Add(new PointF(rx * 0.85f, cy + ry * 0.55f));
            FillPolygon(g, spine, spikes.ToArray());
            FillEllipse(g, tip, -rx * 0.2f, cy - ry * 0.35f, rx * 0.55f, ry * 0.4f);
            using (var pen = new Pen(Alpha(tip, 0.7f), 0.5f))
                for (int i = 0; i < 6; i++)
                {
                    float a = Pi * 1.15f + i * 0.13f * Pi;
                    g.DrawLine(pen, Cos(a) * rx * 0.4f, cy + Sin(a) * ry * 0.4f, Cos(a) * rx * 1.05f, cy + Sin(a) * ry * 1.05f);
                }
            g.Restore(st);
        }

        public static void DrawTraveler(Graphics g, float x, float y, SpiritPose pose, float t, float night)
        {
            var stick = Mix(Hex(0x8A6A4E), Hex(0x34323E), night * 0.8f);
            var cloth = Mix(Hex(0xC9503F), Hex(0x5A3A48), night * 0.7f);
            float lift = -pose.Dy, sway = pose.Tilt * 20;
            using (var pen = RoundPen(stick, 1.1f)) g.DrawLine(pen, x + 4, y - 6 - lift, x - 10 + sway, y - 30 - lift);
            FillEllipse(g, cloth, x - 11 + sway, y - 31 - lift, 4.6f, 4);
            FillEllipse(g, Alpha(Color.White, 0.75f), x - 12.5f + sway, y - 32 - lift, 0.7f, 0.7f);
            FillEllipse(g, Alpha(Color.White, 0.75f), x - 9.5f + sway, y - 30 - lift, 0.7f, 0.7f);
            FillPolygon(g, Mix(cloth, Ink, 0.2f), new PointF(x - 12 + sway, y - 35 - lift), new PointF(x - 9 + sway, y - 36.5f - lift), new PointF(x - 10 + sway, y - 34 - lift));
            DrawSpirit(g, x, y, TravelerR, pose, t);
        }

        public static SpiritPose TravelerPose(float night) => new SpiritPose { Bare = true, Tint = TravelerTint, Blush = 0.4f, Mouth = Mouth.Smile, EyeScale = 1.15f, Night = night, LookX = 1 };

        public static void DrawStill(Graphics g, ICollection<string> seen, PointF tip, float night)
        {
            if (seen.Contains(Visitors.Traveler))
            {
                var p = TravelerPose(night);
                p.Eyes = Eyes.Happy;
                DrawTraveler(g, TravelerRest.X, TravelerRest.Y, p, 0, night);
            }
            if (seen.Contains(Visitors.Plane)) DrawPlane(g, PlaneRest.X, PlaneRest.Y, 0.06f, -1, night);
            if (seen.Contains(Visitors.Duckling))
            {
                DrawDuck(g, 58, DuckY, 1, 0, 0, night, false, 0);
                DrawDuck(g, 76, DuckY + 1, -1, 0, 0, night, true, 0);
            }
            if (seen.Contains(Visitors.Hedgehog)) DrawHedgehog(g, 128, HedgehogY, -1, 0, 0, 0, 0, night);
            if (seen.Contains(Visitors.Balloon))
            {
                var at = new PointF(tip.X + 34, tip.Y - 15);
                DrawCord(g, tip, new PointF(at.X, at.Y + 10.5f), 1.5f, night);
                DrawBalloon(g, at.X, at.Y, 0.08f, night);
            }
        }
    }
}
