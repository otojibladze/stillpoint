using System;
using System.Collections.Generic;
using System.Drawing;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        const float LadyLand = 1.02f * Pi, TurtleY = 215;
        readonly Critter ladybug = new Critter(), bee = new Critter(), squirrel = new Critter(), owl = new Critter(), moth = new Critter(), fox = new Critter(), snails = new Critter(), turtle = new Critter();
        float faceK, nightNext = 25, turtleNext = 30, snailsAt = -1, beeDip, digHole, digMound, moundBefore, acornDrop = -1, foxPrintsT = -9999, turtleHideT = -99;
        PointF digSpot = GardenArt.DigAt;
        int digIndex = -1;
        bool retrieving;

        bool Blooming => breaks >= Garden.FlowerBreaks && season != Season.Winter;
        bool Lantern => (state.Keepsakes & Keepsake.Lantern) != 0;
        bool Daylight => nightK < 0.35f;
        bool Green => season != Season.Winter && !snowGround && Daylight;
        bool Warm => Green && Clock().Month >= 4 && Clock().Month <= 10;
        bool VisitorBusy => ladybug.Active || bee.Active || squirrel.Active || rabbit.Active || worm.Active || cat.Active && cat.D == 1;
        bool AnyVisitor => VisitorBusy || owl.Active || moth.Active || fox.Active || snails.Active || turtle.Active || bat.Active || mouse.Active;
        bool LadybugOnStone => ladybug.State >= 2 && ladybug.State != 4;
        SpiritPose Shape => pose ?? new SpiritPose();

        static float Dist(PointF p, float x, float y) => (float)Math.Sqrt((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y));

        void StepVisitors(float t, float dt)
        {
            ScheduleNight(t);
            if (nightK > 0.5f)
            {
                SendAway(butterfly, t);
                SendAway(bird, t);
                if (ladybug.State >= 1 && ladybug.State <= 3) TakeOff(ladybug, t);
                if (bee.State == 1 || bee.State == 2) BeeLeave(bee, t);
                if (squirrel.State == 1) SquirrelRun(squirrel, t);
            }
            bool faced = !Sleeping && (butterfly.State == 2 || bird.State == 2 || LadybugOnStone || beeDip > 0.3f);
            faceK += ((faced ? 1 : 0) - faceK) * Math.Min(1, dt * 5);
            StepLadybug(t);
            StepBee(t);
            StepSquirrel(t, dt);
            StepOwl(t, dt);
            StepMoth(t, dt);
            StepFox(t, dt);
            StepSnails(t);
            StepTurtle(t, dt);
            float awayFox = away != null && away.Saw(Visitors.Fox) ? 1 - Smooth(480, 600, t - tracesT) : 0;
            traces.FoxPrints = Math.Max(awayFox, 1 - Smooth(240, 300, t - foxPrintsT));
        }

        void StartDayVisitor(float t)
        {
            if (!Daylight)
            {
                nightNext = Math.Min(nightNext, t + 2);
                return;
            }
            var picks = new List<(float Weight, Action<float> Go)> { (NestBusy ? 5f : 2f, x => StartFlyer(bird, x)) };
            if (Warm && windK < 0.75f) picks.Add((2f, x => StartFlyer(butterfly, x)));
            if (Warm) picks.Add((1.6f, StartLadybug));
            if (!squirrel.Active && (Green || state.Caches.Winter && state.Caches.Spot >= 0)) picks.Add((season == Season.Autumn || state.Caches.Winter ? 2.2f : 1.2f, StartSquirrel));
            if (Warm && Blooming) picks.Add((2.4f, StartBee));
            if (!rabbit.Active && !fox.Active) picks.Add((0.8f, StartRabbit));
            if (!cat.Active && rainK < 0.2f && !Running) picks.Add((0.35f, StartDayCat));
            if (!worm.Active && WetGround) picks.Add((1.2f, StartWorm));
            PickOne(picks, t);
        }

        void ScheduleNight(float t)
        {
            PairUp(t);
            bool dusk = nightK > 0.45f && nightK < 0.7f && Clock().Hour >= 15;
            if (nightK < 0.7f && !dusk || rainK > 0.4f || story.Active || owl.Active || moth.Active || fox.Active || bat.Active || mouse.Active || t < nightNext) return;
            nightNext = t + 50 + Rand * 50;
            var picks = new List<(float Weight, Action<float> Go)>();
            if (!dusk)
            {
                picks.Add((Lantern || !NestShown ? 1.1f : 0, StartOwl));
                picks.Add((Lantern || cloudK < 0.5f ? 1f : 0, StartMoth));
                picks.Add((0.9f, StartMouse));
            }
            if (!rabbit.Active) picks.Add((dusk ? 0.6f : 1f, StartFox));
            if (BatSeason && rainK < 0.2f) picks.Add((dusk ? 1.6f : 0.8f, StartBat));
            if (WetGround && !worm.Active) picks.Add((0.7f, StartWorm));
            PickOne(picks, t);
        }

        void SendAwayVisitors(float t)
        {
            if (ladybug.State >= 1 && ladybug.State <= 3) TakeOff(ladybug, t);
            if (bee.State == 1 || bee.State == 2) BeeLeave(bee, t);
            if (squirrel.State == 1) SquirrelRun(squirrel, t);
            if (owl.State == 2 && !Lantern) OwlLeave(owl, t);
            if (rabbit.State == 1 || rabbit.State == 2) LeaveRabbit(t);
            if (cat.D == 1 && (cat.State <= 3 || cat.State == 6) && cat.Active) LeaveCat(t);
        }

        void PokeVisitors(float t)
        {
            if (rabbit.Active && RabbitTrust < Trust.Trusting) RabbitAlarm(t, 0);
            if (mouse.Active) MouseFlee(t);
            WormRetract(t);
            if (squirrel.State == 1) { SquirrelRun(squirrel, t); Later("Oops. I scared it.", t + 0.6f, 2.2f); }
            if (turtle.Active && turtle.P.Hide < 0.5f) turtleHideT = t;
            if (owl.State == 2) OwlLeave(owl, t);
            if (fox.State == 2 && FoxTrust != Trust.Family) fox.B = Math.Min(fox.B, t - fox.T0 + 1.2f);
        }

        void HoverVisitors(PointF q, float t)
        {
            var c = ladybug;
            if ((c.State == 2 || c.State == 3) && Dist(q, c.X, c.Y) < 13)
            {
                c.State = 5;
                c.T0 = t;
                Chat(Pick(new[] { "It's playing dead. Very convincing.", "Oh no! Oh wait. It's pretending." }), 2.8f);
            }
        }

        bool StartVisitor(string visitor, float t)
        {
            if (visitor == Visitors.Ladybug) StartLadybug(t);
            else if (visitor == Visitors.Bee) StartBee(t);
            else if (visitor == Visitors.Squirrel) StartSquirrel(t);
            else if (visitor == Visitors.Owl) StartOwl(t);
            else if (visitor == Visitors.Moth) StartMoth(t);
            else if (visitor == Visitors.Fox) StartFox(t);
            else if (visitor == Visitors.Snails) StartSnails(t);
            else if (visitor == Visitors.Turtle) StartTurtle(t);
            else if (visitor == Visitors.Rabbit) StartRabbit(t);
            else if (visitor == Visitors.Bat) StartBat(t);
            else if (visitor == Visitors.Worm) StartWorm(t);
            else if (visitor == Visitors.Mouse) StartMouse(t);
            else return false;
            return true;
        }

        bool NearFocus(out PointF at)
        {
            at = PointF.Empty;
            if (bee.Active) at = new PointF(bee.X, bee.Y);
            else if (ladybug.Active) at = new PointF(ladybug.X, ladybug.Y);
            else if (squirrel.Active) at = new PointF(squirrel.X - 6, squirrel.Y - 8);
            else if (owl.Active && !Sleeping) at = new PointF(owl.X, owl.Y - 16);
            else if (worm.State == 1 || worm.State == 2) at = new PointF(worm.X, worm.Y - 4);
            else return false;
            return true;
        }

        bool FarFocus(out PointF at)
        {
            at = PointF.Empty;
            if (cat.Active && cat.D == 1) at = new PointF(cat.X - 10, GardenArt.CatAt.Y - 18 - cat.A);
            else if (fox.Active) at = new PointF(fox.X + 6, fox.Y - 14);
            else if (rabbit.Active) at = new PointF(rabbit.X, rabbit.Y - 10);
            else if (mouse.Active && !MouseBehind) at = new PointF(mouse.X, mouse.Y - 4);
            else if (bat.Active) at = new PointF(bat.X, bat.Y);
            else if (snails.Active) at = new PointF((snails.A + snails.B) / 2, TurtleY - 4);
            else if (turtle.Active) at = new PointF(turtle.X, turtle.Y - 4);
            else if (moth.Active) at = new PointF(moth.X, moth.Y);
            else return false;
            return true;
        }

        void ApplyVisitorFace(SpiritPose p)
        {
            p.Wind += beeDip * 0.6f;
            if (VisitorBusy || (owl.State == 2 && !Sleeping)) p.Brow = Brow.Curious;
            if (LadybugOnStone && !Sleeping) { p.LookX = -1; p.LookY = -1; }
        }

        PointF Surface(float a) => GardenArt.StoneSurface(Shape, a);

        float SurfaceAngle(float a)
        {
            var p0 = Surface(a - 0.03f);
            var p1 = Surface(a + 0.03f);
            return (float)Math.Atan2(p1.Y - p0.Y, p1.X - p0.X);
        }

        void StartLadybug(float t)
        {
            var c = ladybug;
            if (c.Active) return;
            c.State = 1;
            c.T0 = t;
            c.A = LadyLand;
            c.From = new PointF(-14, 64 + Rand * 30);
            c.P.Open = 1;
            c.P.Flip = 0;
            c.P.Walk = 0;
        }

        void StepLadybug(float t)
        {
            var c = ladybug;
            if (!c.Active) return;
            var p = c.P;
            float since = t - c.T0;
            if (c.State == 1)
            {
                float u = Clamp(since / 2.8f, 0, 1), e = Smooth(0, 1, u);
                var land = Surface(LadyLand);
                c.X = c.From.X + (land.X - c.From.X) * e + Sin(u * 10) * 5 * (1 - u);
                c.Y = c.From.Y + (land.Y - c.From.Y) * e - Sin(u * Pi) * 16;
                c.K = Sin(t * 5) * 0.12f * (1 - u) + SurfaceAngle(LadyLand) * Smooth(0.7f, 1, u);
                p.Open = 1;
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Ladybug);
                    Chat(Pick(new[] { "A ladybug! It tickles.", "Someone's climbing me.", "Lucky day. A ladybug." }), 2.8f);
                }
                return;
            }
            if (c.State == 4)
            {
                p.Walk = 0;
                p.Flip = 0;
                p.Open = Smooth(0, 0.35f, since);
                float u = Clamp((since - 0.4f) / 2.6f, 0, 1);
                c.X = c.From.X + (c.To.X - c.From.X) * u * u + Sin(u * 9) * 4 * u;
                c.Y = c.From.Y + (c.To.Y - c.From.Y) * u * u - Sin(u * Pi) * 8;
                c.K = c.B + (-0.45f - c.B) * Smooth(0, 0.35f, u) + Sin(t * 6) * 0.1f * u;
                if (u >= 1) c.State = 0;
                return;
            }
            if (c.State == 2)
            {
                p.Open = 1 - Smooth(0, 0.35f, since);
                float q = Clamp((since - 0.5f) / 4.8f, 0, 1);
                c.A = LadyLand + (LadyTop - LadyLand) * (q + Sin(q * Pi * 6) / (Pi * 6) * 0.8f);
                p.Walk = since > 0.5f && q < 1 ? (1 + 0.8f * Cos(q * Pi * 6)) / 1.8f : 0;
                if (since > 5.5f) { c.State = 3; c.T0 = t; }
            }
            else if (c.State == 3)
            {
                p.Walk = 0;
                if (since > 3.4f) TakeOff(c, t);
            }
            else if (c.State == 5)
            {
                p.Walk = 0;
                p.Open = 0;
                p.Flip = Smooth(0, 0.22f, since) * (1 - Smooth(2.6f, 2.85f, since));
                if (since > 2.95f) TakeOff(c, t);
            }
            if (c.State == 4) return;
            var at = Surface(c.A);
            c.X = at.X;
            c.Y = at.Y;
            c.K = SurfaceAngle(c.A);
        }

        void TakeOff(Critter c, float t)
        {
            c.State = 4;
            c.T0 = t;
            c.From = new PointF(c.X, c.Y);
            c.B = c.K;
            c.To = new PointF(SX + 200, -20 + Rand * 30);
        }

        void StartBee(float t)
        {
            var c = bee;
            if (c.Active) return;
            c.State = 1;
            c.T0 = t;
            c.Side = Rand < 0.5f ? -1 : 1;
            c.From = new PointF(SX + c.Side * 175, 60 + Rand * 40);
            c.X = c.From.X;
            c.Side = -c.Side;
        }

        PointF BeeLoop(float tau) => new PointF(points.Tip.X + 22 * Sin(tau * 1.7f), points.Tip.Y - 7 + 7 * Sin(tau * 3.4f));

        void StepBee(float t)
        {
            var c = bee;
            beeDip = 0;
            if (!c.Active) return;
            float since = t - c.T0, before = c.X;
            if (c.State == 1)
            {
                float u = Clamp(since / 1.8f, 0, 1), e = Smooth(0, 1, u);
                var to = BeeLoop(0);
                c.X = c.From.X + (to.X - c.From.X) * e;
                c.Y = c.From.Y + (to.Y - c.From.Y) * e + Sin(u * 12) * 5 * (1 - u);
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Bee);
                    Chat(Pick(new[] { "A bee! Please don't sting.", "My flower has a fan.", "Bzz. Busy little thing." }), 2.6f);
                }
            }
            else if (c.State == 2)
            {
                var loop = BeeLoop(since);
                float dip = Math.Max(Window(3.3f, 4.9f, since, 0.4f), Window(7.6f, 9.2f, since, 0.4f));
                var flower = new PointF(points.Tip.X - c.Side * 4, points.Tip.Y - 5);
                c.X = loop.X + (flower.X - loop.X) * dip;
                c.Y = loop.Y + (flower.Y - loop.Y) * dip + Sin(t * 9) * 0.8f * (1 - dip);
                beeDip = dip;
                c.P.Tilt = 0.35f * dip;
                if (dip > 0.85f && t > c.C)
                {
                    c.C = t + 0.14f;
                    Emit(new Particle { Kind = Bit.Pollen, X = points.Tip.X + (Rand - 0.5f) * 5, Y = points.Tip.Y - 2, Vx = (Rand - 0.5f) * 14, Vy = -6 - Rand * 8, Life = 1.4f, Size = 0.7f + Rand * 0.5f });
                }
                if (since > 10.4f) BeeLeave(c, t);
            }
            else
            {
                float u = Clamp(since / 2.2f, 0, 1);
                c.X = c.From.X + (c.To.X - c.From.X) * u * u + Sin(u * 14) * 6 * u;
                c.Y = c.From.Y + (c.To.Y - c.From.Y) * u * u;
                c.P.Tilt *= 0.9f;
                if (u >= 1) c.State = 0;
            }
            if (beeDip < 0.01f && Math.Abs(c.X - before) > 0.05f) c.Side = c.X > before ? 1 : -1;
        }

        void BeeLeave(Critter c, float t)
        {
            c.State = 3;
            c.T0 = t;
            c.From = new PointF(c.X, c.Y);
            c.To = new PointF(SX + (c.X > SX ? 1 : -1) * 200, 10 + Rand * 20);
        }

        void StartSquirrel(float t)
        {
            var c = squirrel;
            if (c.Active) return;
            var caches = state.Caches;
            digIndex = (caches.Autumn || caches.Winter) && caches.Spot >= 0 ? Math.Min(caches.Spot, GardenArt.CacheSpots.Length - 1) : -1;
            retrieving = caches.Winter && digIndex >= 0;
            digSpot = digIndex >= 0 ? GardenArt.CacheSpots[digIndex] : GardenArt.DigAt;
            c.State = 1;
            c.T0 = t;
            c.Side = -1;
            c.X = 320;
            c.Y = digSpot.Y;
            c.K = 0;
            c.P.Acorn = !caches.Winter;
            moundBefore = retrieving ? 1 : digIndex >= 0 ? 0 : digMound;
            digMound = moundBefore;
            Visit(Visitors.Squirrel);
            if (!Known(Visitors.Squirrel))
                Later(retrieving ? "A squirrel. It remembers where it hid its acorn." : caches.Autumn ? "A squirrel. Hiding acorns for winter." : Pick(new[] { "A squirrel. Hiding snacks for later.", "Hey! That's my sand." }), t + 1.9f, 2.8f);
        }

        void StepSquirrel(float t, float dt)
        {
            var c = squirrel;
            var p = c.P;
            if (!c.Active)
            {
                digHole = Math.Max(0, digHole - dt * 2);
                float target = state.DugToday && !state.Caches.Autumn && !state.Caches.Winter ? 1 : 0;
                if (digIndex >= 0) target = 0;
                digMound += (target - digMound) * Math.Min(1, dt * 3);
                if (digMound < 0.01f && digHole < 0.01f && digIndex >= 0) { digIndex = -1; digSpot = GardenArt.DigAt; }
                return;
            }
            float s = t - c.T0, home = digSpot.X + 9;
            bool snow = snowGround;
            if (c.State == 1)
            {
                if (s < 1.8f)
                {
                    float u = s / 1.8f;
                    c.X = 320 + (home - 320) * (1 - (1 - u) * (1 - u));
                    c.Y = digSpot.Y - Math.Abs(Sin(u * Pi * 5)) * 5 * (1 - u * 0.6f);
                    p.Gait = u * Pi * 5;
                    p.Run = 1 - Smooth(1.55f, 1.8f, s);
                    c.Side = -1;
                    return;
                }
                if (c.K == 0 && s > 2.3f) { c.K = 1; MaybeFoxChase(t); }
                if (fox.Active && fox.C == 1 && fox.X > 6) { c.State = 4; c.T0 = t; p.Dig = 0; p.Paw = 0; return; }
                c.X = home;
                c.Y = digSpot.Y;
                p.Run = 0;
                c.Side = s > 2.1f && s < 2.55f ? 1 : -1;
                p.Dig = Math.Max(Window(2.9f, 5.4f, s, 0.25f), Window(6, 7, s, 0.15f) * 0.7f);
                p.Paw = s > 2.95f && s < 5.3f ? 1 : s > 6.05f && s < 6.95f ? 2 : 0;
                if (retrieving) p.Acorn = s > 5.4f;
                else p.Acorn = state.Caches.Winter ? false : s < 5.5f;
                p.Flick = Math.Max(Window(7, 8, s, 0.1f), Window(2.0f, 2.9f, s, 0.1f) * 0.5f);
                digHole = Smooth(3, 5, s) * (1 - Smooth(6, 6.8f, s));
                digMound = retrieving ? moundBefore * (1 - Smooth(3, 4.6f, s)) : Math.Max(Smooth(6, 7, s), moundBefore * (1 - digHole));
                acornDrop = !retrieving && !state.Caches.Winter && s >= 5.5f && s < 6.1f ? (s - 5.5f) / 0.6f : -1;
                if (p.Paw == 1 && t > c.C)
                {
                    c.C = t + 0.08f;
                    if (snow) Emit(new Particle { Kind = Bit.Snow, X = digSpot.X + 2, Y = digSpot.Y - 2, Vx = 30 + Rand * 40, Vy = -45 - Rand * 35, Size = 0.9f + Rand * 0.6f, Target = digSpot.Y - 1 + Rand * 5, Life = 1.4f });
                    else Emit(new Particle { Kind = Bit.Dirt, X = digSpot.X + 2, Y = digSpot.Y - 1, Vx = 30 + Rand * 40, Vy = -45 - Rand * 35, Life = 1.3f, Size = 0.6f + Rand * 0.6f, Target = digSpot.Y - 1 + Rand * 5, Night = nightK });
                }
                if (s > 8)
                {
                    if (retrieving) { c.State = 5; c.T0 = t; p.Dig = p.Flick = 0; p.Paw = 2; }
                    else if (!state.Caches.Winter && TrustOf(Visitors.Squirrel) >= Trust.Friendly && !giftLeft) { c.State = 3; c.T0 = t; c.From = new PointF(c.X, c.Y); p.Acorn = true; }
                    else SquirrelRun(c, t);
                    if (digIndex >= 0 && !retrieving) digMound = 0;
                }
            }
            else if (c.State == 3)
            {
                float u = Clamp(s / 1.1f, 0, 1);
                c.Side = -1;
                p.Dig = p.Flick = 0;
                p.Paw = 0;
                p.Run = 1 - Smooth(0.75f, 1, u);
                p.Gait = u * Pi * 3;
                c.X = c.From.X + (GardenArt.GiftAt.X + 7 - c.From.X) * u;
                c.Y = GardenArt.DigAt.Y - Math.Abs(Sin(u * Pi * 3)) * 4 * (1 - u);
                p.Acorn = s < 1.4f;
                if (s >= 1.4f && !giftLeft)
                {
                    giftLeft = true;
                    var f = FriendNow(Visitors.Squirrel);
                    Later(f != null && f.Named ? $"{f.Name} left me an acorn!" : "It left me an acorn!", t + 0.3f, 2.8f);
                }
                if (s > 2.3f) SquirrelRun(c, t);
            }
            else if (c.State == 4)
            {
                p.Dig = 0;
                p.Paw = 0;
                p.Run = 0;
                p.Flick = 1;
                c.Side = -1;
                digHole = Math.Max(0, digHole - dt * 2);
                if (s > 0.5f) SquirrelRun(c, t);
            }
            else if (c.State == 5)
            {
                p.Paw = 2;
                p.Acorn = s < 3.2f;
                p.Flick = Window(0.5f, 3, s, 0.3f) * 0.4f;
                digHole = Math.Max(0, digHole - dt * 1.5f);
                if (s > 1 && c.K < 2) c.K = 2;
                if (s > 1 && c.K == 2) { c.K = 3; Later(Tone == Tone.Dry ? "It found its acorn. Show-off." : "It found its acorn from autumn!", t + 0.2f, 2.8f); }
                if (s > 3.6f) SquirrelRun(c, t);
            }
            else
            {
                float u = Clamp(s / 1.4f, 0, 1);
                c.Side = 1;
                p.Run = 1;
                p.Dig = 0;
                p.Paw = 0;
                p.Flick = 0;
                acornDrop = -1;
                c.X = c.From.X + (330 - c.From.X) * u * u;
                c.Y = digSpot.Y - Math.Abs(Sin(u * Pi * 4)) * 5;
                p.Gait = u * Pi * 4;
                if (u >= 1) c.State = 0;
            }
        }

        void SquirrelRun(Critter c, float t)
        {
            c.State = 2;
            c.T0 = t;
            c.From = new PointF(c.X, c.Y);
        }

        PointF OwlPerch() => GardenArt.OwlPerch(Shape, state.Keepsakes);

        void StartOwl(float t)
        {
            var c = owl;
            if (c.Active) return;
            c.State = 1;
            c.T0 = t;
            c.Side = Rand < 0.5f ? -1 : 1;
            c.From = new PointF(SX + c.Side * 190, 20 + Rand * 20);
            c.P.Turn = 0;
            c.P.Spread = 1;
            c.K = 0;
            c.A = 0;
            var trust = TrustOf(Visitors.Owl);
            c.D = (int)trust;
            c.B = trust == Trust.Family ? float.MaxValue : 10 + 5 * (int)trust;
        }

        void StepOwl(float t, float dt)
        {
            var c = owl;
            var p = c.P;
            if (!c.Active) return;
            float s = t - c.T0;
            if (c.State == 1)
            {
                float u = Clamp(s / 3.4f, 0, 1), e = Smooth(0, 1, u);
                var perch = OwlPerch();
                c.X = c.From.X + (perch.X - c.From.X) * e;
                c.Y = c.From.Y + (perch.Y - c.From.Y) * e - Sin(u * Pi) * 14;
                p.Spread = 1 - Smooth(0.9f, 1, u);
                p.Flap = u < 0.55f ? Sin(t * 8) : u < 0.82f ? 0.1f : -Sin((u - 0.82f) / 0.18f * Pi);
                p.Tilt = (perch.X > c.From.X ? 1 : -1) * 0.18f * (1 - Smooth(0.6f, 1, u));
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Owl);
                    if (!Sleeping && !Known(Visitors.Owl)) Chat(Pick(new[] { "Hoo's there? Oh. An owl.", "An owl. We're both night people now." }), 3f);
                }
            }
            else if (c.State == 2)
            {
                var perch = OwlPerch();
                c.X = perch.X;
                c.Y = perch.Y;
                p.Spread = 0;
                p.Tilt = 0;
                float look = c.X > SX ? -0.5f : 0.5f;
                if (pointerIn && t - pointerT < 3 && Dist(pointer, c.X, c.Y - 18) < 40) look = Clamp((pointer.X - c.X) / 22, -1.2f, 1.2f);
                if (moth.State == 2)
                {
                    look = Clamp((moth.X - c.X) / 18, -1.2f, 1.2f);
                    if (c.K <= 0) c.K = t;
                    if (c.A == 0 && t - c.K > 3)
                    {
                        c.A = 1;
                        c.C = 0;
                        owlHootT = t;
                        MothFlee(t, new PointF(c.X, c.Y - 18));
                        if (!Sleeping) Later(Tone == Tone.Dry ? "Owl versus moth. The moth wins." : Tone == Tone.Worry ? "Easy, owl. Let it go." : "The owl hooted at the moth. Off it goes!", t + 0.5f, 2.8f);
                    }
                }
                var trust = (Trust)(int)c.D;
                float kiss = trust >= Trust.Friendly ? Window(6.2f, 8f, s, 0.5f) : 0;
                if (kiss > 0) look += ((c.X > SX ? -1.1f : 1.1f) - look) * kiss;
                float target = look + (trust >= Trust.Curious ? Window(8.5f, 10.9f, s, 0.9f) * Pi : 0);
                p.Turn += (target - p.Turn) * (1 - (float)Math.Exp(-dt * 6));
                float blink = (s + 1.1f) % 3.6f;
                p.Blink = Math.Max(blink < 0.6f ? Sin(blink / 0.6f * Pi) : 0, Window(6.6f, 7.8f, s, 0.45f) * kiss);
                p.Puff = trust == Trust.Wary ? 0 : Math.Max(Window(4.2f, 5, s, 0.2f), Math.Max(Window(12, 12.8f, s, 0.2f), trust >= Trust.Trusting ? Window(19, 19.8f, s, 0.2f) : 0));
                float hoot = Window(0, 1.3f, t - owlHootT, 0.2f);
                p.Puff = Math.Max(p.Puff, hoot);
                p.Tilt = 0.12f * hoot * Math.Sign(moth.X - c.X);
                if (trust == Trust.Wary && pointerIn && t - pointerT < 1 && Dist(pointer, c.X, c.Y - 14) < 26) { OwlLeave(c, t); return; }
                if (p.Puff > 0.9f && t > c.C)
                {
                    c.C = t + 0.35f;
                    Emit(new Particle { Kind = Bit.Hoot, X = c.X + Sin(p.Turn) * 3, Y = c.Y - 18.5f, Life = 1.1f, Size = 1 });
                }
                if (s > c.B || ((Trust)(int)c.D == Trust.Family && nightK < 0.45f)) OwlLeave(c, t);
            }
            else
            {
                float u = Clamp(s / 2.8f, 0, 1);
                p.Spread = Smooth(0, 0.15f, u);
                p.Flap = Sin(t * 9);
                p.Blink = 0;
                p.Puff = 0;
                p.Turn += (0 - p.Turn) * (1 - (float)Math.Exp(-dt * 8));
                p.Tilt = (c.To.X > c.From.X ? 1 : -1) * 0.18f * Smooth(0, 0.3f, u);
                c.X = c.From.X + (c.To.X - c.From.X) * u * u;
                c.Y = c.From.Y + (c.To.Y - c.From.Y) * u - Sin(u * Pi) * 10;
                if (u >= 1) c.State = 0;
            }
        }

        void OwlLeave(Critter c, float t)
        {
            c.State = 3;
            c.T0 = t;
            c.From = new PointF(c.X, c.Y);
            c.To = new PointF(SX + (c.X > SX ? 1 : -1) * 200, -10);
        }

        PointF MothLight(out bool lamp)
        {
            lamp = Lantern;
            return lamp ? new PointF(GardenArt.LanternAt.X, GardenArt.LanternAt.Y - 24) : GardenArt.MoonAt;
        }

        void StartMoth(float t)
        {
            var c = moth;
            if (c.Active) return;
            var light = MothLight(out _);
            c.State = 1;
            c.T0 = t;
            c.Side = light.X > SX ? -1 : 1;
            c.From = new PointF(light.X + c.Side * 90, light.Y - 30);
            c.A = 0;
            c.B = -99;
            c.D = 0;
        }

        void StepMoth(float t, float dt)
        {
            var c = moth;
            if (!c.Active) return;
            float s = t - c.T0;
            var light = MothLight(out bool lamp);
            float dart = (float)Math.Pow(Math.Max(0, Sin(s * 2.1f)), 8);
            float r = ((lamp ? 11 : 10) + 3 * Sin(t * 2.3f)) * (1 - 0.7f * dart);
            c.A += dt * (2.6f + 1.4f * Sin(t * 1.3f));
            float dodge = Window(0, 0.9f, t - c.B, 0.15f);
            var orbit = new PointF(light.X + Cos(c.A) * r + Sin(t * 21) * 0.9f + Sin((t - c.B) * 20) * 3 * dodge, light.Y + Sin(c.A) * r * 0.75f + Cos(t * 17) * 0.9f + dodge * 12);
            if (c.State == 1)
            {
                float u = Clamp(s / 2.4f, 0, 1), e = Smooth(0, 1, u);
                c.X = c.From.X + (orbit.X - c.From.X) * e + Sin(t * 13) * 2 * (1 - u);
                c.Y = c.From.Y + (orbit.Y - c.From.Y) * e + Cos(t * 11) * 2 * (1 - u);
                if (u >= 1) { c.State = 2; c.T0 = t; Visit(Visitors.Moth); }
            }
            else if (c.State == 2)
            {
                c.X = orbit.X;
                c.Y = orbit.Y;
                if (s > 18)
                {
                    c.State = 3;
                    c.T0 = t;
                    c.From = orbit;
                    c.To = new PointF(light.X - c.Side * 120, light.Y - 70);
                }
            }
            else
            {
                float u = Clamp(s / (c.D == 1 ? 1.5f : 2.6f), 0, 1);
                c.X = c.From.X + (c.To.X - c.From.X) * u * u + Sin(t * 13) * 2;
                c.Y = c.From.Y + (c.To.Y - c.From.Y) * u * u + Cos(t * 11) * 2;
                if (u >= 1) c.State = 0;
            }
            float d = Dist(new PointF(c.X, c.Y), light.X, light.Y);
            c.P.Lit = lamp ? Clamp(1 - d / 26, 0, 1) * Math.Max(0, nightK) : 0;
            c.P.Shadow = lamp ? 0 : 1 - Smooth(11, 14, d);
        }

        static readonly float[] FoxHomes = { 30, 52, 74, 92, 100 };
        static readonly float[] FoxStays = { 12, 24, 30, 45, float.MaxValue };

        Trust FoxTrust => (Trust)(int)fox.D;

        void StartFox(float t)
        {
            var c = fox;
            if (c.Active) return;
            var trust = TrustOf(Visitors.Fox);
            c.State = 1;
            c.T0 = t;
            c.Side = 1;
            c.X = -24;
            c.Y = GardenArt.FoxAt.Y;
            c.A = FoxHomes[(int)trust];
            c.B = FoxStays[(int)trust];
            c.D = (int)trust;
            c.P.Sit = 0;
            c.P.Curl = 0;
            c.P.Hide = 0;
            c.P.Flick = 0;
            c.P.Tilt = 0;
            c.C = 0;
            c.K = 0;
        }

        void StepFox(float t, float dt)
        {
            var c = fox;
            var p = c.P;
            if (!c.Active) return;
            float s = t - c.T0, home = c.A;
            var trust = FoxTrust;
            if (c.State == 1)
            {
                float u = Clamp(s / (c.C == 1 ? 3.2f : 2.6f + home / 60), 0, 1);
                c.X = -24 + (home + 24) * (1 - (1 - u) * (1 - u));
                p.Gait = s * 9;
                p.Run = 1 - Smooth(0.85f, 1, u);
                c.Y = GardenArt.FoxAt.Y - Math.Abs(Cos(p.Gait)) * 0.7f * p.Run;
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    Visit(Visitors.Fox);
                    if (!Sleeping && !Known(Visitors.Fox) && c.C == 0) Chat(Pick(new[] { "A fox. Shh, don't scare it.", "Look who's out tonight." }), 3f);
                }
            }
            else if (c.State == 2 && c.C == 1)
            {
                p.Run = 0;
                c.X = home;
                c.Y = GardenArt.FoxAt.Y;
                p.Sit = p.Curl = p.Hide = 0;
                p.Tilt = s < 3 ? 0.42f * Window(0.2f, 3, s, 0.3f) : -0.12f * Window(3, 5.5f, s, 0.3f);
                p.Ear = Window(3.1f, 3.5f, s, 0.1f);
                if (s > 3.4f && c.K == 0)
                {
                    c.K = 1;
                    Later(Tone == Tone.Dry ? "The squirrel got away. Fox, zero." : "Everyone's okay. Just a scare.", t + 0.3f, 2.8f);
                }
                if (s > c.B) { c.State = 3; c.T0 = t; c.Side = -1; p.Tilt = 0; p.Ear = 0; }
            }
            else if (c.State == 2)
            {
                if (trust == Trust.Family && nightK < 0.45f && c.B > s + 3) c.B = s + 3;
                float leave = c.B;
                bool lies = trust >= Trust.Trusting, sleeps = trust == Trust.Family;
                p.Run = 0;
                c.X = home;
                c.Y = GardenArt.FoxAt.Y;
                p.Sit = trust == Trust.Wary ? 0 : Smooth(0, 0.7f, s) * (1 - Smooth(leave - 0.7f, leave, s));
                p.Curl = trust == Trust.Wary ? 0 : Smooth(0.6f, 1.6f, s) * (1 - Smooth(leave - 1.2f, leave - 0.4f, s));
                p.Hide = lies ? Smooth(5, 6.5f, s) * (1 - Smooth(leave - 2.4f, leave - 1.2f, s)) : 0;
                float cycle = lies ? 2.6f : 3.7f, closed = lies ? 0.7f : 0.22f, blink = (s + 0.4f) % cycle;
                p.Blink = sleeps && p.Hide > 0.85f ? 1 : blink < closed ? Sin(blink / closed * Pi) : 0;
                p.Ear = trust == Trust.Wary ? Window(0, 0.5f, s % 2.4f, 0.1f) : Math.Max(Window(6, 6.35f, s, 0.08f), Window(14.2f, 14.5f, s, 0.08f));
                p.Ear = Math.Max(p.Ear, Window(0, 0.8f, t - pokeT, 0.1f));
                p.Yawn = trust >= Trust.Friendly && !(sleeps && p.Hide > 0.5f) ? Window(10.5f, 12.3f, s, 0.5f) : 0;
                p.Tilt = trust == Trust.Wary || lies && p.Hide > 0.3f ? 0 : 0.14f * Window(16, 19.5f, s, 0.6f);
                p.Flick += ((petting && trust >= Trust.Friendly ? 1 : 0) - p.Flick) * Math.Min(1, dt * 4);
                if (s > leave) { c.State = 3; c.T0 = t; c.Side = -1; }
            }
            else
            {
                float u = Clamp(s / 3, 0, 1);
                p.Sit = p.Curl = p.Hide = p.Flick = 0;
                c.X = home + (-30 - home) * u * u;
                p.Run = Smooth(0, 0.15f, u);
                p.Gait = s * 9;
                c.Y = GardenArt.FoxAt.Y - Math.Abs(Cos(p.Gait)) * 0.7f * p.Run;
                if (u >= 1) { c.State = 0; foxPrintsT = t; }
            }
        }
        void StartSnails(float t)
        {
            var c = snails;
            if (c.Active || snail.Active) return;
            c.State = 1;
            c.T0 = t;
            c.A = 30;
            c.B = 270;
            c.D = 0;
            Visit(Visitors.Snails);
            Chat("Two snails. A rainy day romance.", 3.2f, thought: true);
        }

        void StepSnails(float t)
        {
            var c = snails;
            if (!c.Active) return;
            float s = t - c.T0;
            if (c.State == 1)
            {
                float u = Clamp(s / 38, 0, 1);
                c.A = 30 + 109 * u;
                c.B = 270 - 109 * u;
                if (u >= 1) { c.State = 2; c.T0 = t; c.C = t + 0.3f; }
            }
            else if (c.State == 2)
            {
                if (t > c.C && c.D < 2)
                {
                    c.C = t + 1.3f;
                    c.D++;
                    Emit(new Particle { Kind = Bit.Heart, X = 150, Y = TurtleY - 17, Vx = (Rand - 0.5f) * 6, Vy = -18, Vr = (Rand - 0.5f) * 0.4f, Life = 1.8f, Size = 4.5f });
                }
                if (s > 6) { c.State = 3; c.T0 = t; }
            }
            else
            {
                float u = Clamp(s / 45, 0, 1);
                c.A = 139 + 161 * u;
                c.B = 161 + 161 * u;
                if (u >= 1) c.State = 0;
            }
        }

        void StartTurtle(float t)
        {
            var c = turtle;
            if (c.Active) return;
            c.State = 1;
            c.T0 = t;
            c.A = 0;
            c.X = -16;
            c.Y = TurtleY;
            c.P.Hide = 0;
            Visit(Visitors.Turtle);
            if (!Known(Visitors.Turtle)) Chat("A turtle. Slow and steady... and still faster than us.", 3.6f, thought: true);
        }

        void StepTurtle(float t, float dt)
        {
            var c = turtle;
            if (!c.Active) return;
            var p = c.P;
            bool hidden = t - turtleHideT < 3.6f;
            p.Hide = hidden ? Math.Min(1, p.Hide + dt / 0.28f) : Math.Max(0, p.Hide - dt / 1.1f);
            float s = t - c.T0;
            bool resting = s % 11 > 9.3f;
            float speed = p.Hide > 0.01f || resting ? 0 : 2.1f;
            p.Walk += ((speed > 0 ? 1 : 0) - p.Walk) * Math.Min(1, dt * 6);
            p.Tilt = resting && p.Hide < 0.01f ? -0.9f * Window(9.3f, 11, s % 11, 0.4f) : 0;
            c.A += speed * dt;
            p.Gait += speed * dt * 1.4f;
            c.X = -16 + c.A;
            if (c.X > 316) { c.State = 0; turtleNext = t + 240; }
        }

        void DrawGroundVisitors(Graphics g, float t, float night)
        {
            GardenArt.DrawCaches(g, state.Caches, squirrel.Active || digHole > 0.01f || digMound > 0.01f ? digIndex : -1, season, night, snowGround, wind);
            if (digHole > 0.01f || digMound > 0.01f) GardenArt.DrawDig(g, digSpot.X, digSpot.Y, digHole, digMound, season, night, snowGround && digIndex >= 0);
            if (acornDrop >= 0) CritterArt.DrawAcorn(g, digSpot.X, digSpot.Y - 3 * (1 - acornDrop), 0.9f * (1 - acornDrop * 0.6f), night);
            DrawWildGround(g, t, night);
            if (fox.Active) CritterArt.DrawFox(g, fox.X, fox.Y, t, fox.Side, fox.P, night);
            DrawCubs(g, t, night);
            if (snails.Active)
            {
                using (var trail = new Pen(Color.FromArgb(50, 255, 255, 255), 1.2f))
                {
                    if (snails.A - 8 > 30) g.DrawLine(trail, 30, TurtleY - 2.5f, snails.A - 8, TurtleY - 2.5f);
                    g.DrawLine(trail, Math.Min(snails.B, 161) + 8, TurtleY - 2.5f, 270, TurtleY - 2.5f);
                }
                SpiritArt.DrawSnail(g, snails.A, TurtleY - 2, t, 1);
                SpiritArt.DrawSnail(g, snails.B, TurtleY - 2, t + 0.5f, snails.State == 3 ? 1 : -1);
            }
            if (turtle.Active) CritterArt.DrawTurtle(g, turtle.X, turtle.Y, t, 1, turtle.P, night);
            if (squirrel.Active) CritterArt.DrawSquirrel(g, squirrel.X, squirrel.Y, t, squirrel.Side, squirrel.P, night);
        }

        void DrawAirVisitors(Graphics g, float t, float night)
        {
            if (ladybug.Active) CritterArt.DrawLadybug(g, ladybug.X, ladybug.Y, ladybug.K, t, ladybug.P);
            if (owl.Active) CritterArt.DrawOwl(g, owl.X, owl.Y, t, owl.P, night);
            if (bee.Active) CritterArt.DrawBee(g, bee.X, bee.Y, t, bee.Side, bee.P);
            if (moth.Active)
            {
                bool lamp = Lantern;
                if (lamp && moth.P.Lit > 0.05f)
                    SpiritArt.FillEllipse(g, Alpha(Ink, 0.2f * moth.P.Lit), moth.X + (moth.X - GardenArt.LanternAt.X) * 0.7f, GardenArt.LanternAt.Y + 3, 2.4f, 0.7f);
                CritterArt.DrawMoth(g, moth.X, moth.Y, t, moth.P, lamp ? 1 : 0.75f);
            }
            DrawWildAir(g, t, night);
        }
    }
}
