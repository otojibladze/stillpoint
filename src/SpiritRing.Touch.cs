using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        RakeStroke raking;
        bool rakeDown, rakeGap, rakeUsed;
        Point rakeDownAt;
        float treeShakeT = -99, sandWash, sandHoverT = -1, touchSaidT = -99;
        string tipNow;
        readonly HashSet<string> hintsShown = new HashSet<string>();
        readonly Dictionary<Plant, int> plantStages = new Dictionary<Plant, int>();

        public Touches Touch { get; set; } = new Touches();
        public event Action TouchChanged;
        public event Action<string> HintShown;

        bool CanTouch => !plain && Width > 60 && Touch != null;

        bool InPond(PointF q)
        {
            if (!Pond) return false;
            float dx = (q.X - GardenArt.PondAt.X) / (GardenArt.PondRx + 2), dy = (q.Y - GardenArt.PondAt.Y) / (GardenArt.PondRy + 2);
            return dx * dx + dy * dy < 1;
        }

        bool OnSand(PointF q) => q.Y >= GardenArt.Horizon + 5 && q.Y <= 238 && Dist(q, 150, 150) < 136 && !InPond(q);

        bool NearStone(PointF q) => OnStone(q) || Math.Abs(q.X - SX) < R * 1.12f && q.Y < Ground + 5 && q.Y > Ground - R * 1.9f;

        bool OnTree(PointF q)
        {
            if (state.Tree < 0) return false;
            var (c, r) = GardenArt.TreeCrown(state.Tree);
            return Dist(q, c.X, c.Y) < r;
        }

        bool HintDone(string id) => hintsShown.Contains(id) || (state.Hints ?? "").Split(',').Contains(id);

        void MarkHint(string id)
        {
            if (!hintsShown.Add(id)) return;
            HintShown?.Invoke(id);
        }

        void SetTip(string text)
        {
            if (text == tipNow) return;
            tipNow = text;
            roamTip.SetToolTip(this, text);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            rakeUsed = false;
            if (!CanTouch || e.Button != MouseButtons.Left || RoamAt(e.Location)) return;
            var q = ToScene(e.Location);
            if (!OnSand(q) || NearStone(q)) return;
            rakeDown = true;
            rakeGap = false;
            rakeDownAt = e.Location;
            raking = null;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            EndRake();
        }

        void EndRake()
        {
            if (raking != null && raking.Points.Count >= 2) CommitStroke();
            raking = null;
            rakeDown = false;
        }

        void TouchMove(Point at, PointF q, MouseButtons buttons)
        {
            if (rakeDown && (buttons & MouseButtons.Left) != 0)
            {
                bool ok = OnSand(q) && !NearStone(q);
                if (raking == null)
                {
                    if (!rakeGap && Math.Abs(at.X - rakeDownAt.X) + Math.Abs(at.Y - rakeDownAt.Y) < 5) return;
                    if (!ok) return;
                    raking = new RakeStroke { At = Clock() };
                    raking.Points.Add(rakeGap ? q : ToScene(rakeDownAt));
                    rakeGap = false;
                    rakeUsed = true;
                }
                var last = raking.Points[raking.Points.Count - 1];
                if (!ok)
                {
                    if (raking.Points.Count >= 2) CommitStroke();
                    raking = null;
                    rakeGap = true;
                }
                else if (Dist(q, last.X, last.Y) >= 2.5f)
                {
                    raking.Points.Add(q);
                    if (raking.Points.Count >= Touches.MaxPoints)
                    {
                        CommitStroke();
                        raking = new RakeStroke { At = Clock() };
                        raking.Points.Add(q);
                    }
                }
            }
            sandHoverT = !rakeDown && OnSand(q) && !NearStone(q) && !OnTree(q) ? (sandHoverT < 0 ? Now : sandHoverT) : -1;
            SetTip(RoamTipAt(at) ?? StarTip(q));
            if (OnTree(q) || InPond(q)) Cursor = Cursors.Hand;
        }

        void CommitStroke()
        {
            var stroke = raking;
            raking = null;
            if (stroke == null || stroke.Points.Count < 2) return;
            Touch.Strokes.Add(stroke);
            Touch.Prune(Clock());
            TouchChanged?.Invoke();
            float t = Now;
            if (!HintDone("raked"))
            {
                MarkHint("raked");
                Chat(Tone == Tone.Dry ? "Nice lines. I've been doing that for 3000 years." : "Ooh. Very zen.", 2.6f);
                if (!HintDone("plant")) { MarkHint("plant"); Later("Double-click the sand to plant a seed.", t + 3, 3.4f); }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (!CanTouch || e.Button != MouseButtons.Left || RoamAt(e.Location)) return;
            var q = ToScene(e.Location);
            if (OnSand(q) && !OnStone(q) && !OnTree(q)) TryPlant(q);
        }

        bool TouchClick(PointF q)
        {
            if (!CanTouch || rakeUsed) return false;
            float t = Now;
            if (OnTree(q)) { ShakeTree(t); return true; }
            if (InPond(q))
            {
                for (int i = 0; i < 2; i++) Emit(new Particle { Kind = Bit.Ripple, X = q.X, Y = q.Y, Life = 0.9f + i * 0.4f, Size = 1 + i });
                Emit(new Particle { Kind = Bit.Dew, X = q.X, Y = q.Y - 1, Vy = -40, Size = 1.1f, Target = q.Y, Life = 0.8f });
                return true;
            }
            return false;
        }

        void TryPlant(PointF q)
        {
            float t = Now;
            var plants = Touch.Plants;
            if (plants.Count >= Touches.MaxPlants)
            {
                if (t - touchSaidT > 3) { touchSaidT = t; Say("Our flower bed is full. Let these grow first.", 2.8f); }
                return;
            }
            string why = GardenArt.PlantSpot(q, plants, state.Keepsakes);
            if (why != null)
            {
                if (t - touchSaidT > 3) { touchSaidT = t; Say(why, 2.4f); }
                return;
            }
            var plant = new Plant { X = q.X, Y = q.Y, Kind = Touches.KindFor(Clock()), At = Clock() };
            plants.Add(plant);
            plantStages[plant] = 0;
            TouchChanged?.Invoke();
            for (int i = 0; i < 6; i++)
                Emit(new Particle { Kind = Bit.Dirt, X = q.X, Y = q.Y - 1, Vx = (Rand - 0.5f) * 40, Vy = -30 - Rand * 25, Life = 1.1f, Size = 0.6f + Rand * 0.5f, Target = q.Y + Rand * 2, Night = nightK });
            Emit(new Particle { Kind = Bit.Puff, X = q.X, Y = q.Y - 1, Vy = -5, Life = 0.6f, Size = 2.2f });
            if (!HintDone("seed"))
            {
                MarkHint("seed");
                Say("A seed! It grows a little every time you take a break.", 3.8f);
            }
            else Chat(Pick(new[] { "Grow, little one.", "Planted. Now we wait.", "I'll keep an eye on it.", "Breaks are its water." }), 2.4f);
        }

        void ShakeTree(float t)
        {
            if (state.Tree < 0) return;
            var (c, r) = GardenArt.TreeCrown(state.Tree);
            if (state.Tree < 0.12f)
            {
                if (t - touchSaidT > 4) { touchSaidT = t; Say("It's still tiny. Give it time.", 2.4f); }
                return;
            }
            treeShakeT = t;
            bool snow = snowGround || season == Season.Winter;
            int n = snow ? 10 : season == Season.Summer ? 4 : 7;
            for (int i = 0; i < n; i++)
            {
                float a = Rand * Pi * 2, d = (float)Math.Sqrt(Rand) * r * 0.8f;
                var p = new Particle { X = c.X + Cos(a) * d, Y = c.Y + Sin(a) * d * 0.8f, Vx = (Rand - 0.5f) * 14, Vy = 8 + Rand * 12, Vr = (Rand - 0.5f) * 4, Target = 178 + Rand * 22, Life = 6 };
                if (snow) { p.Kind = Bit.Snow; p.Size = 0.9f + Rand; p.Vy = 30 + Rand * 20; }
                else if (season == Season.Autumn) { p.Kind = Bit.Autumn; p.Size = 3.2f; p.Hue = rng.Next(4); }
                else if (season == Season.Spring) { p.Kind = Bit.Petal; p.Size = 2.4f; }
                else { p.Kind = Bit.Leaf; p.Size = 4; }
                Emit(p);
            }
            if (!bird.Active && Daylight && !snow && !Sleeping && state.Tree > 0.3f && Rand < 0.35f)
            {
                bird.State = 3;
                bird.T0 = t;
                bird.D = 0;
                bird.Side = 1;
                bird.From = new PointF(c.X, c.Y - r * 0.3f);
                bird.To = new PointF(-30, 6);
                Emit(new Particle { Kind = Bit.Note, X = c.X + 4, Y = c.Y - r * 0.5f, Vx = 6, Vy = -20, Vr = 0.3f, Life = 1, Size = 7 });
            }
            if (t - touchSaidT < 6) return;
            touchSaidT = t;
            if (!HintDone("tree")) { MarkHint("tree"); Say("Careful! That's our tree.", 2.4f); }
            else Chat(snow ? "Brr. Snow everywhere." : season == Season.Autumn ? "Leaves everywhere!" : season == Season.Spring ? "Petals!" : "Shh. It was resting.", 2.2f);
        }

        float PlantBreaks(Plant p)
        {
            var ends = state.BreakEnds;
            if (ends == null || ends.Count == 0) return 0;
            int lo = 0, hi = ends.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (ends[mid] <= p.At) lo = mid + 1; else hi = mid;
            }
            return ends.Count - lo;
        }

        void StepTouch(float t, float dt)
        {
            if (Touch == null) return;
            bool washing = rainK > 0.45f && Touch.Strokes.Count > 0;
            sandWash = washing ? Math.Min(1, sandWash + dt / 14) : Math.Max(0, sandWash - dt / 3);
            if (sandWash >= 1)
            {
                Touch.Strokes.Clear();
                sandWash = 0;
                TouchChanged?.Invoke();
                if (!Sleeping && t - touchSaidT > 10) { touchSaidT = t; Chat(snowing ? "The snow covered our lines." : "The rain smoothed our sand.", 2.6f); }
            }
            bool quiet = bubble == null || t >= bubbleEnd;
            if (sandHoverT > 0 && Now - sandHoverT > 2.5f && quiet && !HintDone("rake") && !HintDone("raked") && !Sleeping && known && !Busy(t))
            {
                MarkHint("rake");
                Say("Psst. Drag across the sand to rake it.", 3.4f);
            }
            foreach (var p in Touch.Plants)
            {
                int stage = Touches.Stage((int)PlantBreaks(p));
                if (!plantStages.TryGetValue(p, out var was)) { plantStages[p] = stage; continue; }
                if (stage <= was) continue;
                plantStages[p] = stage;
                for (int i = 0; i < 5; i++) Emit(new Particle { Kind = Bit.Spark, X = p.X + (Rand - 0.5f) * 6, Y = p.Y - 8, Vx = (Rand - 0.5f) * 20, Vy = -20 - Rand * 15, Vr = 3, Life = 0.9f, Size = 2 + Rand });
                if (stage == 4 && !Sleeping) Chat(Pick(new[] { "Look! Our flower bloomed.", "It bloomed! All those breaks paid off." }), 3f);
            }
        }

        void DrawLiveTree(Graphics g, float t, float night, SceneLook look)
        {
            if (state.Tree < 0) return;
            float s = t - treeShakeT;
            float shake = s >= 0 && s < 2.2f ? Sin(s * 20) * 5 * (float)Math.Exp(-s * 2.6f) : 0;
            float sway = wind * 0.8f * Smooth(0.05f, 0.4f, state.Tree) + shake;
            var st = g.Save();
            g.TranslateTransform(GardenArt.TreeAt.X, GardenArt.TreeAt.Y);
            g.RotateTransform(sway);
            g.TranslateTransform(-GardenArt.TreeAt.X, -GardenArt.TreeAt.Y);
            GardenArt.DrawTree(g, state.Tree, season, night, snowGround);
            g.Restore(st);
            if (look.Snowman > 0.01f) GardenArt.DrawSnowmanOver(g, look);
        }

        void DrawTouches(Graphics g, float t, float night)
        {
            if (Touch == null) return;
            var now = Clock();
            bool winter = snowGround || season == Season.Winter;
            float wash = 1 - sandWash;
            foreach (var s in Touch.Strokes)
                TouchArt.DrawStroke(g, s.Points, wash * (1 - Smooth(48, 72, (float)(now - s.At).TotalHours)), night, winter);
            if (raking != null) TouchArt.DrawStroke(g, raking.Points, 1, night, winter);
            bool neglected = state.Weeds > 0;
            foreach (var p in Touch.Plants.OrderBy(p => p.Y))
            {
                float age = (float)(now - p.At).TotalDays;
                float wilt = Math.Max(Smooth(Touches.WiltDays, Touches.PlantDays, age), neglected ? 0.35f : 0);
                int stage = Touches.Stage((int)PlantBreaks(p));
                bool dormant = winter && p.Kind != 3 && stage > 0;
                TouchArt.DrawPlant(g, p.X, p.Y, p.Kind, stage, wilt, night, wind, dormant, t);
            }
        }
    }
}
