using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        const float RabbitY = 213, MouseY = 214;
        static readonly PointF[] WormSpots = { new PointF(62, 221), new PointF(238, 214), new PointF(166, 219) };
        static readonly PointF MouseHide = new PointF(150, 182), MousePeekRight = new PointF(191, 183), MousePeekLeft = new PointF(109, 183);
        readonly Critter rabbit = new Critter(), bat = new Critter(), worm = new Critter(), mouse = new Critter();
        readonly List<(PointF At, float T)> prints = new List<(PointF, float)>();
        float rabbitNext = 25, wormNext = 10, castT = -9999, hopFrom, hopTo, hopLen = 1, hopHeight, mouseGo = -1, mouseLen = 1, mouseNextMove, mouseAlarmT = -99;
        PointF castAt, batVel, mouseFrom, mouseTo;
        int batExit = 1;

        bool WetGround => season != Season.Winter && !snowGround && (wetReal > 0.4f || rainK > 0.25f || AwayWet(Now) > 0.3f);
        Trust RabbitTrust => (Trust)(int)rabbit.D;

        bool Twilight
        {
            get
            {
                float h = (float)Clock().TimeOfDay.TotalHours;
                return nightK > 0.12f && nightK < 0.8f && (h < 11 || h >= 15);
            }
        }

        void StepWild(float t, float dt)
        {
            if (!rabbit.Active && t > rabbitNext && Twilight && rainK < 0.3f && !fox.Active && !cat.Active && !story.Active)
            {
                rabbitNext = t + 70 + Rand * 70;
                if (Rand < 0.6f) StartRabbit(t);
            }
            StepRabbit(t, dt);
            StepBat(t, dt);
            StepWorm(t);
            StepMouse(t, dt);
            prints.RemoveAll(p => t - p.T > 300 || t < p.T);
        }

        static float Toward(float v, float target, float step) => v < target ? Math.Min(target, v + step) : Math.Max(target, v - step);

        void StartRabbit(float t)
        {
            if (!Fits(Visitors.Rabbit)) return;
            var c = rabbit;
            if (c.Active) return;
            bool left = cat.Active || Rand < 0.5f;
            c.State = 1;
            c.T0 = t;
            c.Side = left ? 1 : -1;
            c.X = left ? -14 : 314;
            c.Y = RabbitY;
            c.To = new PointF(left ? 44 + Rand * 16 : 244 + Rand * 14, RabbitY);
            c.D = (int)TrustOf(Visitors.Rabbit);
            c.B = 14 + 5 * c.D + Rand * 6;
            c.C = -1;
            c.K = 0;
            c.A = t + 0.2f;
            var p = c.P;
            p.Sit = p.Run = p.Hide = p.Curl = p.Ear = p.Turn = p.Flick = p.Yawn = p.Blink = p.Tilt = 0;
        }

        void Hop(Critter c, float t, float to, float len, float height)
        {
            c.C = t;
            hopFrom = c.X;
            hopTo = to;
            hopLen = len;
            hopHeight = height;
            if (Math.Abs(to - c.X) > 0.5f) c.Side = to > c.X ? 1 : -1;
        }

        bool Hopping(Critter c, float t)
        {
            if (c.C < 0) return false;
            float u = Clamp((t - c.C) / hopLen, 0, 1);
            c.X = hopFrom + (hopTo - hopFrom) * u;
            c.Y = RabbitY - Sin(u * Pi) * hopHeight;
            c.P.Run = Sin(u * Pi);
            if (u < 1) return true;
            c.C = -1;
            c.Y = RabbitY;
            c.P.Run = 0;
            if (snowGround && prints.Count < 40) prints.Add((new PointF(c.X - c.Side * 2, RabbitY + 1), t));
            return false;
        }

        int RabbitThreat(Critter c, float t)
        {
            if (fox.Active && fox.State != 3) return fox.X < c.X ? -1 : 1;
            if (cat.Active && (cat.State == 1 || cat.State == 3 || cat.State == 4)) return cat.X < c.X ? -1 : 1;
            if (RabbitTrust <= Trust.Curious && pointerIn && t - pointerT < 0.4f && Dist(pointer, c.X, c.Y - 6) < 36) return pointer.X < c.X ? -1 : 1;
            return 0;
        }

        void RabbitAlarm(float t, int threat)
        {
            var c = rabbit;
            if (c.State < 1 || c.State > 3) return;
            c.State = 4;
            c.T0 = t;
            c.K = 0;
            c.B = threat == 0 ? (c.X < SX ? -1 : 1) : -threat;
        }

        void StepRabbit(float t, float dt)
        {
            var c = rabbit;
            if (!c.Active) return;
            var p = c.P;
            float s = t - c.T0;
            p.Blink = (t + 0.9f) % 4.1f < 0.14f ? 1 : 0;
            bool air = Hopping(c, t);
            if (c.State <= 3)
            {
                int threat = RabbitThreat(c, t);
                if (threat != 0) { RabbitAlarm(t, threat); return; }
                if (nightK > 0.85f && c.State == 2) LeaveRabbit(t);
            }
            switch (c.State)
            {
                case 1:
                case 3:
                    p.Hide = Toward(p.Hide, 0, dt * 3);
                    p.Curl = Toward(p.Curl, 0, dt * 4);
                    p.Tilt = Toward(p.Tilt, 0, dt * 5);
                    p.Yawn = 0;
                    if (air) break;
                    if (c.State == 3 && (c.X < -12 || c.X > 312)) { c.State = 0; break; }
                    float left = c.To.X - c.X;
                    if (c.State == 1 && Math.Abs(left) < 2)
                    {
                        c.State = 2;
                        c.T0 = t;
                        Visit(Visitors.Rabbit);
                        if (!Sleeping && !Known(Visitors.Rabbit)) Chat(Tone == Tone.Dry ? "A rabbit. Act natural." : "A rabbit! Don't move.", 2.8f);
                        break;
                    }
                    if (t < c.A)
                    {
                        p.Sit = Toward(p.Sit, 1, dt * 5);
                        p.Turn = Sin(t * 3);
                        p.Flick = 1;
                        break;
                    }
                    p.Sit = Toward(p.Sit, 0, dt * 7);
                    p.Turn = 0;
                    if (p.Sit > 0.05f) break;
                    Hop(c, t, c.X + Math.Sign(left) * Math.Min(15, Math.Abs(left)), 0.42f, 6);
                    c.K++;
                    if (c.K % 3 == 0) c.A = t + 0.42f + 0.6f + Rand * 0.5f;
                    break;
                case 2:
                    StepGraze(c, t, dt, s);
                    if (s > c.B) LeaveRabbit(t);
                    break;
                case 4:
                    p.Sit = Toward(p.Sit, 1, dt * 8);
                    p.Hide = p.Curl = p.Tilt = p.Yawn = 0;
                    p.Ear = 0;
                    p.Turn = 0;
                    p.Flick = 1;
                    if (s > 0.35f && c.K == 0 && !air)
                    {
                        c.K = 1;
                        Emit(new Particle { Kind = Bit.Puff, X = c.X - c.Side * 4, Y = RabbitY - 1, Vx = -c.Side * 10, Vy = -5, Life = 0.6f, Size = 2.2f });
                        Emit(new Particle { Kind = Bit.Hoot, X = c.X - c.Side * 4, Y = RabbitY - 2, Life = 0.7f, Size = 1 });
                    }
                    if (s > 0.62f) { c.State = 5; c.T0 = t; }
                    break;
                case 5:
                    p.Sit = 0;
                    p.Ear = 0.7f;
                    p.Flick = 0;
                    if (air) break;
                    if (c.X < -20 || c.X > 320) { c.State = 0; break; }
                    Hop(c, t, c.X + c.B * 24, 0.3f, 7);
                    break;
            }
        }

        void LeaveRabbit(float t)
        {
            var c = rabbit;
            c.State = 3;
            c.T0 = t;
            c.K = 0;
            c.A = t;
            c.To = new PointF(c.X < SX ? -22 : 322, RabbitY);
        }

        void StepGraze(Critter c, float t, float dt, float s)
        {
            var p = c.P;
            int cycle = (int)(s / 5.5f);
            float k = s % 5.5f;
            bool groom = RabbitTrust >= Trust.Trusting && cycle == 1;
            bool flop = RabbitTrust == Trust.Family && cycle == 2;
            bool alert = k > 3.6f && !groom && !flop;
            p.Sit = Toward(p.Sit, alert ? 1 : groom ? 0.7f : 0, dt * 4);
            p.Tilt = Toward(p.Tilt, !alert && !groom && !flop ? 1 : 0, dt * 4);
            p.Yawn = !alert && !groom && !flop ? 1 : 0;
            p.Turn = alert ? Sin(t * 2.5f) : 0;
            p.Flick = alert ? 1 : 0.3f;
            p.Curl = Toward(p.Curl, groom && k > 0.6f && k < 4.6f ? 1 : 0, dt * 4);
            p.Hide = Toward(p.Hide, flop && k > 0.5f && k < 5 ? 1 : 0, dt * 2.5f);
            p.Ear = flop ? p.Hide : 0;
        }

        void StartBat(float t)
        {
            if (!Fits(Visitors.Bat)) return;
            var c = bat;
            if (c.Active) return;
            c.State = 1;
            c.T0 = t;
            batExit = Rand < 0.5f ? 1 : -1;
            c.Side = batExit;
            c.X = batExit > 0 ? -16 : 316;
            c.Y = 45 + Rand * 50;
            batVel = new PointF(batExit * 90, 0);
            c.C = 0;
            c.K = 0;
            c.A = 0;
            c.B = 12 + Rand * 7;
            c.D = 0;
        }

        void StepBat(float t, float dt)
        {
            var c = bat;
            if (!c.Active) return;
            float s = t - c.T0;
            c.K += dt * 23;
            bool leaving = s > c.B || nightK < 0.3f;
            if (t > c.C)
            {
                c.C = t + 0.3f + Rand * 0.45f;
                if (leaving) c.To = new PointF(batExit > 0 ? 345 : -45, 25 + Rand * 30);
                else if (moth.State == 2 && c.A < 2 && Rand < 0.7f) c.To = new PointF(moth.X + (Rand - 0.5f) * 6, moth.Y + (Rand - 0.5f) * 6);
                else if (Lantern && Rand < 0.3f) c.To = new PointF(GardenArt.LanternAt.X + (Rand - 0.5f) * 30, GardenArt.LanternAt.Y - 35 + (Rand - 0.5f) * 20);
                else c.To = new PointF(40 + Rand * 220, 35 + Rand * 95);
            }
            float dx = c.To.X - c.X, dy = c.To.Y - c.Y, d = (float)Math.Sqrt(dx * dx + dy * dy) + 0.01f;
            batVel = new PointF(batVel.X + dx / d * 520 * dt, batVel.Y + dy / d * 520 * dt);
            float speed = (float)Math.Sqrt(batVel.X * batVel.X + batVel.Y * batVel.Y);
            if (speed > 150) batVel = new PointF(batVel.X * 150 / speed, batVel.Y * 150 / speed);
            c.X += batVel.X * dt;
            c.Y += batVel.Y * dt;
            if (Math.Abs(batVel.X) > 8) c.Side = batVel.X > 0 ? 1 : -1;
            if (s > 1.5f && c.D == 0)
            {
                c.D = 1;
                bool first = !state.SeenToday.Contains(Visitors.Bat);
                Visit(Visitors.Bat);
                if (first && !Sleeping) Chat(Tone == Tone.Dry ? "A bat. Night shift starts." : "A bat! It's out hunting bugs.", 2.8f);
            }
            if (moth.State == 2 && Dist(new PointF(c.X, c.Y), moth.X, moth.Y) < 10 && t - moth.B > 1.2f)
            {
                MothDodge(t);
                c.A++;
            }
            if (leaving && (c.X < -30 || c.X > 330 || c.Y < -25) || s > 45) c.State = 0;
        }

        void StartWorm(float t)
        {
            if (!Fits(Visitors.Worm)) return;
            var c = worm;
            if (c.Active) return;
            var spot = WormSpots[rng.Next(WormSpots.Length)];
            if (Dist(spot, GardenArt.CatAt.X, GardenArt.CatAt.Y) < 20 && cat.Active || Dist(spot, rabbit.X, rabbit.Y) < 20 && rabbit.Active) spot = WormSpots[0];
            c.State = 1;
            c.T0 = t;
            c.X = spot.X;
            c.Y = spot.Y;
            c.Side = Rand < 0.5f ? 1 : -1;
            c.K = c.A = c.C = 0;
            c.B = 12 + Rand * 8;
            c.D = 0;
            wormNext = t + 120;
        }

        void StepWorm(float t)
        {
            var c = worm;
            if (!c.Active) return;
            float s = t - c.T0;
            switch (c.State)
            {
                case 1:
                    c.K = Smooth(0, 2.2f, s) * 0.55f;
                    c.A = 0;
                    if (s > 0.8f && c.D == 0)
                    {
                        c.D = 1;
                        bool first = !state.SeenToday.Contains(Visitors.Worm);
                        Visit(Visitors.Worm);
                        if (first && !Sleeping) Chat(Tone == Tone.Dry ? "A worm. Living its best wet life." : "A worm! The rain brought it up.", 2.8f);
                        if (Daylight && !bird.Active && !cat.Active && t > wormBirdNext && Rand < 0.55f) StartGroundBird(t, new PointF(c.X - c.Side * 12, c.Y));
                    }
                    if (s > 2.2f) { c.State = 2; c.T0 = t; }
                    break;
                case 2:
                    c.K = 0.55f + 0.45f * Smooth(0, 2.5f, s);
                    c.A = Smooth(0.8f, 3.5f, s) * (0.85f + 0.15f * Sin(t * 0.7f));
                    if (s > c.B || !WetGround && s > 4) WormRetract(t);
                    break;
                case 3:
                    float u = Clamp(s / 0.45f, 0, 1);
                    c.K = c.C * (1 - u);
                    c.A *= 0.85f;
                    if (u >= 1) { c.State = 0; castAt = new PointF(c.X, c.Y); castT = t; }
                    break;
            }
            if ((c.State == 1 || c.State == 2) && pointerIn && t - pointerT < 0.5f && Dist(pointer, c.X, c.Y - 4) < 14) WormRetract(t);
        }

        void WormRetract(float t)
        {
            var c = worm;
            if (c.State != 1 && c.State != 2) return;
            c.C = c.K;
            c.State = 3;
            c.T0 = t;
        }

        void StartMouse(float t)
        {
            if (!Fits(Visitors.Mouse)) return;
            var c = mouse;
            if (c.Active) return;
            bool left = cat.Active || Rand < 0.5f;
            c.State = 1;
            c.T0 = t;
            c.Side = left ? 1 : -1;
            c.X = left ? -10 : 310;
            c.Y = MouseY;
            bool bold = cat.Active && cat.State == 3;
            c.To = new PointF(bold ? 178 + Rand * 8 : left ? 112 + Rand * 14 : 172 + Rand * 10, MouseY);
            c.B = 9 + Rand * 6;
            c.K = c.A = c.D = 0;
            mouseGo = -1;
            mouseNextMove = t + 0.2f;
            var p = c.P;
            p.Sit = p.Run = p.Flick = p.Blink = 0;
            p.Acorn = false;
        }

        void MoveMouse(float t, PointF to, float speed)
        {
            var c = mouse;
            mouseFrom = new PointF(c.X, c.Y);
            mouseTo = to;
            mouseGo = t;
            mouseLen = Math.Max(0.05f, Dist(mouseFrom, to.X, to.Y) / speed);
            if (Math.Abs(to.X - c.X) > 0.5f) c.Side = to.X > c.X ? 1 : -1;
        }

        bool MouseMoving(float t, float dt)
        {
            var c = mouse;
            if (mouseGo < 0) { c.P.Run = Toward(c.P.Run, 0, dt * 8); return false; }
            float u = Clamp((t - mouseGo) / mouseLen, 0, 1), e = 1 - (1 - u) * (1 - u);
            float nx = mouseFrom.X + (mouseTo.X - mouseFrom.X) * e, ny = mouseFrom.Y + (mouseTo.Y - mouseFrom.Y) * e;
            c.P.Gait += Dist(new PointF(nx, ny), c.X, c.Y) * 2.2f;
            c.X = nx;
            c.Y = ny;
            c.P.Run = 1;
            if (u < 1) return true;
            mouseGo = -1;
            return false;
        }

        void MouseFlee(float t)
        {
            var c = mouse;
            if (c.State < 1 || c.State > 3) return;
            c.State = 7;
            c.T0 = t;
            mouseGo = -1;
            c.P.Sit = 0;
            c.P.Acorn = false;
        }

        void StepMouse(float t, float dt)
        {
            var c = mouse;
            if (!c.Active) return;
            var p = c.P;
            float s = t - c.T0;
            bool moving = MouseMoving(t, dt);
            p.Flick = moving ? 0 : 1;
            p.Blink = (t + 0.3f) % 3.3f < 0.12f ? 1 : 0;
            bool catComing = cat.Active && cat.D == 0 && (cat.State == 1 || cat.State == 2);
            if (c.State >= 1 && c.State <= 3 && catComing && c.X > 0 && c.X < 300)
            {
                c.State = 4;
                c.T0 = t;
                mouseGo = -1;
                p.Sit = 0;
                p.Acorn = false;
                return;
            }
            if (c.State >= 1 && c.State <= 2 && (pointerIn && t - pointerT < 0.3f && Dist(pointer, c.X, c.Y - 3) < 24 || fox.Active && fox.State != 3)) { MouseFlee(t); return; }
            int away = cat.Active && cat.X > SX ? -1 : cat.Active ? 1 : (c.X < SX ? -1 : 1);
            switch (c.State)
            {
                case 1:
                    p.Sit = Toward(p.Sit, 0, dt * 6);
                    if (moving || t < mouseNextMove) break;
                    if (Math.Abs(c.To.X - c.X) < 1.5f)
                    {
                        c.State = 2;
                        c.T0 = t;
                        Visit(Visitors.Mouse);
                        if (!Sleeping && !Known(Visitors.Mouse)) Chat(Tone == Tone.Dry ? "A mouse. Looking for crumbs. Relatable." : "A mouse! Look at its tiny feet.", 2.8f);
                        break;
                    }
                    float step = Math.Sign(c.To.X - c.X) * Math.Min(Math.Abs(c.To.X - c.X), 18 + Rand * 22);
                    MoveMouse(t, new PointF(c.X + step, Clamp(c.Y + (Rand - 0.5f) * 2, MouseY - 2, MouseY + 2)), 90);
                    mouseNextMove = t + mouseLen + 0.35f + Rand * 0.8f;
                    break;
                case 2:
                    float k = s % 8;
                    p.Sit = Smooth(1.3f, 1.6f, k) * (1 - Smooth(5.2f, 5.5f, k));
                    p.Acorn = k > 1.4f && k < 4.6f;
                    p.Flick = p.Sit > 0.5f ? 0.4f : 1;
                    bool catNear = cat.State == 3 && Math.Abs(cat.X - c.X) < 70;
                    if (catNear && c.K == 0 && s > 3) { c.K = 1; mouseAlarmT = t; }
                    if (c.K == 1 && t - mouseAlarmT < 1.3f) { p.Sit = 0; p.Flick = 0; p.Acorn = false; }
                    if (s > c.B || c.K == 1 && t - mouseAlarmT > 1.3f)
                    {
                        c.State = 3;
                        c.T0 = t;
                        c.To = new PointF(away < 0 ? -14 : 314, MouseY);
                        mouseNextMove = t;
                        p.Acorn = false;
                    }
                    break;
                case 3:
                    p.Sit = Toward(p.Sit, 0, dt * 6);
                    if (moving || t < mouseNextMove) break;
                    if (c.X < -8 || c.X > 308) { c.State = 0; break; }
                    float go = Math.Sign(c.To.X - c.X) * Math.Min(Math.Abs(c.To.X - c.X), 20 + Rand * 26);
                    MoveMouse(t, new PointF(c.X + go, MouseY), 110);
                    mouseNextMove = t + mouseLen + 0.2f + Rand * 0.5f;
                    break;
                case 4:
                    p.Sit = 0;
                    p.Flick = 0;
                    if (s > 0.55f) { c.State = 5; c.T0 = t; c.K = 0; MoveMouse(t, MouseHide, 120); }
                    break;
                case 5:
                    if (moving) break;
                    bool catRight = !cat.Active || cat.X > SX;
                    if (c.K == 0 && s > 1.4f) { c.K = 1; MoveMouse(t, catRight ? MousePeekRight : MousePeekLeft, 30); break; }
                    if (c.K == 1) c.Side = catRight ? 1 : -1;
                    if (c.K == 1 && (cat.State == 3 && s > 6 || !cat.Active || cat.D == 1))
                    {
                        c.State = 6;
                        c.T0 = t;
                        c.K = 0;
                        MoveMouse(t, catRight ? MousePeekLeft : MousePeekRight, 26);
                    }
                    break;
                case 6:
                    if (moving) break;
                    bool exitLeft = !cat.Active || cat.X > SX;
                    if (c.K == 0) { c.K = 1; MoveMouse(t, new PointF(c.X + (exitLeft ? -22 : 22), MouseY), 45); break; }
                    c.State = 3;
                    c.T0 = t;
                    c.To = new PointF(exitLeft ? -14 : 314, MouseY);
                    mouseNextMove = t + 0.3f;
                    break;
                case 7:
                    if (moving) break;
                    if (c.X < -8 || c.X > 308) { c.State = 0; break; }
                    MoveMouse(t, new PointF(c.X < SX ? -14 : 314, MouseY), 150);
                    break;
            }
        }

        bool MouseBehind => mouse.Active && mouse.Y < 196;

        void DrawWildGround(Graphics g, float t, float night)
        {
            if (prints.Count > 0)
                foreach (var p in prints) WildArt.DrawRabbitPrints(g, new[] { p.At }, 1 - Smooth(240, 300, t - p.T), night);
            if (castT > 0) WildArt.DrawCastings(g, castAt.X, castAt.Y, 1 - Smooth(40, 60, t - castT), night);
            if (worm.Active) WildArt.DrawWorm(g, worm.X, worm.Y, worm.Side, t, worm.K, worm.A, night);
            if (rabbit.Active) WildArt.DrawRabbit(g, rabbit.X, rabbit.Y, t, rabbit.Side, rabbit.P, night);
            if (mouse.Active && !MouseBehind) WildArt.DrawMouse(g, mouse.X, mouse.Y, t, mouse.Side, mouse.P, night);
        }

        void DrawWildBehind(Graphics g, float t, float night)
        {
            if (MouseBehind) WildArt.DrawMouse(g, mouse.X, mouse.Y, t, mouse.Side, mouse.P, night);
        }

        void DrawWildAir(Graphics g, float t, float night)
        {
            if (bat.Active) WildArt.DrawBat(g, bat.X, bat.Y, Sin(bat.K), bat.Side, 1, night);
        }
    }
}
