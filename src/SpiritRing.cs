using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class RingState
    {
        public bool Running, Paused, Away;
        public float Progress;
        public double SessionSeconds, Interval = 3600;
        public string Time = "0:00:00", Caption = "", Note = "";
        public int BreaksToday;
        public DateTime LastEnd;
        public Keepsake Keepsakes;
        public string Name = "";
        public Chattiness Chat = Chattiness.Normal;
        public int BreaksYesterday;
        public SunTimes Sun;
        public bool Southern;
        public WeatherNow Weather;
    }

    sealed class Critter
    {
        public int State;
        public float T0, X, Y, Side = 1, K;
        public PointF From, To;
        public bool Active => State != 0;
    }

    sealed class SpiritRing : Control
    {
        const float SX = GardenArt.StoneX, Ground = GardenArt.StoneGround, R = GardenArt.StoneR, EyeY = 146;
        static readonly string[] ThoughtsFree = { "Do clouds ever sit down?", "I've sat here for 3000 years. You don't have to.", "Water. Have you had some today?", "I like this sand.", "Roll your shoulders. I'll wait.", "Nothing to do. Perfect." };
        static readonly string[] ThoughtsSitting = { "Blink a few times. Screens are dry.", "Still here. Still sitting.", "Unclench your jaw.", "How's your back?" };
        static readonly (string Name, string Line)[] Stretches =
        {
            ("NECK ROLLS", "Neck rolls. Slow circles, then the other way."),
            ("SHOULDER SHRUGS", "Shoulders up to your ears... and drop."),
            ("REACH UP", "Reach up high. Hold it... and let go."),
            ("SIDE BENDS", "Lean to one side... then the other."),
            ("LOOK FAR AWAY", "Look out a window. Find something far away."),
        };
        const float DemoLength = 8.5f;

        readonly Random rng = new Random();
        readonly Stopwatch watch = Stopwatch.StartNew();
        readonly System.Windows.Forms.Timer wake = new System.Windows.Forms.Timer { Interval = 250 };
        readonly List<Particle> bits = new List<Particle>();
        readonly List<float> pokes = new List<float>();
        readonly Critter butterfly = new Critter(), bird = new Critter(), snail = new Critter(), frog = new Critter(), cat = new Critter();
        SpiritPose pose;
        float last;

        RingState state = new RingState();
        bool known, hasLast, catSeen;
        int breaks = -1, demoIndex;
        DateTime breaksDate, lastEnd;
        Season season = Season.Autumn;

        float stretchT = -99, pokeT = -99, dizzyT = -99, wakeT = -99, yawnT = -99, rakeT = -99, bloomT = -99, snowmanT = -99;
        float nextAct = 8, bfAt = -1, frogAt = -1, snailNext = 20, petSaidT = -99, heartT, noteT, humT = -99, demoT = -99, rainbowT = -99, purrT = -99, cricketNext = 15;
        float rainAcc, ambientAcc;
        float fogK, wetReal, flashT = -99, lightningNext = 20, tracesT = -99;
        float parX, parY, wind, windDrift, gustT = -99, gustNext = 15, starT = -99, starNext = 30, starX, starY, starDx, starDy, shimmerK, shownProgress;
        float zoomT = -99, landT = -99, snowPile, vaporNext, splashAcc, dripAcc, boredT = -99;
        int petalsOnHead;
        readonly Critter dragonfly = new Critter();
        bool snowing, snowGround;
        readonly Traces traces = new Traces();
        AwayReport away;
        bool sparked = true, landed = true, petting;
        float rub, nightK = -1, cloudK, rainK, maxRain;
        float lookX, lookY, lookMidX, lookMidY;
        bool lookReady;
        SpiritPoints points = new SpiritPoints { Tip = new PointF(SX, 90), Head = new PointF(132, 106) };
        PointF pointer;
        bool pointerIn;
        float pointerT = -99;

        string bubble;
        float bubbleT0, bubbleEnd;
        readonly List<(float At, string Text, float Seconds)> pending = new List<(float At, string Text, float Seconds)>();
        bool bubbleThought;

        Font timeFont, captionFont, bubbleFont, plainFont;
        bool plain;

        public bool Plain
        {
            get => plain;
            set
            {
                if (plain == value) return;
                if (IsHandleCreated && Visible && Width > 20 && Height > 20)
                {
                    fadeShot?.Dispose();
                    fadeShot = new Bitmap(Width, Height);
                    using (var g = Graphics.FromImage(fadeShot)) Render(g, Now);
                    fadeT = Now;
                }
                plain = value;
                Invalidate();
            }
        }
        int fontKey;

        public SpiritRing()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            wake.Tick += (s, e) => { };
            wake.Start();
            Application.Idle += OnIdle;
        }

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
        public event Action<string> Visited;
        public event Action<StoneSound> SoundRequested;
        public float Now => (float)watch.Elapsed.TotalSeconds;

        bool Running => state.Running;
        bool NightHour(int from) { int h = Clock().Hour; return h >= from || h < 6; }
        bool Sleeping => state.Paused || (!state.Running && NightHour(22));
        bool DemoActive(float t) => t >= demoT && t < demoT + DemoLength;
        float Drowsiness => Running ? SpiritArt.Clamp(state.Progress, 0, 1.2f) : 0;

        public void SetSession(RingState next)
        {
            float t = Now;
            var today = Clock().Date;
            season = Garden.SeasonOf(today);
            if (breaksDate != today || breaks < 0)
            {
                breaks = next.BreaksToday;
                breaksDate = today;
            }
            else if (next.BreaksToday > breaks)
            {
                rakeT = t + Math.Max(0, 1.9f - (t - stretchT));
                if (breaks < Garden.FlowerBreaks && next.BreaksToday >= Garden.FlowerBreaks)
                {
                    if (season == Season.Winter) { snowmanT = rakeT + 0.8f; Later("I made a snowman!", rakeT + DemoLength + 2.5f, 3f); }
                    else { bloomT = rakeT + 0.8f; zoomT = Math.Max(zoomT, rakeT + DemoLength + 2.3f); Later("I grew a flower!", rakeT + DemoLength + 2.5f, 3f); }
                }
                breaks = next.BreaksToday;
            }

            if (known && next.Running != state.Running)
            {
                if (next.Running) Started(t);
                else Stopped(t);
            }
            if (known && next.Away && !state.Away) Chat("Hello? Are you there?", 4f);
            var gained = next.Keepsakes & ~state.Keepsakes;
            if (known && gained != Keepsake.None)
            {
                zoomT = t + 0.5f;
                string name = (gained & Keepsake.Moss) != 0 ? "moss" : (gained & Keepsake.Lantern) != 0 ? "a stone lantern" : (gained & Keepsake.Pond) != 0 ? "a pond" : "a baby pebble";
                Later("Look! The garden got " + name + ".", t + 0.8f, 3.4f);
            }
            if (known && !string.IsNullOrEmpty(next.Name) && next.Name != state.Name) Say($"{next.Name}. I like it!", 3f);
            known = true;
            hasLast = next.LastEnd != default(DateTime);
            if (!next.Running) lastEnd = next.LastEnd;
            state = next;
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
            frogAt = -1;
            demoT = -99;
            maxRain = 0;
            pending.Clear();
            SendAway(butterfly, t);
            SendAway(bird, t);
            if (frog.State == 1 || frog.State == 2) { frog.State = 3; frog.T0 = t; }
        }

        void Stopped(float t)
        {
            stretchT = t;
            sparked = landed = false;
            humT = -99;
            demoT = t + 2.1f;
            demoIndex = (Clock().DayOfYear + Math.Max(0, breaks)) % Stretches.Length;
            Later(Stretches[demoIndex].Line, demoT, DemoLength - 0.3f);
            bfAt = demoT + DemoLength + 1.5f;
            if (maxRain > 0.15f || (cloudK > 0.6f && rng.NextDouble() < 0.5))
            {
                rainbowT = t + 2;
                Visit(Visitors.Rainbow);
            }
            if (maxRain > 0.25f) frogAt = demoT + DemoLength + 1;
            maxRain = 0;
            BlowAwayTraces();
            for (int i = 0; i < petalsOnHead; i++)
                Emit(new Particle { Kind = Bit.Petal, X = SX + (i - 1) * 14, Y = points.Head.Y - 2, Vx = (i - 1) * 30 + Rand * 10, Vy = -30, Vr = 3, Size = 2.4f, Target = Ground + 6, Life = 4 });
            petalsOnHead = 0;
        }

        public void ShowAway(AwayReport report)
        {
            away = report;
            tracesT = Now;
            traces.Feather = report.Saw(Visitors.Bird);
            traces.Leaves = report.Leaves;
            StepTraces(tracesT);
            string line = report.Message();
            if (line != null) { if (state.Chat != Chattiness.Quiet) Later(line, tracesT + 1.4f, 4f); }
        }

        void StepTraces(float t)
        {
            if (away == null) return;
            float since = t - tracesT;
            traces.SnailTrack = away.Saw(Visitors.Snail) ? 1 - SpiritArt.Smooth(240, 300, since) : 0;
            traces.Paws = away.Saw(Visitors.Cat) ? 1 - SpiritArt.Smooth(480, 600, since) : 0;
            if (since > 1800) { traces.Feather = false; traces.Leaves = 0; }
        }

        float AwayWet(float t) => away != null && away.Rained ? 1 - SpiritArt.Smooth(120, 240, t - tracesT) : 0;

        void BlowAwayTraces()
        {
            if (traces.Feather)
                Emit(new Particle { Kind = Bit.Feather, X = GardenArt.FeatherAt.X, Y = GardenArt.FeatherAt.Y, Vx = -55, Vy = -35, Vr = 2.5f, Life = 4, Night = nightK });
            for (int i = 0; i < Math.Min(traces.Leaves, GardenArt.ExtraLeaves.Length); i++)
            {
                var f = GardenArt.ExtraLeaves[i];
                Emit(new Particle { Kind = Bit.Autumn, X = f[0], Y = f[1], Vx = (f[0] < SX ? -1 : 1) * (40 + Rand * 30), Vy = -25 - Rand * 20, Vr = 3 + Rand * 2, Size = 3.2f, Hue = (int)f[2], Life = 3 });
            }
            traces.Feather = false;
            traces.Leaves = 0;
        }

        public void Reminder()
        {
            yawnT = Now;
            Emit(new Particle { Kind = Bit.Leaf, X = points.Tip.X, Y = points.Tip.Y, Vx = 8, Vy = 4, Vr = 2, Life = 4, Size = 6, Target = Ground - 2 });
        }

        public void Announce(string text, bool special = false)
        {
            Later(text, Now + 1.2f, 3.4f);
            if (special) zoomT = Now + 1;
        }

        void StartDragonfly(float t)
        {
            dragonfly.State = 1;
            dragonfly.T0 = t;
            dragonfly.K = 0;
            dragonfly.Side = rng.NextDouble() < 0.5 ? -1 : 1;
            dragonfly.X = SX - dragonfly.Side * 170;
            dragonfly.Y = 110;
            dragonfly.From = new PointF(dragonfly.X, dragonfly.Y);
            dragonfly.To = new PointF(80 + Rand * 140, 70 + Rand * 60);
            Visit(Visitors.Dragonfly);
        }

        void StepDragonfly(float t)
        {
            var c = dragonfly;
            if (!c.Active) return;
            float since = t - c.T0;
            float hop = since % 1.1f;
            int segment = (int)(since / 1.1f);
            if (segment != (int)c.K) { c.K = segment; c.From = new PointF(c.X, c.Y); c.To = since > 7 ? new PointF(SX + c.Side * 190, 40 + Rand * 40) : new PointF(70 + Rand * 160, 60 + Rand * 70); }
            float k = SpiritArt.Smooth(0, 0.35f, hop);
            float nx = c.From.X + (c.To.X - c.From.X) * k, ny = c.From.Y + (c.To.Y - c.From.Y) * k + SpiritArt.Sin(t * 14) * 0.6f;
            if (Math.Abs(nx - c.X) > 0.3f) c.Side = nx > c.X ? 1 : -1;
            c.X = nx;
            c.Y = ny;
            if (since > 8.2f || Running) c.State = 0;
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

        void Chat(string text, float seconds = 2.6f, bool thought = false)
        {
            if (state.Chat != Chattiness.Quiet) Say(text, seconds, thought);
        }

        void Later(string text, float at, float seconds) => pending.Add((at, text, seconds));

        void Visit(string visitor) => Visited?.Invoke(visitor);

        void Emit(Particle p)
        {
            if (bits.Count < 220) bits.Add(p);
        }

        float Rand => (float)rng.NextDouble();

        bool Animating
        {
            get
            {
                if ((plain && !Fading) || IsDisposed || !IsHandleCreated || !Visible) return false;
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
            if (fadeShot != null && !Fading) Invalidate();
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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWaitableTimerExW(IntPtr attributes, string name, uint flags, uint access);

        [DllImport("kernel32.dll")]
        static extern bool SetWaitableTimer(IntPtr timer, ref long dueTime, int period, IntPtr completion, IntPtr arg, bool resume);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public Point Location;
        }

        [DllImport("user32.dll")]
        static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);

        public void Step(float t, float dt)
        {
            if (nightK < 0) nightK = 0;
            var now = Clock();
            float nightTarget = Sleeping ? 1 : Sky.Darkness(now, state.Sun);
            nightK += (nightTarget - nightK) * (1 - (float)Math.Exp(-dt * 2.5));
            var real = state.Weather != null && state.Weather.Fresh(now) ? state.Weather : null;
            float realClouds = real == null ? 0 : real.CloudCover / 100f * 0.85f;
            float realRain = real == null || real.Rain <= 0.02f ? 0 : SpiritArt.Clamp(0.3f + real.Rain * 0.35f, 0, 1);
            float realSnow = real == null || real.Snowfall <= 0.01f ? 0 : SpiritArt.Clamp(0.35f + real.Snowfall * 0.6f, 0, 1);
            snowing = realSnow > 0 || (season == Season.Winter && realRain == 0);
            snowGround = season == Season.Winter || (real != null && real.SnowDepth > 0.005f);
            fogK += ((real != null && real.Fog ? 1 : 0) - fogK) * (1 - (float)Math.Exp(-dt * 0.3));
            wetReal = real == null ? 0 : real.Wetness(now);
            if (real != null && real.Storm && t > lightningNext) { flashT = t; lightningNext = t + 12 + Rand * 25; }
            StepTraces(t);
            float cloudTarget = Math.Max(Running ? Weather.Clouds(state.SessionSeconds, state.Interval) : 0, Math.Max(realClouds, realRain + realSnow > 0 ? 0.7f : 0));
            float rainTarget = Math.Max(Running ? Weather.Rain(state.SessionSeconds, state.Interval) : 0, Math.Max(realRain, realSnow));
            cloudK += (cloudTarget - cloudK) * (1 - (float)Math.Exp(-dt * 0.35));
            rainK += (rainTarget - rainK) * (1 - (float)Math.Exp(-dt * (rainTarget > rainK ? 0.3 : 0.9)));
            maxRain = Math.Max(maxRain, rainK);
            for (int i = 0; i < pending.Count; i++)
                if (t >= pending[i].At) { Say(pending[i].Text, pending[i].Seconds); pending.RemoveAt(i); break; }

            rub *= (float)Math.Exp(-dt * 1.6);
            bool was = petting;
            petting = rub > (was ? 70 : 150);
            if (petting && !Sleeping)
            {
                if (!was && t - petSaidT > 6) { petSaidT = t; Chat(Pick(new[] { "Mmm. That's nice.", "Purr... wait, I'm a rock." }), 2.2f); }
                if (t - purrT > 2.4f) { purrT = t; SoundRequested?.Invoke(StoneSound.Purr); }
                if (Running) wakeT = t;
                if (t > heartT)
                {
                    heartT = t + 0.38f;
                    Emit(new Particle { Kind = Bit.Heart, X = SX + (Rand - 0.5f) * 50, Y = 108, Vx = (Rand - 0.5f) * 12, Vy = -32, Vr = (Rand - 0.5f) * 0.6f, Life = 1.4f, Size = 5 + Rand * 2 });
                }
            }
            if (nightK > 0.8f && !Running && t > cricketNext)
            {
                cricketNext = t + 30 + Rand * 25;
                SoundRequested?.Invoke(StoneSound.Crickets);
            }

            StepAtmosphere(t, dt, now);
            EmitWeather(dt);

            bool calm = t - humT > 3.8f && !petting && !butterfly.Active && !bird.Active && !dragonfly.Active && t - stretchT > 3 && !DemoActive(t) && t - pokeT > 2 && t - dizzyT > 3;
            Schedule(t, calm);
            StepButterfly(t);
            StepBird(t);
            StepSnail(t);
            StepFrog(t);
            StepCat(t, dt);
            StepDragonfly(t);
            StepBits(dt, t);
            pose = Pose(t);
            SmoothLook(pose, dt);
        }

        float LightSide()
        {
            var now = Clock();
            if (state.Sun.Valid)
            {
                double span = Math.Max(1, (state.Sun.Set - state.Sun.Rise).TotalMinutes);
                return SpiritArt.Clamp((float)((now - state.Sun.Rise).TotalMinutes / span * 2 - 1), -1, 1);
            }
            return SpiritArt.Clamp(((float)now.TimeOfDay.TotalHours - 13) / 6, -1, 1);
        }

        void StepAtmosphere(float t, float dt, DateTime now)
        {
            if (t > gustNext) { gustT = t; gustNext = t + 18 + Rand * 25; }
            float gust = SpiritArt.Sin(SpiritArt.Clamp((t - gustT) / 3.5f, 0, 1) * SpiritArt.Pi);
            wind = 0.35f * SpiritArt.Sin(t * 0.37f) + 0.2f * SpiritArt.Sin(t * 0.91f + 1) + 0.9f * gust;
            windDrift += dt * wind * 6;
            float tx = 0, ty = 0;
            if (pointerIn && t - pointerT < 4) { tx = SpiritArt.Clamp((pointer.X - 150) / 150, -1, 1); ty = SpiritArt.Clamp((pointer.Y - 150) / 150, -1, 1); }
            float pa = 1 - (float)Math.Exp(-dt * 3);
            parX += (tx - parX) * pa;
            parY += (ty - parY) * pa;
            float target = Running ? state.Progress : 0;
            if (target < shownProgress - 0.3f) shownProgress = target;
            else shownProgress += (target - shownProgress) * (1 - (float)Math.Exp(-dt * 4));
            bool hot = season == Season.Summer && now.Hour >= 12 && now.Hour < 16 && nightK < 0.2f && cloudK < 0.4f;
            shimmerK += ((hot ? 1 : 0) - shimmerK) * (1 - (float)Math.Exp(-dt * 0.5));
            if (nightK > 0.8f && cloudK < 0.5f && t > starNext)
            {
                starNext = t + 40 + Rand * 60;
                starT = t;
                starX = 50 + Rand * 140;
                starY = 22 + Rand * 45;
                float angle = 0.35f + Rand * 0.35f;
                starDx = SpiritArt.Cos(angle);
                starDy = SpiritArt.Sin(angle);
            }
            bool raining = rainK > 0.2f && !snowing;
            float topY = points.Head.Y - 6;
            splashAcc += raining ? dt * rainK * 7 : 0;
            while (splashAcc >= 1)
            {
                splashAcc -= 1;
                float x = SX + (Rand - 0.5f) * R * 1.3f;
                Emit(new Particle { Kind = Bit.Splash, X = x, Y = topY + 0.012f * (x - SX) * (x - SX), Life = 0.3f, Night = nightK });
            }
            dripAcc += raining || wetReal > 0.6f ? dt * 0.8f : 0;
            while (dripAcc >= 1)
            {
                dripAcc -= 1;
                float x = SX + (Rand - 0.5f) * R * 1.4f;
                Emit(new Particle { Kind = Bit.Drip, X = x, Y = topY + 12 + 0.01f * (x - SX) * (x - SX), Vy = 14, Life = 2.2f, Target = Ground - 4 });
            }
            if (snowing && rainK > 0.3f) snowPile = Math.Min(0.45f, snowPile + dt * rainK / 900);
            else snowPile = Math.Max(0, snowPile - dt / 1800);
            bool cold = season == Season.Winter || snowGround;
            if (cold && !Sleeping && t > vaporNext)
            {
                vaporNext = t + 2.6f + Rand;
                Emit(new Particle { Kind = Bit.Vapor, X = SX + lookX * 4, Y = Ground - R * 0.84f * 0.94f + R * 0.22f, Vx = 6 + wind * 6, Vy = -9, Life = 1.6f, Size = 2.5f });
            }
        }

        void SmoothLook(SpiritPose p, float dt)
        {
            if (!lookReady)
            {
                lookX = lookMidX = p.LookX;
                lookY = lookMidY = p.LookY;
                lookReady = true;
                return;
            }
            float a = 1 - (float)Math.Exp(-dt * 13);
            lookMidX += (p.LookX - lookMidX) * a;
            lookMidY += (p.LookY - lookMidY) * a;
            lookX += (lookMidX - lookX) * a;
            lookY += (lookMidY - lookY) * a;
            p.LookX = lookX;
            p.LookY = lookY;
        }

        void EmitWeather(float dt)
        {
            bool winter = snowing;
            rainAcc += dt * rainK * (winter ? 26 : 44);
            while (rainAcc >= 1)
            {
                rainAcc -= 1;
                if (winter) Emit(new Particle { Kind = Bit.Snow, X = 22 + Rand * 256, Y = 14 + Rand * 20, Vx = Rand * 8 - 4, Vy = 26 + Rand * 18, Size = 1 + Rand * 1.2f, Target = 178 + Rand * 46, Life = 12 });
                else Emit(new Particle { Kind = Bit.Rain, X = 30 + Rand * 250, Y = 12 + Rand * 30, Vx = -28, Vy = 240 + Rand * 50, Target = 176 + Rand * 50, Life = 2, Night = nightK });
            }
            float ambient = winter ? 2.2f : season == Season.Spring ? 0.5f : season == Season.Autumn ? 0.45f : 0;
            ambientAcc += dt * ambient;
            while (ambientAcc >= 1)
            {
                ambientAcc -= 1;
                if (winter) Emit(new Particle { Kind = Bit.Snow, X = 22 + Rand * 256, Y = 14 + Rand * 20, Vx = Rand * 6 - 3, Vy = 14 + Rand * 12, Size = 0.8f + Rand * 1.1f, Target = 178 + Rand * 46, Life = 16 });
                else if (season == Season.Spring) Emit(new Particle { Kind = Bit.Petal, X = 20 + Rand * 70, Y = 40 + Rand * 70, Vx = 14 + Rand * 10, Vy = 8 + Rand * 6, Vr = Rand * 2 - 1, Size = 2.4f, Target = 180 + Rand * 40, Life = 11 });
                else Emit(new Particle { Kind = Bit.Autumn, X = 16 + Rand * 50, Y = 40 + Rand * 90, Vx = 24 + Rand * 18, Vy = 9 + Rand * 8, Vr = 1.5f + Rand * 2, Size = 3.2f, Hue = rng.Next(4), Target = 180 + Rand * 40, Life = 11 });
            }
        }

        void Schedule(float t, bool calm)
        {
            if (Sleeping) return;
            if (bfAt > 0 && t > bfAt && calm && !Running && rainK < 0.3f)
            {
                bfAt = -1;
                if (rng.NextDouble() < 0.5) StartFlyer(butterfly, t); else StartFlyer(bird, t);
                nextAct = t + 14;
                return;
            }
            if (frogAt > 0 && t > frogAt && !Running && frog.State == 0) { frogAt = -1; frog.State = 1; frog.T0 = t; }
            if (!snail.Active && Running && state.SessionSeconds >= state.Interval * 0.9 && t > snailNext)
            {
                snail.State = 1;
                snail.T0 = t;
                Visit(Visitors.Snail);
                Chat("A snail. Even it moves more than us.", 3.4f, thought: true);
            }
            if (t < nextAct || !calm) return;
            nextAct = t + (state.Chat == Chattiness.Chatty ? 7 + Rand * 5 : 11 + Rand * 8);
            if (Running && Drowsiness > 0.45f) return;
            double r = rng.NextDouble();
            if (!Running && hasLast && r < 0.3 && rainK < 0.3f) { if (rng.NextDouble() < 0.5) StartFlyer(butterfly, t); else StartFlyer(bird, t); }
            else if (season == Season.Summer && !Running && nightK < 0.3f && r < 0.5 && !dragonfly.Active) StartDragonfly(t);
            else if (!Running && r < 0.4 && t - boredT > 60) boredT = t;
            else if (r < (state.Chat == Chattiness.Chatty ? 0.5 : 0.62)) humT = t;
            else Chat(Pick(Running ? ThoughtsSitting : ThoughtsFree), 3.4f, thought: true);
        }

        void StartFlyer(Critter c, float t)
        {
            if (c.Active) return;
            c.Side = rng.NextDouble() < 0.5 ? -1 : 1;
            c.State = 1;
            c.T0 = t;
            c.From = new PointF(SX + c.Side * 180, 40 + Rand * 50);
        }

        void SendAway(Critter c, float t)
        {
            if (c.State != 1 && c.State != 2) return;
            c.State = 3;
            c.T0 = t;
            c.From = new PointF(c.X, c.Y);
            c.To = new PointF(SX - c.Side * 190, -14);
        }

        void StepButterfly(float t)
        {
            var c = butterfly;
            if (c.State == 1)
            {
                float u = SpiritArt.Clamp((t - c.T0) / 3.4f, 0, 1), e = SpiritArt.Smooth(0, 1, u);
                c.X = c.From.X + (points.Tip.X - c.From.X) * e + SpiritArt.Sin(u * 13) * 16 * (1 - u);
                c.Y = c.From.Y + (points.Tip.Y - 5 - c.From.Y) * e + SpiritArt.Cos(u * 9) * 12 * (1 - u);
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Butterfly);
                    Chat(Pick(new[] { "Oh. Hello there.", "A visitor!", "Shh. Don't move." }), 2.4f);
                }
            }
            else if (c.State == 2)
            {
                c.X = points.Tip.X;
                c.Y = points.Tip.Y - 5;
                if (t - c.T0 > 4.5f || Running || Sleeping) SendAway(c, t);
            }
            else if (c.State == 3) FlyOut(c, t, 3);
        }

        void StepBird(float t)
        {
            var c = bird;
            if (c.State == 1)
            {
                float u = SpiritArt.Clamp((t - c.T0) / 2.6f, 0, 1), e = SpiritArt.Smooth(0, 1, u);
                c.X = c.From.X + (points.Head.X - c.From.X) * e;
                c.Y = c.From.Y + (points.Head.Y - c.From.Y) * e - SpiritArt.Sin(u * SpiritArt.Pi) * 18;
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Bird);
                    Chat(Pick(new[] { "A little bird. It tickles.", "Tweet. That's bird for hello.", "We have company." }), 2.6f);
                }
            }
            else if (c.State == 2)
            {
                float since = t - c.T0;
                float hop = since % 1.6f < 0.25f ? SpiritArt.Sin(since % 1.6f / 0.25f * SpiritArt.Pi) * 3 : 0;
                c.X = points.Head.X;
                c.Y = points.Head.Y - hop;
                if (since % 2.2f < 0.05f && since > 0.5f) Emit(new Particle { Kind = Bit.Note, X = c.X + 6 * -c.Side, Y = c.Y - 12, Vx = -c.Side * 10, Vy = -22, Vr = 0.3f, Life = 1.2f, Size = 8 });
                if (since > 5.5f || Running || Sleeping) SendAway(c, t);
            }
            else if (c.State == 3) FlyOut(c, t, 2.4f);
        }

        void FlyOut(Critter c, float t, float seconds)
        {
            float u = SpiritArt.Clamp((t - c.T0) / seconds, 0, 1), e = u * u;
            c.X = c.From.X + (c.To.X - c.From.X) * e + SpiritArt.Sin(u * 11) * 10;
            c.Y = c.From.Y + (c.To.Y - c.From.Y) * e + SpiritArt.Cos(u * 8) * 6;
            if (u >= 1) c.State = 0;
        }

        void StepSnail(float t)
        {
            if (snail.State != 1) return;
            float u = (t - snail.T0) / 80f;
            snail.X = 262 - 218 * u;
            snail.Y = 213;
            if (u >= 1)
            {
                snail.State = 0;
                snailNext = t + 180;
            }
        }

        void StepFrog(float t)
        {
            var c = frog;
            float since = t - c.T0;
            if (c.State == 1)
            {
                float u = SpiritArt.Clamp(since / 1.6f, 0, 1);
                c.X = 22 + (GardenArt.FrogAt.X - 22) * u;
                c.Y = GardenArt.FrogAt.Y - Math.Abs(SpiritArt.Sin(u * SpiritArt.Pi * 2)) * 9;
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Frog);
                    Chat("Ribbit. The rain brought a friend.", 3f);
                }
            }
            else if (c.State == 2)
            {
                c.X = GardenArt.FrogAt.X;
                c.Y = GardenArt.FrogAt.Y;
                float phase = since % 2.6f;
                c.K = phase < 0.9f ? SpiritArt.Sin(phase / 0.9f * SpiritArt.Pi) : 0;
                if (since > 11 || Running) { c.State = 3; c.T0 = t; c.K = 0; }
            }
            else if (c.State == 3)
            {
                float u = SpiritArt.Clamp(since / 1.6f, 0, 1);
                c.X = GardenArt.FrogAt.X - (GardenArt.FrogAt.X - 10) * u;
                c.Y = GardenArt.FrogAt.Y - Math.Abs(SpiritArt.Sin(u * SpiritArt.Pi * 2)) * 9;
                if (u >= 1) c.State = 0;
            }
        }

        void StepCat(float t, float dt)
        {
            float target = nightK > 0.7f && Sleeping ? 1 : 0;
            cat.K += (target - cat.K) * (1 - (float)Math.Exp(-dt * 0.45));
            if (cat.K > 0.9f && !catSeen) { catSeen = true; Visit(Visitors.Cat); }
            if (cat.K < 0.05f) catSeen = false;
        }

        void StepBits(float dt, float t)
        {
            bool pond = (state.Keepsakes & Keepsake.Pond) != 0;
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
                    continue;
                }
                q.X += q.Vx * dt;
                q.Y += q.Vy * dt;
                q.Rot += q.Vr * dt;
                switch (q.Kind)
                {
                    case Bit.Rain:
                        if (q.Y >= q.Target)
                        {
                            float dx = (q.X - GardenArt.PondAt.X) / GardenArt.PondRx, dy = (q.Target - GardenArt.PondAt.Y) / GardenArt.PondRy;
                            bool inPond = pond && dx * dx + dy * dy < 1 && season != Season.Winter;
                            q.Kind = inPond ? Bit.Ripple : Bit.Splash;
                            if (inPond) q.Y = GardenArt.PondAt.Y + (q.Target - GardenArt.PondAt.Y) * 0.4f;
                            q.Vx = q.Vy = q.Vr = 0;
                            q.Age = 0;
                            q.Life = inPond ? 0.9f : 0.3f;
                        }
                        break;
                    case Bit.Leaf:
                    case Bit.Snow:
                    case Bit.Autumn:
                    case Bit.Petal:
                        if (q.Kind == Bit.Leaf) q.Vy = Math.Min(q.Vy + 18 * dt, 26);
                        float sway = q.Kind == Bit.Snow ? 5 : q.Kind == Bit.Petal ? 8 : q.Kind == Bit.Autumn ? 12 : 22;
                        q.X += SpiritArt.Sin(q.Age * 2.6f + q.Size) * sway * dt;
                        if (q.Vy != 0) q.X += wind * (q.Kind == Bit.Snow ? 10 : 22) * dt;
                        if (q.Kind == Bit.Petal && q.Vy != 0 && petalsOnHead < 3 && Math.Abs(q.X - SX) < R * 0.55f && Math.Abs(q.Y - (points.Head.Y - 4 + 0.012f * (q.X - SX) * (q.X - SX))) < 3 && rng.NextDouble() < 0.4)
                        {
                            petalsOnHead++;
                            bits.RemoveAt(i);
                            continue;
                        }
                        if (q.Target > 0 && q.Y >= q.Target && q.Vy != 0)
                        {
                            q.Vx = q.Vy = q.Vr = 0;
                            q.Y = q.Target;
                            q.Life = Math.Min(q.Life, q.Age + (q.Kind == Bit.Snow ? 1.4f : 3f));
                        }
                        break;
                    case Bit.Puff:
                        q.Vx *= 0.94f;
                        q.Vy *= 0.94f;
                        q.X += wind * 8 * dt;
                        break;
                    case Bit.Spark:
                        q.Vy += 90 * dt;
                        break;
                    case Bit.Drip:
                        if (q.Y >= q.Target) { q.Vy = 0; q.Life = Math.Min(q.Life, q.Age + 0.3f); }
                        break;
                    case Bit.Vapor:
                        q.Vx *= 0.98f;
                        q.Vy *= 0.97f;
                        break;
                }
                float ox = q.X - 150, oy = q.Y - 150;
                if (ox * ox + oy * oy > 137 * 137) bits.RemoveAt(i);
            }
        }

        SpiritPose BasePose(float t)
        {
            var p = new SpiritPose
            {
                Night = Math.Max(0, nightK),
                Leaves = Math.Min(3, 1 + Math.Max(0, breaks)),
                Stem = Math.Min(4, Math.Max(0, breaks)),
                Bloom = breaks >= Garden.FlowerBreaks && season != Season.Winter ? SpiritArt.Clamp((t - bloomT) / 1.5f, 0, 1) : 0,
                Snow = snowGround ? 1 + snowPile : snowing ? snowPile * 2 : 0,
                LightX = LightSide(),
                Warm = Sky.Glow(Clock(), state.Sun),
                Wet = snowing ? 0 : Math.Max(rainK, wetReal * 0.6f),
                Wind = wind,
                Petals = petalsOnHead,
                Brow = state.Away && Running ? Brow.Worried : butterfly.Active || bird.Active || dragonfly.Active ? Brow.Curious : Running && Drowsiness > 0.75f ? Brow.Sleepy : Brow.None,
                Wilt = breaks < Garden.FlowerBreaks && state.BreaksYesterday >= Garden.FlowerBreaks && season != Season.Winter ? 1 : 0,
                Bud = breaks >= 2 && breaks < Garden.FlowerBreaks && season != Season.Winter && state.BreaksYesterday < Garden.FlowerBreaks ? 1 : 0,
            };
            float b = SpiritArt.Sin(t * 1.7f) * 0.014f;
            p.Sy += b;
            p.Sx -= b * 0.5f;
            float blink = t % 3.9f;
            if (blink < 0.16f) p.EyeOpen = Math.Abs(blink - 0.08f) / 0.08f;

            Critter flyer = butterfly.Active ? butterfly : bird.Active ? bird : null;
            bool target = false;
            float tx = 0, ty = 0;
            if (flyer != null) { tx = flyer.X; ty = flyer.Y; target = true; }
            else if (pointerIn && t - pointerT < 4) { tx = pointer.X; ty = pointer.Y; target = true; }
            else if (snail.Active) { tx = snail.X; ty = snail.Y; target = true; }
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

            if (Running && state.Away)
            {
                p.EyeOpen = 1;
                p.LookX = SpiritArt.Sin(t * 1.1f);
                p.LookY = -0.15f;
                p.Mouth = Mouth.O;
                p.Yawn = 0.12f;
                p.Tilt = SpiritArt.Sin(t * 1.1f) * 0.05f;
                p.Sprout = 0.7f;
            }
            else if (Running)
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
                        float k = SpiritArt.Sin((u - 0.86f) / 0.07f * SpiritArt.Pi);
                        p.EyeOpen = 1; p.Dy -= 6 * k; p.Mouth = Mouth.O; p.Yawn = 0.3f; p.LookY = 0;
                    }
                }
                float yt = t - yawnT;
                if (dd > 0.35f && (t % 19 < 2.2f || yt < 2.2f))
                {
                    float ph = yt < 2.2f ? yt : t % 19;
                    float k = SpiritArt.Sin(ph / 2.2f * SpiritArt.Pi);
                    p.Mouth = Mouth.O; p.Yawn = k; p.EyeOpen *= 1 - k; p.Sy += 0.05f * k; p.Tilt -= 0.05f * k;
                }
            }
            else if (hasLast)
            {
                p.Mouth = Mouth.Smile;
                p.Blush = 0.45f;
                float since = t - stretchT;
                if (since < 8 && !DemoActive(t)) p.Dy = -Math.Abs(SpiritArt.Sin(t * 4.2f)) * 4 * (1 - since / 8);
            }

            if (t - humT < 3.8f)
            {
                p.Eyes = Eyes.Happy; p.Mouth = Mouth.O; p.Yawn = 0.08f + 0.06f * SpiritArt.Sin(t * 9); p.Tilt += SpiritArt.Sin(t * 3.2f) * 0.08f; p.LookX = 0; p.LookY = 0;
                if (t > noteT)
                {
                    noteT = t + 0.55f;
                    Emit(new Particle { Kind = Bit.Note, X = SX + 34, Y = 112, Vx = 10 + Rand * 10, Vy = -26, Vr = 0.3f, Life = 1.8f, Size = 12 });
                }
            }
            if (butterfly.State == 2 || bird.State == 2) { p.Mouth = Mouth.O; p.Yawn = 0.15f; p.Blush = 0.55f; }
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
                    float k = SpiritArt.Sin(s / 0.6f * SpiritArt.Pi);
                    p.Sy = 1 + 0.22f * k; p.Sx = 1 - 0.12f * k; p.Eyes = Eyes.Squeeze; p.Mouth = Mouth.O; p.Yawn = 0.7f * k; p.Sprout = 1; p.LookX = 0; p.LookY = 0;
                    if (!sparked && s > 0.28f)
                    {
                        sparked = true;
                        for (int i = 0; i < 8; i++)
                        {
                            float a = -SpiritArt.Pi / 2 + (i - 3.5f) * 0.35f;
                            Emit(new Particle { Kind = Bit.Spark, X = SX, Y = 110, Vx = SpiritArt.Cos(a) * 70, Vy = SpiritArt.Sin(a) * 70, Vr = 4, Life = 0.9f, Size = 4 + Rand * 3 });
                        }
                    }
                }
                else
                {
                    float h = SpiritArt.Sin((s - 0.6f) / 0.4f * SpiritArt.Pi);
                    p.Dy = -26 * h; p.Sy = 1 + 0.06f * h; p.Sx = 1 - 0.03f * h; p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Sprout = 1;
                    if (!landed && s > 0.96f)
                    {
                        landed = true;
                        landT = t;
                        SoundRequested?.Invoke(StoneSound.Chime);
                        if (season == Season.Autumn)
                            for (int i = 0; i < 4; i++)
                            {
                                int side = i % 2 == 1 ? 1 : -1;
                                Emit(new Particle { Kind = Bit.Autumn, X = SX + side * (44 + Rand * 20), Y = Ground - 2, Vx = side * (25 + Rand * 25), Vy = -28 - Rand * 18, Vr = 3, Size = 3.2f, Hue = rng.Next(4), Target = Ground + 4 + Rand * 14, Life = 3 });
                            }
                        for (int i = 0; i < 6; i++)
                        {
                            int side = i % 2 == 1 ? 1 : -1;
                            Emit(new Particle { Kind = Bit.Puff, X = SX + side * (30 + Rand * 14), Y = Ground - 3, Vx = side * (30 + Rand * 30), Vy = -6 - Rand * 8, Life = 0.7f, Size = 3 + Rand * 2 });
                        }
                    }
                }
            }
            if (DemoActive(t) && !Sleeping && !Running) ApplyStretch(p, t - demoT);
            if (petting && !Sleeping)
            {
                p.Eyes = Eyes.Happy; p.Mouth = Mouth.Grin; p.Blush = 0.75f; p.Sy -= 0.03f; p.Sx += 0.02f;
                p.Tilt += SpiritArt.Clamp((pointer.X - SX) / 300, -0.12f, 0.12f);
            }
            float pt = t - pokeT;
            if (pt >= 0 && pt < 1)
            {
                float w = (float)Math.Exp(-pt * 4) * SpiritArt.Sin(pt * 26);
                float jelly = (float)Math.Exp(-pt * 5) * SpiritArt.Sin(pt * 34 + 1);
                p.Tilt += 0.18f * w; p.Sx += 0.06f * w + 0.07f * jelly; p.Sy -= 0.04f * w + 0.08f * jelly;
                if (!Sleeping) { p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Blush = 0.6f; }
                else if (pt < 0.5f) { p.Eyes = Eyes.Open; p.EyeOpen = 0.3f; }
            }
            float lt = t - landT;
            if (lt >= 0 && lt < 0.7f)
            {
                float settle = (float)Math.Exp(-lt * 7) * SpiritArt.Cos(lt * 22);
                p.Sy -= 0.1f * settle;
                p.Sx += 0.08f * settle;
            }
            if (t - boredT < 2.6f && !Running && !Sleeping) { p.Mouth = Mouth.Tongue; p.Eyes = Eyes.Happy; }
            float zt = t - dizzyT;
            if (zt >= 0 && zt < 2.8f)
            {
                float f = 1 - zt / 2.8f;
                p.Eyes = Eyes.Dizzy; p.Mouth = Mouth.Wavy; p.Tilt += 0.16f * SpiritArt.Sin(t * 7) * f; p.Sx += 0.03f * SpiritArt.Sin(t * 9) * f;
            }
            return p;
        }

        void ApplyStretch(SpiritPose p, float s)
        {
            float fade = SpiritArt.Smooth(0, 0.5f, s) * (1 - SpiritArt.Smooth(DemoLength - 0.5f, DemoLength, s));
            float tilt = 0, dy = 0, sx = 1, sy = 1, lookX = p.LookX, lookY = p.LookY;
            switch (demoIndex)
            {
                case 0:
                    float a = s * 1.6f;
                    tilt = 0.16f * SpiritArt.Sin(a);
                    dy = -2 - 2 * SpiritArt.Cos(a);
                    lookX = 0.6f * SpiritArt.Sin(a);
                    lookY = -0.3f * SpiritArt.Cos(a);
                    p.Eyes = Eyes.Closed;
                    break;
                case 1:
                    float u = s % 2 / 2;
                    float up = u < 0.45f ? SpiritArt.Smooth(0, 0.3f, u) : 1 - SpiritArt.Smooth(0.45f, 0.6f, u);
                    sy = 1 + 0.1f * up; sx = 1 - 0.05f * up; dy = -3 * up;
                    p.Eyes = up > 0.5f ? Eyes.Squeeze : Eyes.Happy;
                    break;
                case 2:
                    float w = s % 4 / 4;
                    float k = w < 0.25f ? SpiritArt.Smooth(0, 0.25f, w) : w < 0.7f ? 1 : 1 - SpiritArt.Smooth(0.7f, 0.9f, w);
                    sy = 1 + 0.24f * k; sx = 1 - 0.12f * k;
                    p.Eyes = k > 0.6f ? Eyes.Squeeze : Eyes.Open;
                    p.Mouth = Mouth.O;
                    p.Yawn = 0.3f * k;
                    p.Sprout = 1;
                    break;
                case 3:
                    float v = s % 4 / 4;
                    float side = v < 0.5f ? -1 : 1;
                    float hold = SpiritArt.Sin((v % 0.5f) / 0.5f * SpiritArt.Pi);
                    tilt = 0.28f * side * hold;
                    sx = 1 + 0.03f * hold;
                    p.Eyes = Eyes.Happy;
                    break;
                default:
                    lookX = SpiritArt.Sin(s * 0.8f);
                    lookY = -0.4f;
                    p.EyeOpen = 1;
                    p.Mouth = Mouth.Smile;
                    break;
            }
            p.Tilt += tilt * fade;
            p.Dy += dy * fade;
            p.Sx += (sx - 1) * fade;
            p.Sy += (sy - 1) * fade;
            p.LookX += (lookX - p.LookX) * fade;
            p.LookY += (lookY - p.LookY) * fade;
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
            if (Running && Drowsiness > 0.5f) { wakeT = t; Say(Pick(new[] { "Huh? I'm awake. I'm awake.", "Was I snoring?" })); }
            else Say(Pick(new[] { "Hehe.", "That tickles.", "Hi!", "Boop." }), 1.6f);
        }

        public void ShowVisitor(string visitor)
        {
            float t = Now;
            if (visitor == Visitors.Butterfly) StartFlyer(butterfly, t);
            else if (visitor == Visitors.Bird) StartFlyer(bird, t);
            else if (visitor == Visitors.Snail) { snail.State = 1; snail.T0 = t - 30; }
            else if (visitor == Visitors.Frog) { frog.State = 1; frog.T0 = t; }
            else if (visitor == Visitors.Rainbow) rainbowT = t - 3;
        }

        void EnsureFonts(float size)
        {
            int key = (int)size * 1000 + DeviceDpi;
            if (key == fontKey && timeFont != null) return;
            fontKey = key;
            timeFont?.Dispose();
            captionFont?.Dispose();
            bubbleFont?.Dispose();
            plainFont?.Dispose();
            float pt = 72f / Math.Max(1, DeviceDpi);
            timeFont = Theme.Light(Math.Max(6f, size * 0.1f * pt));
            captionFont = Theme.SemiBold(Math.Max(5f, size * 0.033f * pt));
            bubbleFont = Theme.Regular(Math.Max(6f, size * 0.046f * pt));
            plainFont = Theme.Light(Math.Max(6f, size * 0.15f * pt));
        }

        void RenderPlain(Graphics g)
        {
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height);
            if (size < 20) return;
            var o = Origin;
            Draw.Ring(g, new RectangleF(o.X, o.Y, size, size), size * 0.04f, Theme.Track, Theme.Accent, Running ? state.Progress : 0);
            EnsureFonts(size);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float cx = o.X + size / 2, cy = o.Y + size / 2;
            CenteredText(g, state.Caption, captionFont, Theme.Muted, cx, cy - size * 0.14f);
            CenteredText(g, state.Time, plainFont, Running ? Theme.Ink : Theme.Muted, cx, cy);
            CenteredText(g, state.Note, bubbleFont, Theme.AccentText, cx, cy + size * 0.14f);
        }

        Bitmap backCache, groundCache, frameCache, fadeShot;
        long backKey = -1, groundKey = -1;
        int frameKey = -1;
        float fadeT = -99;

        Bitmap Layer(ref Bitmap cache, ref long cacheKey, long key, int px, float s, Action<Graphics> draw)
        {
            if (cache != null && key == cacheKey && cache.Width == px) return cache;
            if (cache == null || cache.Width != px)
            {
                cache?.Dispose();
                cache = new Bitmap(px, px, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }
            cacheKey = key;
            using (var g = Graphics.FromImage(cache))
            using (var disc = Disc(143))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(s, s);
                g.SetClip(disc, CombineMode.Intersect);
                draw(g);
            }
            return cache;
        }

        Bitmap FrameImage(int px, float s)
        {
            if (frameCache != null && frameKey == px) return frameCache;
            frameCache?.Dispose();
            frameCache = new Bitmap(px, px, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            frameKey = px;
            using (var g = Graphics.FromImage(frameCache))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(s, s);
                GardenArt.DrawDiscShadow(g);
            }
            return frameCache;
        }

        static GraphicsPath Disc(float r = 140)
        {
            var disc = new GraphicsPath();
            disc.AddEllipse(150 - r, 150 - r, r * 2, r * 2);
            return disc;
        }

        float RainbowLevel(float t)
        {
            float since = t - rainbowT;
            if (since < 0 || since > 50) return 0;
            return SpiritArt.Smooth(0, 3, since) * (1 - SpiritArt.Smooth(42, 50, since));
        }

        float ZoomLevel(float t)
        {
            float since = t - zoomT;
            if (since < 0 || since > 2.6f) return 1;
            return 1 + 0.07f * SpiritArt.Smooth(0, 0.6f, since) * (1 - SpiritArt.Smooth(1.6f, 2.6f, since));
        }

        bool Fading => fadeShot != null && Now - fadeT < 0.45f;

        protected override void OnPaint(PaintEventArgs e) => Render(e.Graphics, Now);

        public void Render(Graphics g, float t)
        {
            if (plain) RenderPlain(g);
            else RenderGarden(g, t);
            if (fadeShot == null) return;
            float k = (t - fadeT) / 0.45f;
            if (k >= 1)
            {
                fadeShot.Dispose();
                fadeShot = null;
                return;
            }
            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 1 - SpiritArt.Smooth(0, 1, k) });
                g.DrawImage(fadeShot, new Rectangle(0, 0, fadeShot.Width, fadeShot.Height), 0, 0, fadeShot.Width, fadeShot.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        void RenderGarden(Graphics g, float t)
        {
            g.Clear(Parent?.BackColor ?? Theme.Background);
            if (pose == null) pose = Pose(t);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height), s = size / 300f;
            if (size < 20) return;
            var o = Origin;
            int px = (int)Math.Ceiling(size) + 1;
            g.DrawImage(FrameImage(px, s), new Rectangle((int)o.X, (int)o.Y, px, px));
            var state0 = g.Save();
            float night = Math.Max(0, nightK);
            float z = ZoomLevel(t);
            if (z > 1.0005f)
            {
                float zx = o.X + 150 * s, zy = o.Y + 160 * s;
                using (var disc = Disc(143))
                using (var m = new Matrix())
                {
                    m.Translate(o.X, o.Y);
                    m.Scale(s, s);
                    disc.Transform(m);
                    g.SetClip(disc, CombineMode.Intersect);
                }
                g.TranslateTransform(zx, zy);
                g.ScaleTransform(z, z);
                g.TranslateTransform(-zx, -zy);
            }
            var now = Clock();
            var look = new SceneLook
            {
                Night = night,
                Season = season,
                Clouds = cloudK,
                RakeLines = 2 + Math.Min(Math.Max(0, breaks), 8),
                NewestRake = breaks > 0 ? SpiritArt.Clamp((t - rakeT) / 1.4f, 0, 1) : 1,
                Keepsakes = state.Keepsakes,
                Snowman = season != Season.Winter ? 0 : breaks >= Garden.FlowerBreaks ? SpiritArt.Clamp((t - snowmanT) / 1.5f, 0, 1) : state.BreaksYesterday >= Garden.FlowerBreaks ? 1 : 0,
                SnowmanMelted = season == Season.Winter && breaks < Garden.FlowerBreaks && state.BreaksYesterday >= Garden.FlowerBreaks,
                Rainbow = RainbowLevel(t),
                Dusk = Sky.Glow(now, state.Sun),
                MoonPhase = Sky.MoonPhase(now),
                Southern = state.Southern,
                Wet = Math.Max(wetReal, AwayWet(t)),
                SnowGround = snowGround,
                FadedRakes = state.BreaksYesterday > breaks ? 2 + Math.Min(state.BreaksYesterday, 8) : 0,
                FadeAlpha = 0.65f - 0.45f * SpiritArt.Clamp((float)now.TimeOfDay.TotalHours / 18f, 0, 1),
                Shafts = MorningLight(now),
                Halo = Math.Max(fogK, cloudK > 0.15f && cloudK < 0.8f ? 0.6f : 0),
            };
            float bx = -parX * 3, by = -parY * 2, gx = -parX * 1.4f, gy = -parY * 0.8f;
            var back = Layer(ref backCache, ref backKey, look.BackKey(px), px, s, bg => GardenArt.DrawBackground(bg, look, new RectangleF(-10, -10, 320, 320)));
            g.DrawImage(back, new Rectangle((int)Math.Round(o.X + bx * s), (int)Math.Round(o.Y + by * s), px, px));

            var sky = g.Save();
            g.TranslateTransform((float)Math.Round(o.X + bx * s), (float)Math.Round(o.Y + by * s));
            g.ScaleTransform(s, s);
            GardenArt.DrawStars(g, t, night * (1 - cloudK));
            if (t - starT < 0.9f) GardenArt.DrawShootingStar(g, starX + starDx * (t - starT) * 160, starY + starDy * (t - starT) * 160, starDx, starDy, (t - starT) / 0.9f);
            if (cloudK > 0.02f || fogK > 0.02f)
            {
                using (var disc = Disc()) g.SetClip(disc, CombineMode.Intersect);
                GardenArt.DrawClouds(g, t + windDrift, cloudK, night, new RectangleF(0, 0, 300, 300));
                GardenArt.DrawFog(g, t, fogK, night);
            }
            g.Restore(sky);

            var groundImage = Layer(ref groundCache, ref groundKey, look.GroundKey(px), px, s, gg => GardenArt.DrawGround(gg, look));
            g.DrawImage(groundImage, new Rectangle((int)Math.Round(o.X + gx * s), (int)Math.Round(o.Y + gy * s), px, px));
            g.TranslateTransform((float)Math.Round(o.X + gx * s), (float)Math.Round(o.Y + gy * s));
            g.ScaleTransform(s, s);
            GardenArt.DrawTufts(g, season, night, wind);
            if ((state.Keepsakes & Keepsake.Pond) != 0 && !snowGround) GardenArt.DrawPondGlints(g, t, night);
            if ((state.Keepsakes & Keepsake.Lantern) != 0) GardenArt.DrawLanternFlicker(g, t, night);
            GardenArt.DrawHeatShimmer(g, t, shimmerK);
            float fireflies = Math.Max(night, season == Season.Summer && NightHour(19) ? 0.6f : 0);
            GardenArt.DrawFireflies(g, t, fireflies * (1 - cloudK * 0.6f));
            bool clipped = bird.Active || butterfly.Active || dragonfly.Active || cat.K > 0.02f || frog.Active;
            var clip = g.Save();
            if (clipped) using (var disc = Disc()) g.SetClip(disc, CombineMode.Intersect);
            if (traces.Any) GardenArt.DrawTraces(g, traces, night, snowGround);
            points = SpiritArt.DrawSpirit(g, SX, Ground, R, pose, t);
            if ((state.Keepsakes & Keepsake.Baby) != 0)
                SpiritArt.DrawSpirit(g, GardenArt.BabyAt.X, GardenArt.BabyAt.Y, GardenArt.BabyR, GardenArt.BabyPose(pose, t), t);
            if (cat.K > 0.02f) SpiritArt.DrawCat(g, GardenArt.CatAt.X + (1 - cat.K) * 80, GardenArt.CatAt.Y, t);
            if (snail.Active)
            {
                using (var trail = new Pen(Color.FromArgb(50, 255, 255, 255), 1.2f)) g.DrawLine(trail, snail.X + 8, snail.Y - 0.5f, 262, snail.Y - 0.5f);
                SpiritArt.DrawSnail(g, snail.X, snail.Y, t, -1);
            }
            if (frog.Active) SpiritArt.DrawFrog(g, frog.X, frog.Y, t, frog.K);
            SpiritArt.DrawParticles(g, bits);
            if (bird.Active) SpiritArt.DrawBird(g, bird.X, bird.Y, t, bird.State == 3 ? bird.To.X - bird.From.X : -bird.Side, bird.State != 2);
            if (butterfly.Active) SpiritArt.DrawButterfly(g, butterfly.X, butterfly.Y, t, butterfly.State == 2);
            if (dragonfly.Active) GardenArt.DrawDragonfly(g, dragonfly.X, dragonfly.Y, t, dragonfly.Side);
            g.Restore(clip);
            float flash = t - flashT;
            if (flash >= 0 && flash < 0.45f)
            {
                float k = flash < 0.08f ? 1 : flash < 0.16f ? 0.2f : flash < 0.24f ? 0.7f : 1 - SpiritArt.Smooth(0.24f, 0.45f, flash);
                SpiritArt.FillEllipse(g, SpiritArt.Alpha(Color.White, 0.45f * k), 150, 150, 143, 143);
            }
            g.Restore(state0);

            var ringState = g.Save();
            g.TranslateTransform(o.X, o.Y);
            g.ScaleTransform(s, s);
            bool near = Running && (1 - state.Progress) * state.Interval < 120;
            float pulse = near ? 0.5f + 0.5f * SpiritArt.Sin(t * 4) : 0;
            Draw.Ring(g, new RectangleF(1, 1, 298, 298), 10 + 1.6f * pulse, Theme.Track, Theme.Accent, shownProgress);
            if (shownProgress > 0.005f && shownProgress < 0.999f)
            {
                float a = (-90 + 360 * shownProgress) * SpiritArt.Pi / 180, r = 144;
                float tx = 150 + SpiritArt.Cos(a) * r, ty = 150 + SpiritArt.Sin(a) * r;
                SpiritArt.FillRadial(g, tx, ty, 11 + 3 * pulse, 11 + 3 * pulse, SpiritArt.Alpha(Theme.Accent, 0.55f + 0.3f * pulse), SpiritArt.Alpha(Theme.Accent, 0));
            }
            g.Restore(ringState);

            EnsureFonts(size);
            bool dark = night > 0.5f;
            string cap = state.Caption;
            if (DemoActive(t) && !Running) cap = "STRETCH  ·  " + Stretches[demoIndex].Name;
            var timeColor = dark ? SpiritArt.Hex(0xE6E2DA) : Running ? SpiritArt.Ink : SpiritArt.Hex(0x5F5C5A);
            var capColor = dark ? SpiritArt.Hex(0xB9BACB) : SpiritArt.Hex(0x5F5C5A);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            RollingText(g, state.Time, timeFont, timeColor, o.X + 150 * s, o.Y + 234 * s, t);
            CenteredText(g, cap, captionFont, capColor, o.X + 150 * s, o.Y + 257 * s);
            DrawBubble(g, t, o, s);
        }

        string shownTime = "";
        float[] rollT = new float[0];
        string rollOld = "";

        void RollingText(Graphics g, string text, Font font, Color color, float cx, float cy, float t)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text != shownTime)
            {
                if (text.Length == shownTime.Length)
                {
                    var times = new float[text.Length];
                    for (int i = 0; i < text.Length; i++) times[i] = text[i] != shownTime[i] ? t : (i < rollT.Length ? rollT[i] : -99);
                    rollT = times;
                    rollOld = shownTime;
                }
                else
                {
                    rollT = new float[text.Length];
                    for (int i = 0; i < rollT.Length; i++) rollT[i] = -99;
                    rollOld = text;
                }
                shownTime = text;
            }
            var size = Measure(g, text, font);
            float emPx = font.SizeInPoints * g.DpiY / 72f;
            float ascent = emPx * font.FontFamily.GetCellAscent(font.Style) / font.FontFamily.GetEmHeight(font.Style);
            float top = cy + emPx * 0.36f - ascent, left = cx - size.Width / 2, lineH = emPx * 1.2f;
            var band = g.Save();
            g.SetClip(new RectangleF(left - 4, top + emPx * 0.05f, size.Width + 8, lineH * 0.95f), CombineMode.Intersect);
            for (int i = 0; i < text.Length; i++)
            {
                float x = left + (i == 0 ? 0 : Measure(g, text.Substring(0, i), font).Width);
                float k = i < rollT.Length ? SpiritArt.Smooth(0, 0.3f, t - rollT[i]) : 1;
                if (k >= 1 || i >= rollOld.Length)
                {
                    using (var b = new SolidBrush(color)) g.DrawString(text[i].ToString(), font, b, x, top, Tight);
                    continue;
                }
                using (var oldBrush = new SolidBrush(SpiritArt.Alpha(color, (color.A / 255f) * (1 - k))))
                    g.DrawString(rollOld[i].ToString(), font, oldBrush, x, top - k * emPx * 0.7f, Tight);
                using (var newBrush = new SolidBrush(SpiritArt.Alpha(color, (color.A / 255f) * k)))
                    g.DrawString(text[i].ToString(), font, newBrush, x, top + (1 - k) * emPx * 0.7f, Tight);
            }
            g.Restore(band);
        }

        static float MorningLight(DateTime now)
        {
            float h = (float)now.TimeOfDay.TotalHours;
            return SpiritArt.Smooth(6.5f, 7.5f, h) * (1 - SpiritArt.Smooth(9.5f, 11f, h));
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
            float maxWidth = 272 * s;
            float padX = 12 * s, padY = 8 * s, lineH = bubbleFont.SizeInPoints * g.DpiY / 72f * 1.25f;
            var lines = new List<string> { bubble };
            if (Measure(g, bubble, bubbleFont).Width > maxWidth - padX * 2)
            {
                int mid = bubble.Length / 2, cut = -1;
                for (int d = 0; d < mid && cut < 0; d++)
                {
                    if (bubble[mid - d] == ' ') cut = mid - d;
                    else if (mid + d < bubble.Length && bubble[mid + d] == ' ') cut = mid + d;
                }
                if (cut > 0) lines = new List<string> { bubble.Substring(0, cut), bubble.Substring(cut + 1) };
            }
            float textWidth = 0;
            foreach (var line in lines) textWidth = Math.Max(textWidth, Measure(g, line, bubbleFont).Width);
            float w = Math.Min(maxWidth, textWidth + padX * 2), h = lineH * lines.Count + padY * 2;
            float cx = o.X + 150 * s, top = o.Y + 22 * s + (1 - a) * 6 * s;
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
            for (int i = 0; i < lines.Count; i++)
                CenteredText(g, lines[i], bubbleFont, color, cx, rect.Y + padY + lineH * (i + 0.5f));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.Idle -= OnIdle;
                wake.Dispose();
                if (frameTimer != IntPtr.Zero) CloseHandle(frameTimer);
                frameTimer = IntPtr.Zero;
                backCache?.Dispose();
                groundCache?.Dispose();
                frameCache?.Dispose();
                fadeShot?.Dispose();
                timeFont?.Dispose();
                captionFont?.Dispose();
                bubbleFont?.Dispose();
                plainFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
