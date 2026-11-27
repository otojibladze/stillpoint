using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    static class TouchArt
    {
        static readonly Color NightBlue = Hex(0x3A3E52);

        static Color Dim(Color c, float night, float k = 0.45f) => Mix(c, NightBlue, night * k);

        static PointF[] Offset(IList<PointF> pts, float d)
        {
            var result = new PointF[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var a = pts[Math.Max(0, i - 1)];
                var b = pts[Math.Min(pts.Count - 1, i + 1)];
                float dx = b.X - a.X, dy = b.Y - a.Y, len = (float)Math.Sqrt(dx * dx + dy * dy) + 0.0001f;
                result[i] = new PointF(pts[i].X - dy / len * d, pts[i].Y + dx / len * d);
            }
            return result;
        }

        public static void DrawStroke(Graphics g, IList<PointF> pts, float alpha, float night, bool winter)
        {
            if (pts.Count < 2 || alpha < 0.01f) return;
            var (dark, light) = GardenArt.RakeColors(night, winter);
            using (var darkPen = new Pen(Color.FromArgb((int)(dark.A * alpha), dark), 1.2f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var lightPen = new Pen(Color.FromArgb((int)(light.A * alpha), light), 0.9f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                foreach (float d in new[] { -2.4f, 0f, 2.4f })
                {
                    var line = Offset(pts, d);
                    g.DrawLines(darkPen, line);
                    var lit = Offset(pts, d - 0.7f);
                    for (int i = 0; i < lit.Length; i++) lit[i] = new PointF(lit[i].X - 0.3f, lit[i].Y - 0.4f);
                    g.DrawLines(lightPen, lit);
                }
        }

        static readonly Color[][] Petals =
        {
            new[] { Hex(0xE0566B), Hex(0xF08AA0) },
            new[] { Hex(0xF2C230), Hex(0xF7D96A) },
            new[] { Hex(0x9B7BD0), Hex(0xB9A0E6) },
            new[] { Hex(0xF6F7FA), Hex(0xDCE3EC) },
        };

        public static void DrawPlant(Graphics g, float x, float y, int kind, int stage, float wilt, float night, float wind, bool dormant, float t)
        {
            var dirt = Dim(Hex(0x9C8668), night, 0.8f);
            FillEllipse(g, Alpha(dirt, 0.45f), x, y + 0.5f, 3.6f, 1.1f);
            if (stage == 0)
            {
                FillEllipse(g, Mix(dirt, Hex(0x7E6A52), 0.4f), x, y - 0.6f, 3.2f, 1.5f);
                FillEllipse(g, Dim(Hex(0x6E5444), night), x + 0.4f, y - 1.4f, 0.8f, 0.55f);
                return;
            }
            var green = Dim(Mix(Hex(0x6E9A4E), Hex(0x8C7A52), wilt * 0.7f), night, 0.8f);
            var leaf = Dim(Mix(Hex(0x86AE62), Hex(0x9C8A5C), wilt * 0.7f), night, 0.8f);
            if (dormant)
            {
                using (var stub = RoundPen(Dim(Hex(0x7A6A52), night, 0.8f), 0.8f)) g.DrawLine(stub, x, y, x + 0.4f, y - 3.5f);
                FillEllipse(g, Dim(Hex(0xF6F8FA), night, 0.6f), x, y - 0.6f, 3.4f, 1.6f);
                return;
            }
            float tall = new[] { 1f, 1.55f, 0.95f, 0.7f }[kind];
            float h = (stage == 1 ? 3.5f : stage == 2 ? 8 : 12) * tall;
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(1.45f, 1.45f);
            g.RotateTransform(wind * 3 + wilt * 22);
            if (stage == 1)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    var l = g.Save();
                    g.TranslateTransform(0, -h);
                    g.RotateTransform(s * 58);
                    FillEllipse(g, leaf, 0, -1.3f, 0.95f, 1.6f);
                    g.Restore(l);
                }
                using (var stem = RoundPen(green, 0.7f)) g.DrawLine(stem, 0, 0, 0, -h);
                g.Restore(st);
                return;
            }
            float bend = kind == 3 ? 1 : 0;
            var top = new PointF(bend * 2.2f, -h);
            using (var stem = RoundPen(green, kind == 1 ? 1.1f : 0.8f))
            using (var path = new GraphicsPath())
            {
                Quad(path, PointF.Empty, new PointF(-0.6f + bend * 0.4f, -h * 0.55f), top);
                g.DrawPath(stem, path);
            }
            int leaves = kind == 1 ? 3 : 2;
            for (int i = 0; i < leaves; i++)
            {
                float ly = -h * (0.22f + i * 0.24f), side = i % 2 == 0 ? -1 : 1;
                var l = g.Save();
                g.TranslateTransform(0, ly);
                g.RotateTransform(side * (kind == 0 ? 30 : 62) + wilt * 25 * side);
                float lw = kind == 0 ? 0.9f : kind == 1 ? 1.6f : 1.1f, lh = kind == 0 ? 4.2f : kind == 1 ? 2.8f : 2.2f;
                FillEllipse(g, leaf, 0, -lh * 0.9f, lw, lh);
                g.Restore(l);
            }
            if (stage >= 3)
            {
                var c = Petals[kind];
                var petal = Dim(Mix(c[0], Hex(0x8C6A4E), wilt * 0.6f), night, 0.55f);
                var petal2 = Dim(Mix(c[1], Hex(0x9C7A5E), wilt * 0.6f), night, 0.55f);
                var head = g.Save();
                g.TranslateTransform(top.X, top.Y);
                g.RotateTransform(wilt * 35 + (kind == 3 ? 150 : 0));
                if (stage == 3)
                {
                    FillEllipse(g, leaf, 0, 0.4f, 1.2f, 1);
                    FillEllipse(g, petal, 0, -1.4f, 1.1f, 1.8f);
                }
                else if (kind == 0)
                {
                    FillEllipse(g, petal, -1.3f, -1.9f, 1.4f, 2.4f);
                    FillEllipse(g, petal, 1.3f, -1.9f, 1.4f, 2.4f);
                    FillEllipse(g, petal2, 0, -2.2f, 1.5f, 2.6f);
                    FillEllipse(g, Alpha(Color.White, 0.3f * (1 - night)), -0.5f, -3, 0.4f, 1);
                }
                else if (kind == 3)
                {
                    FillEllipse(g, petal2, -1.1f, 1.6f, 0.9f, 2);
                    FillEllipse(g, petal2, 1.1f, 1.6f, 0.9f, 2);
                    FillEllipse(g, petal, 0, 1.9f, 1, 2.2f);
                    FillEllipse(g, Dim(Hex(0x8CB06A), night, 0.6f), 0, -0.2f, 0.8f, 0.7f);
                }
                else
                {
                    int n = kind == 1 ? 11 : 14;
                    float r = kind == 1 ? 3.4f : 2.6f;
                    int shown = (int)Math.Round(n * (1 - wilt * 0.5f));
                    for (int i = 0; i < shown; i++)
                    {
                        float a = i / (float)n * 360 + t * 0.5f * (1 - wilt);
                        var p = g.Save();
                        g.RotateTransform(a);
                        FillEllipse(g, i % 2 == 0 ? petal : petal2, 0, -r * 0.62f, kind == 1 ? 0.95f : 0.55f, r * 0.5f);
                        g.Restore(p);
                    }
                    FillEllipse(g, Dim(kind == 1 ? Hex(0x6B4428) : Hex(0xE8C24A), night, 0.5f), 0, 0, kind == 1 ? 1.8f : 1.1f, kind == 1 ? 1.8f : 1.1f);
                }
                g.Restore(head);
            }
            g.Restore(st);
        }

        public static void DrawBubble(Graphics g, float cx, float cy, float k, float night, PointF from)
        {
            if (k < 0.01f) return;
            float a = Smooth(0, 0.2f, k);
            var fill = Alpha(Mix(Hex(0xF7F4FB), Hex(0xE4E2F2), night * 0.5f), 0.95f * a);
            var edge = Alpha(Hex(0xA9A4C4), 0.9f * a);
            float s = 0.45f + 0.55f * Smooth(0, 1, k);
            for (int i = 0; i < 3; i++)
            {
                float u = (i + 1) / 4f, r = (1.2f + i * 0.7f) * s;
                if (k < 0.2f + i * 0.15f) continue;
                float x = from.X + (cx + 18 - from.X) * u, y = from.Y + (cy + 18 - from.Y) * u;
                FillEllipse(g, fill, x, y, r, r);
            }
            float rx = 30 * s, ry = 20 * s;
            GraphicsPath Cloud(float grow)
            {
                var path = new GraphicsPath { FillMode = FillMode.Winding };
                path.AddEllipse(cx - rx - grow, cy - ry - grow, rx * 2 + grow * 2, ry * 2 + grow * 2);
                foreach (var (dx, dy, r) in new[] { (-0.75f, -0.55f, 0.42f), (-0.1f, -0.85f, 0.45f), (0.62f, -0.6f, 0.42f), (0.88f, 0.1f, 0.36f), (0.55f, 0.72f, 0.38f), (-0.45f, 0.75f, 0.4f), (-0.92f, 0.12f, 0.36f) })
                    path.AddEllipse(cx + dx * rx - r * rx - grow, cy + dy * ry - r * rx - grow, r * rx * 2 + grow * 2, r * rx * 2 + grow * 2);
                return path;
            }
            using (var outline = Cloud(0.7f))
            using (var b = new SolidBrush(edge)) g.FillPath(b, outline);
            using (var body = Cloud(0))
            using (var b = new SolidBrush(fill)) g.FillPath(b, body);
            using (var b = new SolidBrush(Alpha(Color.White, 0.5f * a))) g.FillEllipse(b, cx - rx * 0.55f, cy - ry * 0.75f, rx * 0.5f, ry * 0.35f);
        }

        public static void DrawDreamIcon(Graphics g, string motif, float x, float y, float t, float scale)
        {
            if (scale < 0.02f) return;
            var st = g.Save();
            g.TranslateTransform(x, y);
            float bob = Sin(t * 1.6f + x) * 1.2f;
            g.TranslateTransform(0, bob);
            g.ScaleTransform(scale, scale);
            var pose = new CritterPose();
            switch (motif)
            {
                case Visitors.Fox: g.ScaleTransform(0.55f, 0.55f); CritterArt.DrawFox(g, 0, 9, t, 1, new CritterPose { Sit = 1 }, 0); break;
                case Visitors.Owl: g.ScaleTransform(0.48f, 0.48f); CritterArt.DrawOwl(g, 0, 14, t, pose, 0); break;
                case Visitors.Cat: g.ScaleTransform(0.42f, 0.42f); DrawCat(g, 0, 10, t); break;
                case Visitors.Squirrel: g.ScaleTransform(0.65f, 0.65f); CritterArt.DrawSquirrel(g, 0, 7, t, 1, new CritterPose { Acorn = true }, 0); break;
                case Visitors.Rabbit: g.ScaleTransform(0.7f, 0.7f); WildArt.DrawRabbit(g, 0, 7, t, 1, new CritterPose { Sit = 1 }, 0); break;
                case Visitors.Mouse: g.ScaleTransform(0.85f, 0.85f); WildArt.DrawMouse(g, 0, 5, t, 1, new CritterPose { Sit = 1, Acorn = true }, 0); break;
                case Visitors.Bird:
                case Visitors.Fledged: g.ScaleTransform(0.85f, 0.85f); DrawBird(g, 0, 4, t, 1, true); break;
                case Visitors.Frog:
                case Visitors.Froglets: g.ScaleTransform(0.75f, 0.75f); DrawFrog(g, 0, 5, t, 0.5f + 0.5f * Sin(t * 3)); break;
                case Visitors.Turtle: g.ScaleTransform(0.7f, 0.7f); CritterArt.DrawTurtle(g, 0, 4, t, 1, pose, 0); break;
                case Visitors.Snail:
                case Visitors.Snails: g.ScaleTransform(0.65f, 0.65f); DrawSnail(g, 0, 6, t, 1); break;
                case Visitors.Butterfly: g.ScaleTransform(0.7f, 0.7f); DrawButterfly(g, 0, 0, t, false); break;
                case Visitors.Bee: CritterArt.DrawBee(g, 0, 0, t, 1, pose); break;
                case Visitors.Ladybug: g.ScaleTransform(1.1f, 1.1f); CritterArt.DrawLadybug(g, 0, 3, 0, t, pose); break;
                case Visitors.Moth: g.ScaleTransform(1.2f, 1.2f); FillEllipse(g, Hex(0xF4ECD6), 5, -3, 3.4f, 3.4f); CritterArt.DrawMoth(g, -2, 1, t, pose, 1); break;
                case Visitors.Bat: WildArt.DrawBat(g, 0, 0, Sin(t * 8), 1, 1.1f, 0); break;
                case Visitors.Worm: WildArt.DrawWorm(g, -3, 5, 1, t, 0.8f, 0.4f, 0); break;
                case Visitors.Dragonfly: GardenArt.DrawDragonfly(g, 0, 0, t, 1); break;
                case Visitors.Balloon: g.ScaleTransform(0.75f, 0.75f); StoryArt.DrawCord(g, new PointF(1, 2), new PointF(-1, 12), 1, 0); StoryArt.DrawBalloon(g, 0, -8, Sin(t) * 0.1f, 0); break;
                case Visitors.Plane: StoryArt.DrawPlane(g, 0, 0, -0.15f + Sin(t * 2) * 0.08f, 1, 0); break;
                case Visitors.Traveler: g.ScaleTransform(0.7f, 0.7f); StoryArt.DrawTraveler(g, 0, 8, StoryArt.TravelerPose(0), t, 0); break;
                case Visitors.Duckling: StoryArt.DrawDuck(g, 0, 4, 1, 0, 0, 0, true, 0); break;
                case Visitors.Hedgehog: g.ScaleTransform(0.8f, 0.8f); StoryArt.DrawHedgehog(g, 0, 4, 1, 0, 0, 0, 0, 0); break;
                case Visitors.Cubs: g.ScaleTransform(0.8f, 0.8f); WildArt.DrawCub(g, 0, 6, t, 1, new CritterPose { Sit = 1 }, 0.5f, 1, 0); break;
                case Visitors.Meteors:
                    foreach (var (mx, my) in new[] { (-6f, -5f), (2f, -1f), (6f, -7f) })
                        using (var b = new LinearGradientBrush(new PointF(mx - 6, my - 4), new PointF(mx, my), Alpha(Hex(0x9FB4E8), 0), Hex(0x6E86C8)))
                        using (var pen = new Pen(b, 1)) g.DrawLine(pen, mx - 6, my - 4, mx, my);
                    break;
                case Visitors.Comet:
                    using (var b = new LinearGradientBrush(new PointF(-9, 4), new PointF(4, -3), Alpha(Hex(0x9FC6F0), 0), Hex(0x9FC6F0)))
                    using (var pen = new Pen(b, 1.8f)) g.DrawLine(pen, -9, 4, 4, -3);
                    FillRadial(g, 4, -3, 2.6f, 2.6f, Hex(0xFFF4D6), Alpha(Hex(0xFFE0A0), 0));
                    break;
                case Visitors.Aurora:
                    foreach (var (c, oy) in new[] { (Hex(0x7FD6A6), 0f), (Hex(0x8FB8E8), 3f) })
                        using (var pen = new Pen(Alpha(c, 0.85f), 2.2f))
                        {
                            var pts = new PointF[7];
                            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(-10 + i * 3.4f, oy - 2 + Sin(t * 2 + i) * 2);
                            g.DrawCurve(pen, pts);
                        }
                    break;
                case Visitors.Eclipse:
                    FillRadial(g, 0, 0, 7, 7, Hex(0xFFD98A), Alpha(Hex(0xFFD98A), 0));
                    FillEllipse(g, Hex(0x2E2C38), 0, 0, 4.2f, 4.2f);
                    break;
                case Visitors.Rainbow:
                    var bands = new[] { Hex(0xE27C6B), Hex(0xE8C46A), Hex(0x9CC08A), Hex(0x8DB1D6) };
                    for (int i = 0; i < bands.Length; i++)
                        using (var pen = new Pen(bands[i], 1.4f)) g.DrawArc(pen, -9 + i * 1.4f, -3 + i * 1.4f, 18 - i * 2.8f, 18 - i * 2.8f, 180, 180);
                    break;
                case "chair":
                    var wood = Hex(0x9C6B4E);
                    using (var pen = RoundPen(wood, 1.4f))
                    {
                        g.DrawLine(pen, -5, -9, -5, 6);
                        g.DrawLine(pen, -5, 0, 5, 0);
                        g.DrawLine(pen, 5, 0, 5, 6);
                        g.DrawLine(pen, -2, 0, -2, 6);
                    }
                    using (var b = new SolidBrush(Hex(0xC46040))) g.FillRectangle(b, -5.6f, -1.6f, 11, 1.8f);
                    DrawSpirit(g, 0, -1, 4.6f, new SpiritPose { Bare = true, EyeScale = 1.3f, Mouth = Mouth.O, Brow = Brow.Worried }, t, false);
                    break;
                case "flower":
                    DrawPlant(g, 0, 7, 0, 4, 0, 0, Sin(t) * 0.3f, false, t);
                    break;
                case "moon":
                    FillEllipse(g, Hex(0xF4ECD6), 0, 0, 6, 6);
                    FillEllipse(g, Hex(0xE6DDC4), -2, 1, 1.2f, 1.2f);
                    FillEllipse(g, Hex(0xE6DDC4), 2, -2, 0.8f, 0.8f);
                    FillEllipse(g, Ink, -1.6f, -1, 0.5f, 0.5f);
                    FillEllipse(g, Ink, 1.6f, -1, 0.5f, 0.5f);
                    using (var pen = RoundPen(Ink, 0.4f)) Arc(g, pen, 0, 0.6f, 1.4f, Pi * 0.2f, Pi * 0.8f);
                    break;
                case "sea":
                    for (int i = 0; i < 3; i++)
                        using (var pen = RoundPen(Hex(0x7FA8C9), 1.1f))
                        {
                            var pts = new PointF[6];
                            for (int k = 0; k < pts.Length; k++) pts[k] = new PointF(-10 + k * 4, -3 + i * 3.2f + Sin(t * 2 + k + i) * 1);
                            g.DrawCurve(pen, pts);
                        }
                    DrawSpirit(g, 0, -4, 3.4f, new SpiritPose { Bare = true, Mouth = Mouth.Smile, EyeScale = 1.3f }, t, false);
                    break;
                default:
                    using (var b = new SolidBrush(Hex(0xFFFFFF)))
                    {
                        g.FillEllipse(b, -8, -3, 10, 6);
                        g.FillEllipse(b, -3, -6, 10, 8);
                        g.FillEllipse(b, 1, -3, 9, 6);
                    }
                    using (var pen = new Pen(Alpha(Hex(0xB4B0CC), 0.7f), 0.5f)) g.DrawArc(pen, -8, -6, 18, 9, 0, 180);
                    break;
            }
            g.Restore(st);
        }
    }
}
