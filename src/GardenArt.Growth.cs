using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    static partial class GardenArt
    {
        public static readonly PointF TreeAt = new PointF(44, 178);
        public static readonly PointF[] WeedSpots = { new PointF(116, 205), new PointF(186, 207), new PointF(101, 198), new PointF(205, 199), new PointF(132, 214), new PointF(172, 215) };
        static readonly float[][] Crown = { new[] { 0f, 0f, 1f }, new[] { -0.58f, 0.18f, 0.68f }, new[] { 0.6f, 0.14f, 0.7f }, new[] { -0.3f, -0.5f, 0.7f }, new[] { 0.32f, -0.46f, 0.66f }, new[] { 0f, -0.8f, 0.52f }, new[] { -0.82f, -0.18f, 0.46f }, new[] { 0.84f, -0.26f, 0.45f } };
        static readonly float[][] Blossoms = { new[] { -0.4f, -0.3f }, new[] { 0.2f, -0.6f }, new[] { 0.55f, 0.1f }, new[] { -0.7f, 0.2f }, new[] { 0.05f, 0.25f }, new[] { -0.15f, -0.85f }, new[] { 0.75f, -0.45f }, new[] { -0.45f, -0.65f }, new[] { 0.35f, -0.15f } };

        public static (PointF Center, float Radius) TreeCrown(float growth)
        {
            float h = 9 + 69 * growth;
            if (h < 17) return (new PointF(TreeAt.X + 0.6f, TreeAt.Y - h * 0.6f), 7);
            float cr = 5 + 22 * growth;
            return (new PointF(TreeAt.X, TreeAt.Y - h + cr * 0.8f), cr * 1.1f);
        }

        public static void DrawTree(Graphics g, float growth, Season season, float n, bool snowy)
        {
            float x = TreeAt.X, y = TreeAt.Y, h = 9 + 69 * growth;
            var bark = Mix(Hex(0x7A5C44), Hex(0x2E2C38), n * 0.85f);
            bool bare = season == Season.Winter;
            var snow = Mix(Hex(0xF6F8FA), Hex(0x9AA2BA), n);
            FillEllipse(g, Color.FromArgb(40, 60, 45, 35), x + 2, y + 0.6f, 3 + 14 * growth, 1.2f + 2.4f * growth);
            if (h < 17)
            {
                float lean = 1.2f;
                using (var stem = RoundPen(bark, 0.9f + 0.4f * growth)) g.DrawLine(stem, x, y, x + lean, y - h);
                if (bare)
                {
                    if (snowy) FillEllipse(g, snow, x + lean, y - h, 1.2f, 0.6f);
                    return;
                }
                int leaves = Math.Min(5, 2 + (int)((h - 7) / 2.5f));
                for (int i = 0; i < leaves; i++)
                {
                    float u = 1 - i * 0.2f, side = i % 2 == 0 ? 1 : -1;
                    var st = g.Save();
                    g.TranslateTransform(x + lean * u, y - h * u);
                    g.RotateTransform(side * 55);
                    FillEllipse(g, Leafy(season, n, i), 0, -1.6f, 1, 1.9f);
                    g.Restore(st);
                }
                return;
            }
            float cr = 5 + 22 * growth, cy = y - h + cr * 0.8f;
            float bw = 1.2f + 3.6f * growth, tw = bw * 0.45f, ty = cy + cr * 0.3f;
            FillPolygon(g, bark, new PointF(x - bw, y), new PointF(x - tw, ty), new PointF(x + tw, ty), new PointF(x + bw, y));
            FillEllipse(g, bark, x, y - 0.4f, bw * 1.5f, 1.1f);
            using (var lit = RoundPen(Alpha(Mix(Hex(0xA88466), Hex(0x3A3846), n), 0.6f), Math.Max(0.5f, bw * 0.35f))) g.DrawLine(lit, x - bw * 0.45f, y - 1, x - tw * 0.4f, ty + 2);
            for (int i = 1; i < Crown.Length; i++)
            {
                float bx = x + Crown[i][0] * cr * (bare ? 0.95f : 0.7f), by = cy + Crown[i][1] * cr * (bare ? 0.95f : 0.7f);
                using (var branch = RoundPen(bark, Math.Max(0.5f, tw * (bare ? 0.8f : 1f) * (1 - i * 0.06f)))) g.DrawLine(branch, x, ty + (cy - ty) * 0.15f, bx, by);
                if (!bare) continue;
                using (var twig = RoundPen(bark, Math.Max(0.4f, tw * 0.35f)))
                {
                    g.DrawLine(twig, bx, by, bx + Crown[i][0] * cr * 0.22f + 1.5f, by - cr * 0.2f);
                    g.DrawLine(twig, bx, by, bx + Crown[i][0] * cr * 0.3f - 1, by - cr * 0.05f);
                }
                if (snowy) FillEllipse(g, snow, bx, by - 0.6f, 1 + cr * 0.06f, 0.5f + cr * 0.02f);
            }
            if (bare) return;
            for (int pass = 0; pass < 3; pass++)
                for (int i = 0; i < Crown.Length; i++)
                {
                    float bx = x + Crown[i][0] * cr, by = cy + Crown[i][1] * cr, r = Crown[i][2] * cr * 0.7f;
                    var c = Leafy(season, n, i);
                    if (pass == 0) FillEllipse(g, Mix(c, Hex(0x2C3328), 0.35f), bx + r * 0.12f, by + r * 0.16f, r, r * 0.92f);
                    else if (pass == 1) FillEllipse(g, c, bx, by, r * 0.94f, r * 0.86f);
                    else FillEllipse(g, Alpha(Mix(c, Color.White, 0.3f), 0.55f * (1 - n)), bx - r * 0.3f, by - r * 0.32f, r * 0.45f, r * 0.35f);
                }
            if (season == Season.Spring)
                foreach (var b in Blossoms)
                    FillEllipse(g, Mix(Hex(0xF2BCC3), Hex(0x6A5F78), n * 0.8f), x + b[0] * cr, cy + b[1] * cr, 0.8f + cr * 0.05f, 0.8f + cr * 0.05f);
        }

        static Color Leafy(Season season, float n, int i)
        {
            Color c;
            if (season == Season.Autumn) c = new[] { Hex(0xD9823F), Hex(0xC4552F), Hex(0xE3B04B), Hex(0xB86B3A) }[i % 4];
            else if (season == Season.Summer) c = i % 3 == 0 ? Hex(0x5F8C45) : Hex(0x6E9A4E);
            else c = i % 3 == 0 ? Hex(0x86AE62) : Hex(0x93B86E);
            return Mix(c, Hex(0x2E3A3A), n * 0.8f);
        }

        static void DrawWildStill(Graphics g, GardenDay day, float night)
        {
            var v = day.Visitors;
            if ((day.Keepsakes & Keepsake.Pond) != 0 && day.Season != Season.Winter)
            {
                var (stage, k) = Wildlife.Pond(day.Date.AddHours(12), false);
                if (stage == PondStage.Spawn) WildArt.DrawSpawn(g, 59, 184.6f, 0, k, night);
                else if (stage == PondStage.Tadpoles)
                    for (int i = 0; i < 4; i++) WildArt.DrawTadpole(g, PondAt.X - 12 + i * 8, PondAt.Y + (i % 2) * 2 - 1, i % 2 == 0 ? 0.3f : 3.4f, i, 0.6f + 0.5f * k, Smooth(0.65f, 1, k), night);
                else if (stage == PondStage.Froglets || v.Contains(Visitors.Froglets))
                    foreach (var at in new[] { new PointF(51, 187.5f), new PointF(91, 189), new PointF(97, 183.5f) }) DrawFrog(g, at.X, at.Y, 0, 0, 0, 0.32f);
            }
            if (v.Contains(Visitors.Bat)) WildArt.DrawBat(g, 232, 74, 0.7f, 1, 1, night);
        }

        static readonly (float Y, float[] X)[] Slots = { (217, new float[] { 230, 70, 256, 44, 202, 98, 150, 176, 124 }), (231, new float[] { 243, 57, 216, 84, 188, 112, 163, 137 }) };

        static void DrawGroundStills(Graphics g, ICollection<string> v, float night)
        {
            var still = new CritterPose();
            var items = new List<(string Id, float Width, Action<float, float, float> Draw)>
            {
                (Visitors.Fox, 30, (x, y, f) => CritterArt.DrawFox(g, x, y, 0, f, new CritterPose { Sit = 1, Curl = 1 }, night)),
                (Visitors.Cat, 30, (x, y, f) => DrawCat(g, x, y, 0, -f, new CritterPose { Curl = 1 })),
                (Visitors.Duckling, 36, (x, y, f) => { StoryArt.DrawDuck(g, x - 9 * f, y, f, 0, 0, night, false, 0); StoryArt.DrawDuck(g, x + 9 * f, y + 1, -f, 0, 0, night, true, 0); }),
                (Visitors.Cubs, 30, (x, y, f) => { WildArt.DrawCub(g, x - 8, y, 0.5f, 1, new CritterPose { Sit = 1 }, 0.6f, 0.52f, night); WildArt.DrawCub(g, x + 8, y, 1.7f, -1, new CritterPose(), 0.6f, 0.52f, night); }),
                (Visitors.Traveler, 24, (x, y, f) => { var p = StoryArt.TravelerPose(night); p.Eyes = Eyes.Happy; p.LookX = f; StoryArt.DrawTraveler(g, x, y, p, 0, night); }),
                (Visitors.Squirrel, 26, (x, y, f) => CritterArt.DrawSquirrel(g, x, y, 0.3f, f, new CritterPose { Acorn = true }, night)),
                (Visitors.Rabbit, 22, (x, y, f) => WildArt.DrawRabbit(g, x, y, 0.4f, f, new CritterPose { Tilt = 1 }, night)),
                (Visitors.Turtle, 22, (x, y, f) => CritterArt.DrawTurtle(g, x, y, 0.5f, f, still, night)),
                (Visitors.Hedgehog, 24, (x, y, f) => StoryArt.DrawHedgehog(g, x, y, f, 0, 0, 0, 0, night)),
                (Visitors.Snails, 36, (x, y, f) => { DrawSnail(g, x - 10, y, 0.4f, 1); DrawSnail(g, x + 10, y, 0.9f, -1); }),
                (Visitors.Snail, 24, (x, y, f) => DrawSnail(g, x, y, 0.4f, f)),
                (Visitors.Frog, 16, (x, y, f) => DrawFrog(g, x, y, 0, 0)),
                (Visitors.Mouse, 18, (x, y, f) => WildArt.DrawMouse(g, x, y, 0.6f, f, new CritterPose { Sit = 1, Acorn = true }, night)),
                (Visitors.Plane, 20, (x, y, f) => StoryArt.DrawPlane(g, x, y, 0.06f, f, night)),
                (Visitors.Worm, 14, (x, y, f) => WildArt.DrawWorm(g, x, y, f, 0.4f, 0.8f, 0.7f, night)),
            };
            var placed = new List<(float X, float Y, float Width, Action<float, float, float> Draw)>();
            foreach (var item in items.Where(i => v.Contains(i.Id)))
            {
                bool done = false;
                foreach (var (y, xs) in Slots)
                {
                    foreach (var x in xs)
                    {
                        if (x - item.Width / 2 < 16 || x + item.Width / 2 > 284) continue;
                        if (placed.Any(p => p.Y == y && Math.Abs(p.X - x) < (p.Width + item.Width) / 2 + 3)) continue;
                        placed.Add((x, y, item.Width, item.Draw));
                        done = true;
                        break;
                    }
                    if (done) break;
                }
            }
            foreach (var p in placed.OrderBy(p => p.Y)) p.Draw(p.X, p.Y, p.X < StoneX ? 1 : -1);
        }

        public static void DrawSnowmanOver(Graphics g, SceneLook look) => DrawSnowman(g, look.Snowman, look.Night, look.SnowmanMelted);

        public static string PlantSpot(PointF q, System.Collections.Generic.IEnumerable<Plant> plants, Keepsake keepsakes)
        {
            if (q.Y < 182 || q.Y > 230 || (q.X - 150) * (q.X - 150) + (q.Y - 150) * (q.Y - 150) > 128 * 128) return "Not there. Try the open sand.";
            if (q.X > 96 && q.X < 204 && q.Y > 213) return "Not there. That's where the time goes.";
            if (Math.Abs(q.X - StoneX) < 64 && q.Y < StoneGround + 6) return "Too close to me. I'd squash it.";
            if ((keepsakes & Keepsake.Pond) != 0)
            {
                float dx = (q.X - PondAt.X) / (PondRx + 5), dy = (q.Y - PondAt.Y) / (PondRy + 4);
                if (dx * dx + dy * dy < 1) return "Not in the pond.";
            }
            if ((keepsakes & Keepsake.Lantern) != 0 && Math.Abs(q.X - LanternAt.X) < 11 && q.Y < LanternAt.Y + 5) return "Not there. Try the open sand.";
            foreach (var r in Rocks)
                if ((q.X - r[0]) * (q.X - r[0]) + (q.Y - r[1]) * (q.Y - r[1]) < (r[2] + 4) * (r[2] + 4)) return "That's a rock. Try the sand.";
            if (Math.Abs(q.X - TreeAt.X) < 8 && q.Y < TreeAt.Y + 4) return "Too close to our tree.";
            if ((keepsakes & Keepsake.Baby) != 0 && Math.Abs(q.X - BabyAt.X) < BabyR + 6 && q.Y < BabyAt.Y + 6) return "That's where the little one sits.";
            if (Math.Abs(q.X - SnowmanAt.X) < 12 && q.Y < SnowmanAt.Y + 6 && q.Y > SnowmanAt.Y - 30) return "Not there. Try the open sand.";
            foreach (var p in plants)
                if ((q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y) < 13 * 13) return "Too close to the other one.";
            return null;
        }

        public static void DrawWeeds(Graphics g, int count, Season ground, float n)
        {
            for (int i = 0; i < Math.Min(count, WeedSpots.Length); i++) DrawWeed(g, WeedSpots[i].X, WeedSpots[i].Y, ground, n, i);
        }

        public static void DrawWeed(Graphics g, float x, float y, Season ground, float n, int i)
        {
            bool dry = ground == Season.Winter;
            var blade = Mix(dry ? Hex(0x8B7556) : Hex(0x5D7D3A), Hex(0x2E3438), n * 0.8f);
            FillEllipse(g, Alpha(Mix(Hex(0x8F7C66), Hex(0x2A2C38), n), 0.35f), x, y + 0.4f, 3.4f, 0.9f);
            using (var pen = RoundPen(blade, 0.9f))
                for (int b = -2; b <= 2; b++)
                {
                    float tall = 5 + (2 - Math.Abs(b)) * 1.6f + (i % 3);
                    g.DrawLines(pen, new[] { new PointF(x + b * 0.7f, y), new PointF(x + b * 1.4f + 0.6f, y - tall * 0.55f), new PointF(x + b * 2.1f, y - tall) });
                }
            if (dry || i % 2 == 1) return;
            using (var stalk = RoundPen(blade, 0.6f)) g.DrawLine(stalk, x + 0.5f, y, x + 1.2f, y - 9);
            FillEllipse(g, Mix(Hex(0xF2C94C), Hex(0x6B6650), n * 0.8f), x + 1.2f, y - 9.6f, 1.7f, 1.4f);
        }
    }
}
