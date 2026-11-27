using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed class SceneLook
    {
        public float Night, Clouds, Rainbow, Snowman, NewestRake = 1, Dusk, MoonPhase = 0.25f, Wet, FadeAlpha, Shafts, Halo, MoonShadow, MoonDim, SunCover;
        public bool SunAnnular;

        public long BackKey(int px)
        {
            long k = px;
            k = k * 49 + Q(Night, 48);
            k = k * 4 + (int)Season;
            k = k * 25 + Q(Clouds, 24);
            k = k * 31 + Q(Dusk, 30);
            k = k * 57 + Q(MoonPhase, 56);
            k = k * 25 + Q(Rainbow, 24);
            k = k * 21 + Q(Shafts, 20);
            k = k * 21 + Q(Halo, 20);
            k = k * 4 + (Southern ? 1 : 0) + (SnowGround ? 2 : 0);
            k = k * 41 + Q(MoonShadow, 40);
            k = k * 11 + Q(MoonDim, 10);
            k = k * 61 + Q(SunCover, 60);
            k = k * 2 + (SunAnnular ? 1 : 0);
            return k;
        }

        public long GroundKey(int px) => Key(px) ^ (long)Q(Night, 48) << 50;
        public Season Season = Season.Autumn;
        public int RakeLines = 2, FadedRakes, Weeds;
        public float Tree = -1;
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
            m = m * 8 + Math.Min(7, Weeds);
            m = m * 402 + (Tree < 0 ? 0 : 1 + Q(Tree, 400));
            return k * 1000003 ^ m;
        }

        static int Q(float v, int steps) => (int)Math.Round(Clamp(v, 0, 1) * steps);
    }

    sealed class Traces
    {
        public float SnailTrack, Paws, FoxPrints;
        public bool Feather;
        public int Leaves;
        public bool Any => SnailTrack > 0.01f || Paws > 0.01f || FoxPrints > 0.01f || Feather || Leaves > 0;
    }

    static partial class GardenArt
    {
        public const float StoneX = 150, StoneGround = 194, StoneR = 56, Horizon = 174, BabyR = 11;
        public static readonly PointF LanternAt = new PointF(262, 192), PondAt = new PointF(72, 184), SnowmanAt = new PointF(40, 196);
        public static readonly PointF BabyAt = new PointF(220, 201), CatAt = new PointF(229, 217), FrogAt = new PointF(90, 213);
        public static readonly PointF DigAt = new PointF(204, 216), FoxAt = new PointF(52, 217), MoonAt = new PointF(206, 66), SunAt = new PointF(92, 64), GiftAt = new PointF(186, 214);
        public const float LadybugSeat = 1.32f * Pi, OwlSeat = 1.37f * Pi, NestSeat = 1.38f * Pi;

        public static PointF StoneSurface(SpiritPose p, float a, float inset = 0.99f)
        {
            float rx = StoneR, ry = StoneR * 0.84f, ground = StoneGround - (p.Cushion ? 10 : 0);
            float c = Cos(a), s = Sin(a);
            float y = s > 0 ? (float)Math.Pow(s, 0.72) * 0.94f : s;
            float x = Math.Sign(c) * (float)Math.Pow(Math.Abs(c), 0.88) + 0.05f * s;
            float lx = x * rx * inset * p.Sx, ly = (y * ry * inset - ry * 0.94f) * p.Sy;
            float tc = Cos(p.Tilt), ts = Sin(p.Tilt);
            return new PointF(StoneX + lx * tc - ly * ts, ground + p.Dy + lx * ts + ly * tc);
        }

        public static PointF OwlPerch(SpiritPose p, Keepsake keepsakes) =>
            (keepsakes & Keepsake.Lantern) != 0 ? new PointF(LanternAt.X, LanternAt.Y - 37.5f) : StoneSurface(p, OwlSeat);

        public static readonly PointF[] CacheSpots = { new PointF(204, 216), new PointF(238, 211), new PointF(257, 219), new PointF(222, 224) };

        public static void DrawDig(Graphics g, float x, float y, float hole, float mound, Season season, float night, bool snowy = false)
        {
            if (hole > 0.01f) FillEllipse(g, Alpha(Mix(Hex(0x7E6A52), Hex(0x25262F), night), 0.8f), x, y, 3.8f * hole, 1.3f * hole);
            if (mound <= 0.01f) return;
            if (snowy)
            {
                FillEllipse(g, Alpha(Mix(Hex(0xA9B6C8), Hex(0x3C4256), night), 0.4f * mound), x + 0.4f, y + 0.7f, 5.6f * mound, 1.5f * mound);
                FillEllipse(g, Mix(Hex(0xF6F8FA), Hex(0x9AA2BA), night), x, y - 0.6f * mound, 5 * mound, 2.2f * mound);
                return;
            }
            var sand = Mix(SandDay(season), SandNight(season), night);
            FillEllipse(g, Alpha(Mix(Hex(0x8A745A), Hex(0x2C2E3A), night), 0.5f * mound), x + 0.4f, y + 0.7f, 5.6f * mound, 1.6f * mound);
            FillEllipse(g, Mix(sand, Hex(0x9C8668), 0.35f), x, y - 0.5f * mound, 4.8f * mound, 2 * mound);
            FillEllipse(g, Alpha(Color.White, 0.4f * (1 - night) * mound), x - 1.3f, y - 1.4f * mound, 2.1f * mound, 0.6f * mound);
        }

        public static void DrawCaches(Graphics g, Caches caches, int skip, Season season, float night, bool snowy, float wind)
        {
            for (int i = 0; i < Math.Min(caches.Buried, CacheSpots.Length); i++)
                if (i != skip) DrawDig(g, CacheSpots[i].X, CacheSpots[i].Y, 0, 0.75f, season, night, snowy);
            for (int i = 0; i < Math.Min(caches.Sprouts, CacheSpots.Length); i++)
                WildArt.DrawSeedling(g, CacheSpots[i].X, CacheSpots[i].Y, caches.Growth, night, wind);
        }
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
            DrawBackground(g, look, area);
            DrawGround(g, look);
            DrawTufts(g, look.Season, look.Night, 0);
        }

        static readonly Color[] HillTints = { Hex(0xA9B79A), Hex(0x9DAE84), Hex(0xB9A88E), Hex(0xDCE2E9) };

        public static void DrawBackground(Graphics g, SceneLook look, RectangleF area)
        {
            float n = look.Night;
            var season = look.Season;
            var top = SkyAt(season, n, look.Clouds, true);
            var bottom = SkyAt(season, n, look.Clouds, false);
            var mid = Mix(top, bottom, 0.55f);
            float d = look.Dusk * (1 - look.Clouds * 0.6f);
            top = Mix(top, Hex(0x34406A), d * 0.55f);
            mid = Mix(mid, Hex(0xC98B8F), d * 0.45f);
            bottom = Mix(bottom, Hex(0xF2B27E), d * 0.7f);
            using (var floor = new SolidBrush(bottom)) g.FillRectangle(floor, area);
            var skyRect = new RectangleF(area.X, area.Y, area.Width, Horizon + 7 - area.Y);
            using (var sky = new LinearGradientBrush(RectangleF.Inflate(skyRect, 0, 1), top, bottom, LinearGradientMode.Vertical))
            {
                sky.InterpolationColors = new ColorBlend { Colors = new[] { top, mid, bottom }, Positions = new[] { 0f, 0.55f, 1f } };
                g.FillRectangle(sky, skyRect);
            }
            if (n > 0.3f && look.Clouds < 0.7f) DrawMilkyWay(g, (n - 0.3f) / 0.7f * (1 - look.Clouds));
            float dusk = look.Dusk * (1 - look.Clouds * 0.6f);
            if (dusk > 0.01f)
            {
                FillRadial(g, 150, Horizon + 8, 230, 95, Color.FromArgb((int)(170 * dusk), 242, 152, 104), Color.FromArgb(0, 242, 152, 104));
                FillRadial(g, 150, Horizon - 20, 170, 60, Color.FromArgb((int)(120 * dusk), 246, 196, 140), Color.FromArgb(0, 246, 196, 140));
            }

            float sun = (1 - n) * (1 - look.Clouds * 0.8f) * (1 - look.SunCover * 0.85f);
            if (sun > 0.01f)
            {
                var glow = season == Season.Winter ? Color.FromArgb(240, 236, 224) : season == Season.Summer ? Color.FromArgb(252, 210, 140) : season == Season.Spring ? Color.FromArgb(250, 212, 192) : Color.FromArgb(250, 208, 162);
                int alpha = season == Season.Summer ? 170 : season == Season.Winter ? 110 : 140;
                FillRadial(g, SunAt.X, SunAt.Y, 90, 90, Color.FromArgb((int)(alpha * sun), glow), Color.FromArgb(0, glow));
            }
            if (look.Shafts > 0.01f && sun > 0.01f)
                using (var shaft = new SolidBrush(Color.FromArgb((int)(42 * look.Shafts * sun), 255, 246, 226)))
                    foreach (var angle in new[] { 48f, 60f, 73f })
                    {
                        float a0 = (angle - 4) * Pi / 180, a1 = (angle + 4) * Pi / 180;
                        g.FillPolygon(shaft, new[] { new PointF(SunAt.X, 40), new PointF(SunAt.X + Cos(a0) * 240, 40 + Sin(a0) * 240), new PointF(SunAt.X + Cos(a1) * 240, 40 + Sin(a1) * 240) });
                    }
            if (n > 0.01f)
            {
                if (look.Halo > 0.01f) FillRadial(g, MoonAt.X, MoonAt.Y, 34, 34, Color.FromArgb((int)(80 * look.Halo * n), 240, 236, 220), Color.FromArgb(0, 240, 236, 220));
                DrawMoon(g, MoonAt.X, MoonAt.Y, 13, look.MoonPhase, look.Southern, n * (1 - look.Clouds * 0.7f), top);
                SkyArt.DrawMoonShadow(g, MoonAt.X, MoonAt.Y, 13, look.MoonShadow, look.MoonDim, n * (1 - look.Clouds * 0.7f));
            }
            if (look.SunCover > 0.01f) SkyArt.DrawSunEclipse(g, SunAt.X, SunAt.Y, 9, look.SunCover, look.SunAnnular, top);
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
            DrawHills(g, look, area, bottom);
        }

        static void DrawHills(Graphics g, SceneLook look, RectangleF area, Color horizon)
        {
            float n = look.Night;
            var tint = HillTints[(int)(look.SnowGround ? Season.Winter : look.Season)];
            var far = Mix(Mix(horizon, tint, 0.45f), Hex(0x2B3044), n * 0.85f);
            var near = Mix(Mix(horizon, Mix(tint, Hex(0x6E6A60), 0.25f), 0.6f), Hex(0x252938), n * 0.85f);
            Func<float, float> farY = x => 158 - 9 * Sin(x * 0.021f + 0.8f) - 5 * Sin(x * 0.047f + 2);
            Func<float, float> nearY = x => 167 - 5 * Sin(x * 0.03f + 2.4f) - 3 * Sin(x * 0.071f);
            foreach (var (shape, color) in new[] { (farY, far), (nearY, near) })
            {
                var pts = new List<PointF> { new PointF(area.Left - 10, Horizon + 10) };
                for (float x = area.Left - 10; x <= area.Right + 10; x += 8) pts.Add(new PointF(x, shape(x)));
                pts.Add(new PointF(area.Right + 10, Horizon + 10));
                FillPolygon(g, color, pts.ToArray());
            }
            var haze = new RectangleF(area.Left, 146, area.Width, Horizon + 6 - 146);
            using (var b = new LinearGradientBrush(RectangleF.Inflate(haze, 0, 1), Alpha(horizon, 0), Alpha(horizon, 0.55f), LinearGradientMode.Vertical))
                g.FillRectangle(b, haze);
        }

        static readonly PointF[] MilkyDots = BuildMilky();

        static PointF[] BuildMilky()
        {
            var rnd = new Random(23);
            var dots = new PointF[140];
            for (int i = 0; i < dots.Length; i++)
            {
                float u = (float)rnd.NextDouble(), off = (float)((rnd.NextDouble() + rnd.NextDouble() + rnd.NextDouble() - 1.5) * 14);
                dots[i] = new PointF(20 + u * 260 + off * 0.4f, 120 - u * 100 + off);
            }
            return dots;
        }

        static void DrawMilkyWay(Graphics g, float k)
        {
            var st = g.Save();
            g.TranslateTransform(150, 70);
            g.RotateTransform(-21);
            FillRadial(g, 0, 0, 150, 20, Color.FromArgb((int)(34 * k), 200, 204, 230), Color.FromArgb(0, 200, 204, 230));
            FillRadial(g, 10, 2, 90, 8, Color.FromArgb((int)(30 * k), 225, 226, 240), Color.FromArgb(0, 225, 226, 240));
            g.Restore(st);
            using (var b = new SolidBrush(Color.FromArgb((int)(110 * k), 235, 236, 248)))
                for (int i = 0; i < MilkyDots.Length; i++)
                {
                    float s = 0.35f + (i % 5) * 0.12f;
                    g.FillEllipse(b, MilkyDots[i].X - s, MilkyDots[i].Y - s, s * 2, s * 2);
                }
        }

        public static void DrawGround(Graphics g, SceneLook look)
        {
            float n = look.Night;
            var season = look.Season;
            var ground = look.SnowGround ? Season.Winter : season;
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
            if (look.Tree >= 0) DrawTree(g, look.Tree, season, n, look.SnowGround);
            DrawWeeds(g, look.Weeds, ground, n);
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
            if (traces.FoxPrints > 0.01f)
            {
                var c = Alpha(snowy ? Hex(0x8C98AE) : Mix(Hex(0x8F7C66), Hex(0x2A2C38), night), 0.5f * traces.FoxPrints);
                for (int i = 0; i < 5; i++)
                {
                    float x = 28 + i * 6.5f, y = 222 - i * 0.6f + (i % 2) * 1.2f;
                    FillEllipse(g, c, x, y, 1.5f, 1.15f);
                    FillEllipse(g, c, x + 2.5f, y - 0.5f, 0.5f, 0.55f);
                    FillEllipse(g, c, x + 2.5f, y + 0.5f, 0.5f, 0.55f);
                    FillEllipse(g, c, x + 1.5f, y - 1.5f, 0.45f, 0.5f);
                    FillEllipse(g, c, x + 1.5f, y + 1.5f, 0.45f, 0.5f);
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
        }

        public static void DrawTufts(Graphics g, Season season, float n, float wind)
        {
            if (season != Season.Spring && season != Season.Summer) return;
            var blade = Mix(season == Season.Summer ? Hex(0x7C9E5A) : Hex(0x8DAE6C), Hex(0x3D4B48), n * 0.8f);
            using (var pen = RoundPen(blade, 1.2f))
                foreach (var tuft in Tufts)
                    for (int i = -2; i <= 2; i++)
                        g.DrawLine(pen, tuft[0] + i * 1.6f, tuft[1], tuft[0] + i * 2.6f + wind * 2.4f, tuft[1] - 5 - (2 - Math.Abs(i)) * 1.6f);
            if (season == Season.Spring)
                foreach (var tuft in Tufts)
                    FillEllipse(g, Mix(Hex(0xEFB3B8), Hex(0x585467), n * 0.8f), tuft[0] + 4 + wind * 2, tuft[1] - 6, 1.3f, 1.3f);
        }

        public static void DrawPondGlints(Graphics g, float t, float n)
        {
            for (int i = 0; i < 2; i++)
            {
                float x = PondAt.X - 12 + ((t * (3 + i * 2) + i * 11) % 24), a = 0.55f * Sin(((t * 0.7f + i * 0.5f) % 1) * Pi) * (1 - n * 0.4f);
                FillEllipse(g, Alpha(Color.White, a), x, PondAt.Y - 1 + i * 2, 2.2f, 0.5f);
            }
        }

        public static void DrawLanternFlicker(Graphics g, float t, float n)
        {
            if (n < 0.05f) return;
            float flicker = 0.85f + 0.1f * Sin(t * 9.3f) + 0.05f * Sin(t * 23.1f);
            FillRadial(g, LanternAt.X, LanternAt.Y - 24, 28 * flicker, 22 * flicker, Color.FromArgb((int)(55 * n * flicker), 255, 190, 100), Color.FromArgb(0, 255, 190, 100));
        }

        public static void DrawHeatShimmer(Graphics g, float t, float k)
        {
            if (k < 0.02f) return;
            using (var pen = new Pen(Alpha(Color.White, 0.13f * k), 1.2f))
                for (int line = 0; line < 4; line++)
                {
                    var pts = new PointF[30];
                    float y = 158 + line * 4.5f;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float x = 20 + i * 9;
                        pts[i] = new PointF(x, y + Sin(x * 0.12f + t * (2.2f + line * 0.4f)) * 1.4f);
                    }
                    g.DrawCurve(pen, pts);
                }
        }

        public static void DrawDragonfly(Graphics g, float x, float y, float t, float facing)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(facing < 0 ? -1 : 1, 1);
            float flap = 0.5f + 0.5f * Math.Abs(Sin(t * 40));
            var wing = Color.FromArgb(110, 214, 232, 240);
            foreach (var (wx, angle) in new[] { (-1f, -0.35f), (-1f, 0.35f), (1.5f, -0.25f), (1.5f, 0.25f) })
            {
                var w = g.Save();
                g.TranslateTransform(wx, -1);
                g.RotateTransform((angle * flap + (angle < 0 ? -1.2f : 1.2f)) * Deg);
                FillEllipse(g, wing, 0, -4.5f, 1.6f, 4.5f);
                g.Restore(w);
            }
            using (var body = RoundPen(Hex(0x3E8A86), 1.6f)) g.DrawLine(body, -9, 0, 3, 0);
            FillEllipse(g, Hex(0x2F6E6B), 4, 0, 1.9f, 1.7f);
            FillEllipse(g, Alpha(Color.White, 0.7f), 4.6f, -0.6f, 0.5f, 0.5f);
            g.Restore(st);
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
            FillEllipse(g, Ink, -1.9f, -21.5f, 0.8f, 0.8f);
            FillEllipse(g, Ink, 1.9f, -21.5f, 0.8f, 0.8f);
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
                if (Math.Abs(s.X - MoonAt.X) > 16 || Math.Abs(s.Y - MoonAt.Y) > 16)
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

        public static void DrawFireflies(Graphics g, float t, float glow, int layer = -1)
        {
            if (glow < 0.02f) return;
            for (int i = 0; i < FlyPhase.Length; i++)
            {
                float ph = FlyPhase[i], sp = FlySpeed[i];
                if (layer >= 0 && (Sin(t * 0.35f * sp + ph) > 0 ? 1 : 0) != layer) continue;
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
            MoonShadow = night > 0.3f && EclipseOn(day, false) ? 1 : 0,
            SunCover = night < 0.5f && EclipseOn(day, true) ? 0.75f : 0,
            Tree = day.Tree,
            Weeds = day.Weeds,
        };

        static bool EclipseOn(GardenDay day, bool solar)
        {
            var e = day.Visitors.Contains(Visitors.Eclipse) ? SkyEvents.EclipseOnDay(day.Date) : null;
            return e != null && e.Solar == solar;
        }

        static readonly Meteor[] AlbumMeteors =
        {
            new Meteor { X = 84, Y = 46, Dx = 0.8f, Dy = 0.6f, Life = 1, Length = 26, Bright = 0.9f },
            new Meteor { X = 176, Y = 34, Dx = 0.75f, Dy = 0.66f, Life = 1, Length = 20, Bright = 0.75f },
            new Meteor { X = 128, Y = 80, Dx = 0.85f, Dy = 0.53f, Life = 1, Length = 16, Bright = 0.6f },
        };

        static void DrawHappenings(Graphics g, GardenDay day, float night)
        {
            if (night < 0.3f) return;
            var v = day.Visitors;
            if (v.Contains(Visitors.Aurora)) SkyArt.DrawAurora(g, 2.1f, 0.85f * night);
            if (v.Contains(Visitors.Comet)) SkyArt.DrawComet(g, 128, 50, 0.9f * night, 0);
            if (v.Contains(Visitors.Meteors)) foreach (var m in AlbumMeteors) SkyArt.DrawMeteor(g, m, 0.5f);
        }

        static void DrawNestOn(Graphics g, GardenDay day, SpiritPose pose, float night)
        {
            var (stage, progress) = Nest.On(day.Date.AddHours(12), false);
            if (stage == NestStage.None || day.Season == Season.Winter) return;
            var at = StoneSurface(pose, NestSeat, 0.95f);
            var before = StoneSurface(pose, NestSeat - 0.03f, 0.95f);
            CritterArt.DrawNest(g, at.X, at.Y, (float)Math.Atan2(at.Y - before.Y, at.X - before.X), stage, progress, 0.4f, 0, 0, 3, night);
        }

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
            DrawHappenings(g, day, night);
            DrawClouds(g, day.Date.DayOfYear * 37 % 300, look.Clouds, night, area);
            if (night > 0.5f && day.Season == Season.Summer) DrawFireflies(g, 3.1f, night * 0.8f, 0);
            var pose = PoseFor(day, night);
            var points = DrawSpirit(g, StoneX, StoneGround, StoneR, pose, 2f);
            if (night > 0.5f && day.Season == Season.Summer) DrawFireflies(g, 3.1f, night * 0.8f, 1);
            DrawNestOn(g, day, pose, night);
            DrawCaches(g, day.Caches, -1, day.Season, night, day.Season == Season.Winter, 0);
            if ((day.Keepsakes & Keepsake.Baby) != 0) DrawSpirit(g, BabyAt.X, BabyAt.Y, BabyR, BabyPose(pose, 0.5f), 2f);
            var v = day.Visitors;
            DrawGroundStills(g, v, night);
            if (v.Contains(Visitors.Bird)) DrawBird(g, points.Head.X, points.Head.Y, 0, 1, false);
            if (v.Contains(Visitors.Butterfly)) DrawButterfly(g, points.Tip.X, points.Tip.Y - 5, 0.6f, true);
            if (v.Contains(Visitors.Dragonfly)) DrawDragonfly(g, 96, 118, 0.3f, 1);
            var still = new CritterPose();
            if (v.Contains(Visitors.Owl)) { var perch = OwlPerch(pose, Keepsake.None); CritterArt.DrawOwl(g, perch.X, perch.Y, 0, new CritterPose { Turn = -0.4f }, night); }
            if (v.Contains(Visitors.Moth))
            {
                CritterArt.DrawMoth(g, MoonAt.X - 8, MoonAt.Y - 4, 0.02f, new CritterPose { Shadow = 0.85f }, 0.8f);
            }
            if (v.Contains(Visitors.Ladybug))
            {
                var seat = StoneSurface(pose, 1.12f * Pi);
                var before = StoneSurface(pose, 1.12f * Pi - 0.03f);
                CritterArt.DrawLadybug(g, seat.X, seat.Y, (float)Math.Atan2(seat.Y - before.Y, seat.X - before.X), 0, still);
            }
            if (v.Contains(Visitors.Bee)) CritterArt.DrawBee(g, points.Tip.X + 15, points.Tip.Y - 9, 0.37f, -1, still);
            StoryArt.DrawStill(g, v, points.Tip, night);
            DrawWildStill(g, day, night);
            g.Restore(state);
        }
    }
}
