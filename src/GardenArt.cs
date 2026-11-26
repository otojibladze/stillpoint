using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed class SceneLook
    {
        public float Night, Clouds, Rainbow, Snowman, NewestRake = 1, Dusk, MoonPhase = 0.25f, Wet, FadeAlpha;
        public Season Season = Season.Autumn;
        public int RakeLines = 2, FadedRakes;
        public Keepsake Keepsakes;
        public bool SnowGround, SnowmanMelted, Southern;

        public long Key(int px)
        {
            long k = px;
            k = k * 49 + Q(Night, 48);
            k = k * 4 + (int)Season;
            k = k * 25 + Q(Clouds, 24);
            k = k * 16 + Math.Min(15, RakeLines);
            k = k * 61 + Q(NewestRake, 60);
            k = k * 16 + (int)Keepsakes;
            k = k * 21 + Q(Snowman, 20);
            k = k * 25 + Q(Rainbow, 24);
            long m = Q(Dusk, 30);
            m = m * 57 + Q(MoonPhase, 56);
            m = m * 21 + Q(Wet, 20);
            m = m * 11 + Q(FadeAlpha, 10);
            m = m * 16 + Math.Min(15, FadedRakes);
            m = m * 8 + (SnowGround ? 1 : 0) + (SnowmanMelted ? 2 : 0) + (Southern ? 4 : 0);
            return k * 1000003 ^ m;
        }

        static int Q(float v, int steps) => (int)Math.Round(Clamp(v, 0, 1) * steps);
    }

    sealed class Traces
    {
        public float SnailTrack, Paws;
        public bool Feather;
        public int Leaves;
        public bool Any => SnailTrack > 0.01f || Paws > 0.01f || Feather || Leaves > 0;
    }

    static class GardenArt
    {
        public const float StoneX = 150, StoneGround = 194, StoneR = 56, Horizon = 174, BabyR = 11;
        public static readonly PointF LanternAt = new PointF(262, 192), PondAt = new PointF(72, 184), SnowmanAt = new PointF(40, 196);
        public static readonly PointF BabyAt = new PointF(220, 201), CatAt = new PointF(229, 217), FrogAt = new PointF(90, 213);
        public const float PondRx = 26, PondRy = 6.5f;
        static readonly float[][] Rocks = { new[] { 62f, 207f, 7f }, new[] { 74f, 212f, 4.5f }, new[] { 238f, 205f, 5.5f } };
        static readonly float[][] FallenLeaves = { new[] { 46f, 193f, 0f }, new[] { 86f, 200f, 1f }, new[] { 112f, 182f, 2f }, new[] { 190f, 184f, 3f }, new[] { 214f, 214f, 0f }, new[] { 250f, 200f, 2f }, new[] { 30f, 208f, 1f }, new[] { 270f, 212f, 3f } };
        static readonly float[][] Tufts = { new[] { 34f, 186f }, new[] { 98f, 179f }, new[] { 204f, 180f }, new[] { 268f, 186f }, new[] { 22f, 200f } };

        struct Star { public float X, Y, S, Ph; }
        static readonly Star[] Stars;
        static readonly float[] FlyPhase, FlySpeed;

        static GardenArt()
        {
            var rnd = new Random(11);
            var stars = new List<Star>();
            for (int i = 0; i < 26; i++)
            {
                float x = 20 + (float)rnd.NextDouble() * 260, y = 14 + (float)rnd.NextDouble() * 140;
                stars.Add(new Star { X = x, Y = y, S = 0.6f + (float)rnd.NextDouble() * 1.1f, Ph = (float)rnd.NextDouble() * 6 });
            }
            Stars = stars.ToArray();
            FlyPhase = new float[5];
            FlySpeed = new float[5];
            for (int i = 0; i < 5; i++) { FlyPhase[i] = i * 1.3f + (float)rnd.NextDouble(); FlySpeed[i] = 0.6f + (float)rnd.NextDouble() * 0.5f; }
        }

        static Color SkyTop(Season s) => Hex(s == Season.Winter ? 0xD6DFE8 : s == Season.Summer ? 0xF6EEDF : s == Season.Spring ? 0xF4EDEA : 0xF4ECE2);
        static Color SkyBottom(Season s) => Hex(s == Season.Winter ? 0xE2E8EE : s == Season.Summer ? 0xF0E5D3 : s == Season.Spring ? 0xEEE6E0 : 0xEEE3D6);
        static Color SandDay(Season s) => Hex(s == Season.Winter ? 0xEDF0F3 : s == Season.Autumn ? 0xE3D5C2 : 0xE2D9C9);
        static Color SandNight(Season s) => Hex(s == Season.Winter ? 0x5E667E : 0x45475A);

        public static Color SkyAt(Season season, float night, float clouds, bool top)
        {
            var day = top ? SkyTop(season) : SkyBottom(season);
            var dark = top ? Hex(0x1F2433) : Hex(season == Season.Winter ? 0x454C64 : 0x3A3E52);
            var c = Mix(day, dark, night);
            return Mix(c, Mix(Hex(0xC9C5C0), Hex(0x2A2E3A), night), clouds * 0.55f);
        }

        public static void DrawScene(Graphics g, SceneLook look, RectangleF area)
        {
            float n = look.Night;
            var season = look.Season;
            var ground = look.SnowGround ? Season.Winter : season;
            using (var floor = new SolidBrush(SkyAt(season, n, look.Clouds, false))) g.FillRectangle(floor, area);
            var skyRect = new RectangleF(area.X, area.Y, area.Width, Horizon + 7 - area.Y);
            using (var sky = new LinearGradientBrush(RectangleF.Inflate(skyRect, 0, 1), SkyAt(season, n, look.Clouds, true), SkyAt(season, n, look.Clouds, false), LinearGradientMode.Vertical))
                g.FillRectangle(sky, skyRect);
            float dusk = look.Dusk * (1 - look.Clouds * 0.6f);
            if (dusk > 0.01f)
            {
                FillRadial(g, 150, Horizon + 8, 230, 95, Color.FromArgb((int)(170 * dusk), 242, 152, 104), Color.FromArgb(0, 242, 152, 104));
                FillRadial(g, 150, Horizon - 20, 170, 60, Color.FromArgb((int)(120 * dusk), 246, 196, 140), Color.FromArgb(0, 246, 196, 140));
            }

            float sun = (1 - n) * (1 - look.Clouds * 0.8f);
            if (sun > 0.01f)
            {
                var glow = season == Season.Winter ? Color.FromArgb(240, 236, 224) : season == Season.Summer ? Color.FromArgb(252, 210, 140) : season == Season.Spring ? Color.FromArgb(250, 212, 192) : Color.FromArgb(250, 208, 162);
                int alpha = season == Season.Summer ? 170 : season == Season.Winter ? 110 : 140;
                FillRadial(g, 92, 64, 90, 90, Color.FromArgb((int)(alpha * sun), glow), Color.FromArgb(0, glow));
            }
            if (n > 0.01f) DrawMoon(g, 206, 66, 13, look.MoonPhase, look.Southern, n * (1 - look.Clouds * 0.7f), SkyAt(season, n, look.Clouds, true));
            float bow = look.Rainbow * (1 - n);
            if (bow > 0.01f)
            {
                var bands = new[] { Hex(0xE27C6B), Hex(0xE8C46A), Hex(0x9CC08A), Hex(0x8DB1D6) };
                for (int i = 0; i < bands.Length; i++)
                    using (var pen = new Pen(Alpha(bands[i], 0.3f * bow), 6.2f))
                    {
                        float r = 128 - i * 6;
                        g.DrawArc(pen, 150 - r, 205 - r, r * 2, r * 2, 180, 180);
                    }
            }

            using (var groundPath = new GraphicsPath())
            {
                groundPath.AddEllipse(150 - 260, Horizon, 520, 300);
                var sandColor = Mix(SandDay(ground), SandNight(ground), n);
                if (look.Wet > 0 && ground != Season.Winter) sandColor = Mix(sandColor, Mix(sandColor, Hex(0x8A7660), 0.45f), look.Wet * 0.7f);
                using (var sand = new SolidBrush(sandColor)) g.FillPath(sand, groundPath);
                var state = g.Save();
                g.SetClip(groundPath, CombineMode.Intersect);
                bool winter = ground == Season.Winter;
                var darkLine = n > 0.5f ? Color.FromArgb(89, 20, 22, 34) : winter ? Color.FromArgb(80, 120, 135, 160) : Color.FromArgb(82, 140, 120, 96);
                var lightLine = n > 0.5f ? Color.FromArgb(64, 120, 124, 150) : winter ? Color.FromArgb(220, 255, 255, 255) : Color.FromArgb(140, 255, 250, 240);
                int lines = Math.Min(10, Math.Max(look.RakeLines, look.FadedRakes));
                for (int i = 0; i < lines; i++)
                {
                    bool faded = i >= look.RakeLines;
                    float a = faded ? look.FadeAlpha : 1;
                    if (a <= 0.01f) continue;
                    float rx = 74 + i * 15, ry = rx * 0.2f;
                    float prog = !faded && i == look.RakeLines - 1 ? look.NewestRake : 1;
                    if (prog <= 0) continue;
                    float sweep = 360 * Math.Min(1, prog);
                    using (var dark = new Pen(Color.FromArgb((int)(darkLine.A * a), darkLine), 1.4f))
                    using (var light = new Pen(Color.FromArgb((int)(lightLine.A * a), lightLine), 1f))
                    {
                        g.DrawArc(dark, StoneX - rx, StoneGround - 2 - ry, rx * 2, ry * 2, 90, sweep);
                        g.DrawArc(light, StoneX - rx - 2, StoneGround - 1 - ry - 0.6f, rx * 2 + 4, ry * 2 + 1.2f, 90, sweep);
                    }
                }
                if (look.Wet > 0.05f && !winter)
                    foreach (var puddle in Puddles)
                    {
                        FillEllipse(g, Alpha(Mix(Hex(0x8A7660), Hex(0x2C3040), n), 0.35f * look.Wet), puddle[0], puddle[1] + 0.6f, puddle[2] + 1.2f, puddle[3] + 0.8f);
                        FillEllipse(g, Alpha(SkyAt(season, n, look.Clouds, true), 0.85f * look.Wet), puddle[0], puddle[1], puddle[2], puddle[3]);
                        FillEllipse(g, Alpha(Color.White, 0.35f * look.Wet * (1 - n)), puddle[0] - puddle[2] * 0.3f, puddle[1] - puddle[3] * 0.3f, puddle[2] * 0.35f, puddle[3] * 0.3f);
                    }
                g.Restore(state);
            }

            DrawSeasonGround(g, season, n);
            if ((look.Keepsakes & Keepsake.Pond) != 0) DrawPond(g, ground, n);
            DrawRocks(g, ground, n, (look.Keepsakes & Keepsake.Moss) != 0);
            if ((look.Keepsakes & Keepsake.Lantern) != 0) DrawLantern(g, ground, n);
            if (look.Snowman > 0.01f) DrawSnowman(g, look.Snowman, n, look.SnowmanMelted);
        }

        static readonly float[][] Puddles = { new[] { 118f, 203f, 9f, 2.2f }, new[] { 190f, 207f, 7f, 1.8f }, new[] { 52f, 199f, 6f, 1.5f } };

        static void DrawMoon(Graphics g, float x, float y, float r, float phase, bool southern, float alpha, Color sky)
        {
            var light = Hex(0xF4ECD6);
            FillEllipse(g, Alpha(Mix(sky, light, 0.16f), alpha), x, y, r, r);
            float k = Cos(phase * 2 * Pi);
            if (k > 0.985f) return;
            bool litRight = (phase < 0.5f) ^ southern;
            float rx = r * Math.Abs(k);
            using (var path = new GraphicsPath())
            {
                if (litRight) path.AddArc(x - r, y - r, r * 2, r * 2, -90, 180);
                else path.AddArc(x - r, y - r, r * 2, r * 2, 90, 180);
                float start = litRight ? 90 : -90;
                float sweep = k > 0 ? -180 : 180;
                if (rx < 0.4f) path.AddLine(x, litRight ? y + r : y - r, x, litRight ? y - r : y + r);
                else path.AddArc(x - rx, y - r, rx * 2, r * 2, start, sweep);
                path.CloseFigure();
                using (var b = new SolidBrush(Alpha(light, alpha))) g.FillPath(b, path);
            }
        }

        static readonly float[][] PawSteps = { new[] { 286f, 203f }, new[] { 276f, 208f }, new[] { 266f, 204f }, new[] { 256f, 210f }, new[] { 246f, 206f }, new[] { 238f, 212f } };
        public static readonly float[][] ExtraLeaves = { new[] { 128f, 197f, 1f }, new[] { 168f, 201f, 0f }, new[] { 214f, 213f, 2f }, new[] { 236f, 198f, 3f }, new[] { 58f, 214f, 1f }, new[] { 84f, 192f, 2f }, new[] { 262f, 205f, 0f }, new[] { 110f, 210f, 3f } };
        public static readonly PointF FeatherAt = new PointF(212, 209);

        public static void DrawTraces(Graphics g, Traces traces, float night, bool snowy)
        {
            if (traces.SnailTrack > 0.01f)
            {
                var points = new List<PointF>();
                for (float x = 262; x >= 40; x -= 6) points.Add(new PointF(x, 213 + 2.2f * Sin(x / 17)));
                using (var under = RoundPen(Alpha(Hex(0x9C8B78), 0.28f * traces.SnailTrack), 2.2f))
                using (var shine = RoundPen(Alpha(Color.White, 0.55f * traces.SnailTrack * (1 - night * 0.6f)), 0.9f))
                {
                    g.DrawLines(under, points.ToArray());
                    g.DrawLines(shine, points.ToArray());
                }
            }
            if (traces.Paws > 0.01f)
            {
                var c = Alpha(snowy ? Hex(0x8C98AE) : Mix(Hex(0x8F7C66), Hex(0x2A2C38), night), 0.55f * traces.Paws);
                foreach (var p in PawSteps)
                {
                    FillEllipse(g, c, p[0], p[1], 1.7f, 1.3f);
                    for (int i = -1; i <= 1; i++) FillEllipse(g, c, p[0] - 1.9f + Math.Abs(i) * 0.4f, p[1] + i * 1.3f, 0.55f, 0.55f);
                }
            }
            if (traces.Leaves > 0)
            {
                var hues = new[] { Hex(0xD9823F), Hex(0xC4552F), Hex(0xE3B04B), Hex(0xB86B3A) };
                for (int i = 0; i < Math.Min(traces.Leaves, ExtraLeaves.Length); i++)
                {
                    var f = ExtraLeaves[i];
                    var st = g.Save();
                    g.TranslateTransform(f[0], f[1]);
                    g.RotateTransform(f[0] * 11 % 180);
                    FillEllipse(g, Mix(hues[(int)f[2]], Hex(0x3E4052), night * 0.8f), 0, 0, 2, 3.4f);
                    g.Restore(st);
                }
            }
            if (traces.Feather) DrawFeather(g, FeatherAt.X, FeatherAt.Y, 0.35f, night);
        }

        public static void DrawFeather(Graphics g, float x, float y, float angle, float night)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle * Deg);
            var vane = Mix(Hex(0xF2EEE8), Hex(0x7D8196), night);
            FillEllipse(g, Alpha(Hex(0x6E5444), 0.15f), 0.6f, 1.2f, 7.5f, 1.4f);
            FillEllipse(g, vane, 0, 0, 7, 2.1f);
            FillEllipse(g, Mix(Hex(0xC9B8A6), Hex(0x5A5D6E), night), 3.6f, 0, 3, 1.4f);
            using (var quill = RoundPen(Mix(Hex(0x8A7660), Hex(0x3A3C4D), night), 0.6f)) g.DrawLine(quill, -8.5f, 0.3f, 7, 0);
            g.Restore(st);
        }

        public static void DrawFog(Graphics g, float t, float fog, float night)
        {
            if (fog < 0.02f) return;
            var color = Alpha(Mix(Hex(0xF4F2EE), Hex(0x5A5F72), night), 0.28f * fog);
            using (var b = new SolidBrush(color))
                for (int i = 0; i < 4; i++)
                {
                    float x = (i * 97 + t * (4 + i * 1.5f)) % 420 - 60, y = 140 + i * 18;
                    g.FillEllipse(b, x - 90, y - 10, 180, 20);
                    g.FillEllipse(b, x + 40, y - 6, 140, 14);
                }
        }

        static void DrawSeasonGround(Graphics g, Season season, float n)
        {
            if (season == Season.Autumn)
            {
                var hues = new[] { Hex(0xD9823F), Hex(0xC4552F), Hex(0xE3B04B), Hex(0xB86B3A) };
                foreach (var f in FallenLeaves)
                {
                    var st = g.Save();
                    g.TranslateTransform(f[0], f[1]);
                    g.RotateTransform(f[0] * 7 % 180);
                    FillEllipse(g, Mix(hues[(int)f[2]], Hex(0x3E4052), n * 0.8f), 0, 0, 2, 3.4f);
                    g.Restore(st);
                }
            }
            else if (season == Season.Spring || season == Season.Summer)
            {
                var blade = Mix(season == Season.Summer ? Hex(0x7C9E5A) : Hex(0x8DAE6C), Hex(0x3D4B48), n * 0.8f);
                using (var pen = RoundPen(blade, 1.2f))
                    foreach (var tuft in Tufts)
                        for (int i = -2; i <= 2; i++)
                            g.DrawLine(pen, tuft[0] + i * 1.6f, tuft[1], tuft[0] + i * 2.6f, tuft[1] - 5 - (2 - Math.Abs(i)) * 1.6f);
                if (season == Season.Spring)
                    foreach (var tuft in Tufts)
                        FillEllipse(g, Mix(Hex(0xEFB3B8), Hex(0x585467), n * 0.8f), tuft[0] + 4, tuft[1] - 6, 1.3f, 1.3f);
            }
        }

        public static void DrawKeepsakeIcon(Graphics g, RectangleF dest, Keepsake keepsake, bool earned)
        {
            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var circle = new GraphicsPath())
            {
                circle.AddEllipse(dest);
                using (var b = new SolidBrush(Hex(0xE2D9C9))) g.FillPath(b, circle);
                g.SetClip(circle, CombineMode.Intersect);
            }
            float cx, cy, view;
            switch (keepsake)
            {
                case Keepsake.Moss: cx = Rocks[0][0]; cy = Rocks[0][1] - 2; view = 22; break;
                case Keepsake.Lantern: cx = LanternAt.X; cy = LanternAt.Y - 18; view = 50; break;
                case Keepsake.Pond: cx = PondAt.X; cy = PondAt.Y; view = 62; break;
                default: cx = 150; cy = 185; view = 30; break;
            }
            float s = dest.Width / view;
            g.TranslateTransform(dest.X + dest.Width / 2 - cx * s, dest.Y + dest.Height / 2 - cy * s);
            g.ScaleTransform(s, s);
            switch (keepsake)
            {
                case Keepsake.Moss: DrawRock(g, 0, Season.Autumn, 0, true); break;
                case Keepsake.Lantern: DrawLantern(g, Season.Autumn, 0); break;
                case Keepsake.Pond: DrawPond(g, Season.Autumn, 0); break;
                default: DrawSpirit(g, 150, 194, BabyR, new SpiritPose { Bare = true, EyeScale = 1.25f, Blush = 0.45f, Mouth = Mouth.Smile }, 0); break;
            }
            g.Restore(state);
            if (!earned) FillEllipse(g, Alpha(Theme.Surface, 0.6f), dest.X + dest.Width / 2, dest.Y + dest.Height / 2, dest.Width / 2 + 0.5f, dest.Height / 2 + 0.5f);
        }

        static void DrawRocks(Graphics g, Season season, float n, bool moss)
        {
            for (int i = 0; i < Rocks.Length; i++) DrawRock(g, i, season, n, moss && i == 0);
        }

        static void DrawRock(Graphics g, int i, Season season, float n, bool moss)
        {
            {
                float x = Rocks[i][0], y = Rocks[i][1], r = Rocks[i][2];
                FillEllipse(g, Color.FromArgb(46, 60, 45, 35), x, y + r * 0.55f, r * 1.1f, r * 0.3f);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(x - r, y - r * 0.72f, r * 2, r * 1.44f);
                    using (var b = new PathGradientBrush(path) { CenterPoint = new PointF(x - r * 0.4f, y - r * 0.4f), CenterColor = Mix(Hex(0xA9A097), Hex(0x5A5D6E), n), SurroundColors = new[] { Mix(Hex(0x6E655D), Hex(0x33364A), n) } })
                        g.FillPath(b, path);
                }
                if (moss)
                {
                    var green = Mix(Hex(0x6F9050), Hex(0x34433F), n * 0.8f);
                    FillEllipse(g, green, x - 1.5f, y - r * 0.5f, r * 0.75f, r * 0.32f);
                    FillEllipse(g, green, x + 2.4f, y - r * 0.38f, r * 0.45f, r * 0.26f);
                    FillEllipse(g, Mix(Hex(0x8FB06A), Hex(0x3F4F47), n * 0.8f), x - 2.5f, y - r * 0.6f, r * 0.35f, r * 0.15f);
                }
                if (season == Season.Winter)
                    FillEllipse(g, Mix(Hex(0xF6F8FA), Hex(0x8A93AC), n), x - r * 0.1f, y - r * 0.55f, r * 0.8f, r * 0.28f);
            }
        }

        static void DrawPond(Graphics g, Season season, float n)
        {
            float x = PondAt.X, y = PondAt.Y;
            bool ice = season == Season.Winter;
            FillEllipse(g, Mix(ice ? Hex(0xD3DBE4) : Hex(0xC9BBA6), Hex(0x3A3C4D), n), x, y + 0.6f, PondRx + 2.5f, PondRy + 1.6f);
            var top = Mix(ice ? Hex(0xD8E3EC) : Hex(0x9DB3BE), Hex(0x232A3D), n);
            var bottom = Mix(ice ? Hex(0xEEF3F7) : Hex(0xC6D4D9), Hex(0x323A52), n);
            var rect = new RectangleF(x - PondRx, y - PondRy, PondRx * 2, PondRy * 2);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(rect);
                using (var b = new LinearGradientBrush(RectangleF.Inflate(rect, 0, 1), top, bottom, LinearGradientMode.Vertical)) g.FillPath(b, path);
            }
            using (var shine = new Pen(Alpha(Color.White, ice ? 0.7f : 0.45f * (1 - n)), 1f))
                g.DrawLine(shine, x - 12, y - 1.5f, x + 2, y - 1.5f);
            if (n > 0.3f) FillEllipse(g, Alpha(Hex(0xF4ECD6), 0.5f * n), x + 9, y + 1, 3, 1.1f);
        }

        static void DrawLantern(Graphics g, Season season, float n)
        {
            float x = LanternAt.X, y = LanternAt.Y;
            var light = Mix(Hex(0xA9A096), Hex(0x55596C), n);
            var dark = Mix(Hex(0x857C73), Hex(0x3E4154), n);
            FillEllipse(g, Color.FromArgb(50, 60, 45, 35), x, y + 0.5f, 10, 2);
            using (var b = new SolidBrush(dark)) g.FillRectangle(b, x - 7, y - 4, 14, 4);
            using (var b = new SolidBrush(light)) g.FillRectangle(b, x - 2.5f, y - 16, 5, 12);
            using (var b = new SolidBrush(dark)) g.FillRectangle(b, x - 8, y - 19.5f, 16, 3.5f);
            using (var b = new SolidBrush(light)) g.FillRectangle(b, x - 6, y - 29, 12, 9.5f);
            var window = Mix(Hex(0x5E564F), Hex(0xF6C76B), n);
            using (var b = new SolidBrush(window)) g.FillRectangle(b, x - 3, y - 26.5f, 6, 5);
            if (n > 0.05f) FillRadial(g, x, y - 24, 24, 20, Color.FromArgb((int)(120 * n), 246, 199, 107), Color.FromArgb(0, 246, 199, 107));
            FillPolygon(g, dark, new PointF(x - 11, y - 28.5f), new PointF(x, y - 37), new PointF(x + 11, y - 28.5f));
            FillEllipse(g, dark, x, y - 38.2f, 2, 2);
            if (season == Season.Winter)
                FillPolygon(g, Mix(Hex(0xF7F9FB), Hex(0x9AA2BA), n), new PointF(x - 9, y - 30.2f), new PointF(x, y - 37), new PointF(x + 9, y - 30.2f));
        }

        static void DrawSnowman(Graphics g, float k, float n, bool melted)
        {
            float x = SnowmanAt.X, y = SnowmanAt.Y;
            var state = g.Save();
            g.TranslateTransform(x, y);
            if (melted)
            {
                g.RotateTransform(-9);
                g.ScaleTransform(k * 0.82f, k * 0.74f);
            }
            else g.ScaleTransform(k, k);
            var white = Mix(Hex(0xF6F8FA), Hex(0x9AA2BA), n);
            var shade = Mix(Hex(0xCFD8E1), Hex(0x6D7590), n);
            FillEllipse(g, Color.FromArgb(40, 60, 70, 90), 0, 0, 9, 2);
            FillRadial(g, 0, -7, 8.6f, 8.6f, white, shade);
            FillRadial(g, 0, -20, 6, 6, white, shade);
            using (var scarf = RoundPen(Mix(Hex(0xC46040), Hex(0x6E4A4A), n), 2.2f))
            {
                g.DrawLine(scarf, -4.6f, -15, 4.6f, -15);
                g.DrawLine(scarf, 3, -15, 4.4f, -10.5f);
            }
            FillEllipse(g, Theme.Ink, -1.9f, -21.5f, 0.8f, 0.8f);
            FillEllipse(g, Theme.Ink, 1.9f, -21.5f, 0.8f, 0.8f);
            FillPolygon(g, Hex(0xE38A3C), new PointF(0, -20.2f), new PointF(5, -19.3f), new PointF(0, -18.6f));
            using (var twig = RoundPen(Mix(Hex(0x6E5444), Hex(0x3C3A44), n), 1f))
            {
                g.DrawLine(twig, -7.5f, -11, -13, -16);
                g.DrawLine(twig, -11, -14, -12.5f, -11.5f);
                g.DrawLine(twig, 7.5f, -11, 13, -16.5f);
            }
            g.Restore(state);
        }

        public static void DrawStars(Graphics g, float t, float night)
        {
            if (night <= 0.01f) return;
            foreach (var s in Stars)
                if (Math.Abs(s.X - 206) > 16 || Math.Abs(s.Y - 66) > 16)
                    FillEllipse(g, Alpha(Hex(0xF0ECDC), night * (0.45f + 0.45f * Sin(t * 1.6f + s.Ph))), s.X, s.Y, s.S, s.S);
        }

        public static void DrawClouds(Graphics g, float t, float clouds, float night, RectangleF area)
        {
            if (clouds < 0.02f) return;
            float a = 0.35f + 0.55f * clouds;
            var baseColor = Mix(Mix(Hex(0xF6F4F1), Hex(0xBDB9B5), clouds), Hex(0x4A4F60), night);
            var shadeColor = Alpha(Mix(baseColor, Hex(0x8C8884), 0.4f), 0.6f * a);
            var bodyColor = Alpha(baseColor, a);
            var lightColor = Alpha(Color.White, 0.4f * a * (1 - night));
            float span = area.Width + 120;
            float[][] layout = { new[] { 0f, 38f, 1.1f, 2.2f }, new[] { 120f, 62f, 0.8f, 3.1f }, new[] { 210f, 30f, 0.95f, 1.6f }, new[] { 300f, 88f, 0.7f, 2.6f }, new[] { 60f, 104f, 0.6f, 1.9f } };
            int count = 2 + (int)Math.Round(clouds * 3);
            using (var shade = new SolidBrush(shadeColor))
            using (var body = new SolidBrush(bodyColor))
            using (var light = new SolidBrush(lightColor))
                for (int i = 0; i < count; i++)
                {
                    var c = layout[i];
                    float x = area.X - 60 + ((c[0] + t * c[3]) % span + span) % span, y = c[1], s = c[2] * (0.9f + clouds * 0.4f);
                    g.FillEllipse(shade, x - 24 * s, y - 1 * s, 50 * s, 12 * s);
                    g.FillEllipse(body, x - 18 * s, y - 9 * s, 36 * s, 18 * s);
                    g.FillEllipse(body, x - 2 * s, y - 15 * s, 28 * s, 20 * s);
                    g.FillEllipse(body, x - 26 * s, y - 4 * s, 24 * s, 13 * s);
                    g.FillEllipse(body, x - 8 * s, y - 4 * s, 40 * s, 13 * s);
                    g.FillEllipse(light, x - 10 * s, y - 13 * s, 22 * s, 9 * s);
                }
        }

        public static void DrawFireflies(Graphics g, float t, float glow)
        {
            if (glow < 0.02f) return;
            for (int i = 0; i < FlyPhase.Length; i++)
            {
                float ph = FlyPhase[i], sp = FlySpeed[i];
                float x = 150 + Cos(t * 0.35f * sp + ph) * 95 + Sin(t * 0.9f * sp + ph) * 12;
                float y = 120 + Sin(t * 0.5f * sp + ph * 2) * 50;
                float a = glow * (0.35f + 0.65f * Math.Max(0, Sin(t * 2.4f * sp + ph * 3)));
                FillRadial(g, x, y, 9, 9, Alpha(Hex(0xF0E28C), a), Color.FromArgb(0, 240, 226, 140));
                FillEllipse(g, Alpha(Hex(0xFFF8C8), a), x, y, 1.6f, 1.6f);
            }
        }

        public static SpiritPose BabyPose(SpiritPose main, float t)
        {
            var b = main.Copy();
            b.Bare = true;
            b.Bloom = 0;
            b.Cushion = false;
            b.Zzz = 0;
            b.EyeScale = 1.25f;
            b.Blush = Math.Max(b.Blush, 0.45f);
            b.Dy = main.Dy * 0.5f;
            b.Tilt = main.Tilt * 0.6f;
            if (b.Eyes == Eyes.Open)
            {
                float blink = (t + 1.3f) % 4.4f;
                b.EyeOpen = blink < 0.16f ? Math.Abs(blink - 0.08f) / 0.08f : Math.Max(0.3f, main.EyeOpen);
                b.LookX = -0.7f;
                b.LookY = -0.4f;
            }
            return b;
        }

        public static SceneLook LookFor(GardenDay day, float night) => new SceneLook
        {
            Night = night,
            Season = day.Season,
            Clouds = day.Clouds * 0.7f,
            RakeLines = 2 + Math.Min(day.Breaks, 8),
            Keepsakes = day.Keepsakes,
            Snowman = day.Season == Season.Winter && day.Breaks >= Garden.FlowerBreaks ? 1 : 0,
            Rainbow = day.Visitors.Contains(Visitors.Rainbow) ? 0.8f : 0,
            MoonPhase = Sky.MoonPhase(day.Date.AddHours(21)),
        };

        public static SpiritPose PoseFor(GardenDay day, float night)
        {
            var p = new SpiritPose
            {
                Night = night,
                Leaves = Math.Min(3, 1 + day.Breaks),
                Stem = Math.Min(4, day.Breaks),
                Bloom = day.Season != Season.Winter && day.Breaks >= Garden.FlowerBreaks ? 1 : 0,
                Snow = day.Season == Season.Winter ? 1 : 0,
                Sprout = day.Breaks > 0 ? 1 : 0.6f,
                Bud = day.Season != Season.Winter && day.Breaks >= 2 && day.Breaks < Garden.FlowerBreaks ? 1 : 0,
            };
            if (day.Empty) { p.Eyes = Eyes.Closed; p.Blush = 0.2f; }
            else { p.Mouth = Mouth.Smile; p.Blush = 0.45f; p.LookX = 0.3f; }
            return p;
        }

        public static void RenderDay(Graphics g, RectangleF dest, GardenDay day, float night, float viewTop = 34, float viewHeight = 212)
        {
            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.SetClip(dest);
            float s = dest.Height / viewHeight, vw = dest.Width / s;
            g.TranslateTransform(dest.X + dest.Width / 2 - 150 * s, dest.Y - viewTop * s);
            g.ScaleTransform(s, s);
            var area = new RectangleF(150 - vw / 2 - 2, viewTop - 2, vw + 4, viewHeight + 4);
            var look = LookFor(day, night);
            DrawScene(g, look, area);
            DrawStars(g, 1.7f, night * (1 - look.Clouds));
            DrawClouds(g, day.Date.DayOfYear * 37 % 300, look.Clouds, night, area);
            if (night > 0.5f && day.Season == Season.Summer) DrawFireflies(g, 3.1f, night * 0.8f);
            var pose = PoseFor(day, night);
            var points = DrawSpirit(g, StoneX, StoneGround, StoneR, pose, 2f);
            if ((day.Keepsakes & Keepsake.Baby) != 0) DrawSpirit(g, BabyAt.X, BabyAt.Y, BabyR, BabyPose(pose, 0.5f), 2f);
            var v = day.Visitors;
            if (v.Contains(Visitors.Snail)) DrawSnail(g, 198, 213, 0.4f, -1);
            if (v.Contains(Visitors.Frog)) DrawFrog(g, FrogAt.X, FrogAt.Y, 0, 0);
            if (v.Contains(Visitors.Cat)) DrawCat(g, CatAt.X, CatAt.Y, 0);
            if (v.Contains(Visitors.Bird)) DrawBird(g, points.Head.X, points.Head.Y, 0, 1, false);
            if (v.Contains(Visitors.Butterfly)) DrawButterfly(g, points.Tip.X, points.Tip.Y - 5, 0.6f, true);
            g.Restore(state);
        }
    }
}
