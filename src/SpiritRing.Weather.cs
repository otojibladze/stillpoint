using System;
using System.Collections.Generic;
using System.Drawing;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        const float RainFrogX = 113, RainFrogY = 207;
        float windK, gustK, gustPower = 0.9f, shelterK, shelterNext = 20, rainFrogNext = 15, leafDripNext, birdPeck, birdCock, birdShakeT = -99, wormBirdNext = 30;
        PointF birdGround;

        float WindPush => windK * gustK;
        bool BirdShelters => bird.D == 1 && bird.State == 2;
        PointF ShelterPerch => Surface(1.56f * Pi);
        PointF BirdSeat => bird.D == 1 ? ShelterPerch : bird.D == 2 ? birdGround : points.Head;

        void StepWind(float t, float dt, WeatherNow real)
        {
            float target = real == null ? 0 : real.Windiness;
            windK += (target - windK) * (1 - (float)Math.Exp(-dt * 0.5));
            if (t > gustNext)
            {
                gustT = t;
                gustNext = t + (18 + Rand * 25) * (1 - 0.6f * windK);
                gustPower = 0.9f + 1.4f * windK * (0.6f + 0.4f * Rand);
            }
            gustK = Sin(Clamp((t - gustT) / 3.5f, 0, 1) * Pi);
            wind = (0.35f + 0.35f * windK) * Sin(t * 0.37f) + 0.2f * Sin(t * 0.91f + 1) + gustPower * gustK;
            shelterK += ((BirdShelters ? 1 : 0) - shelterK) * Math.Min(1, dt * 2);
        }

        bool TryRainVisitor(float t)
        {
            var picks = new List<(float Weight, Action<float> Go)>();
            if (!bird.Active && !NestShown && t > shelterNext && nightK < 0.4f) picks.Add((2f, StartShelter));
            if (!frog.Active && season != Season.Winter && t > rainFrogNext) picks.Add((2f, StartRainFrog));
            if (!worm.Active && WetGround && t > wormNext) picks.Add((1.2f, StartWorm));
            if (picks.Count == 0 || Rand > 0.6f) return false;
            PickOne(picks, t);
            return true;
        }

        void PickOne(List<(float Weight, Action<float> Go)> picks, float t)
        {
            float total = 0;
            foreach (var p in picks) total += p.Weight;
            if (total <= 0) return;
            float r = Rand * total;
            foreach (var p in picks)
                if (p.Weight > 0 && (r -= p.Weight) <= 0) { p.Go(t); return; }
            for (int i = picks.Count - 1; i >= 0; i--)
                if (picks[i].Weight > 0) { picks[i].Go(t); return; }
        }

        void StartShelter(float t)
        {
            if (bird.Active) return;
            StartFlyer(bird, t);
            bird.D = 1;
            shelterNext = t + 240;
        }

        void StartGroundBird(float t, PointF at)
        {
            if (bird.Active) return;
            StartFlyer(bird, t);
            bird.D = 2;
            birdGround = at;
            wormBirdNext = t + 180;
        }

        void StepBird(float t, float dt)
        {
            var c = bird;
            birdPeck = birdCock = 0;
            if (c.State == 1)
            {
                var seat = BirdSeat;
                float u = Clamp((t - c.T0) / (c.D == 1 ? 2f : 2.6f), 0, 1), e = Smooth(0, 1, u);
                c.X = c.From.X + (seat.X - c.From.X) * e;
                c.Y = c.From.Y + (seat.Y - c.From.Y) * e - Sin(u * Pi) * (c.D == 2 ? 26 : 18);
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    c.C = t + 4 + Rand * 3;
                    BirdLanded(t);
                }
            }
            else if (c.State == 2)
            {
                float since = t - c.T0;
                var seat = BirdSeat;
                if (c.D == 1) StepShelter(t, since, seat);
                else if (c.D == 2) StepGroundBird(t, since, seat);
                else
                {
                    float hop = since % 1.6f < 0.25f ? Sin(since % 1.6f / 0.25f * Pi) * 3 : 0;
                    c.X = seat.X;
                    c.Y = seat.Y - hop;
                    if (since > 0.5f && (int)(since / SongEvery) != (int)c.A) { c.A = (int)(since / SongEvery); Emit(new Particle { Kind = Bit.Note, X = c.X + 6 * -c.Side, Y = c.Y - 12, Vx = -c.Side * 10, Vy = -22, Vr = 0.3f, Life = 1.2f, Size = 8 }); }
                    birdCock = DayCatStalking ? 0.4f * Window(0, 2, since % 3.4f, 0.4f) : 0;
                    float stay = DayCatStalking ? 30 : NestBusy ? 10 : 5.5f;
                    if (since > stay || Running || Sleeping) SendAway(c, t);
                }
            }
            else if (c.State == 3) FlyOut(c, t, 2.4f);
            if (c.State == 0) c.D = 0;
        }

        bool FlushBird(float t)
        {
            var c = bird;
            if (c.State != 1 && c.State != 2) return false;
            c.Side = c.X < SX ? 1 : -1;
            SendAway(c, t);
            c.To = new PointF(SX - c.Side * 210, -20);
            Emit(new Particle { Kind = Bit.Feather, X = c.X, Y = c.Y - 3, Vx = -c.Side * 12, Vy = -10, Vr = 2, Life = 2.4f, Night = nightK });
            return true;
        }

        void BirdLanded(float t)
        {
            Visit(Visitors.Bird);
            bird.K = 0;
            if (bird.D == 1)
            {
                bird.Side = 1;
                Chat(Tone == Tone.Dry ? "Fine. You can borrow my leaf." : Tone == Tone.Worry ? "Quick, get under my leaf!" : Tone == Tone.Plain ? "You can stay till the rain stops." : "Stay under my leaf, little one.", 3.2f);
                return;
            }
            if (bird.D == 2)
            {
                bird.Side = worm.Active && worm.X > bird.X ? -1 : 1;
                return;
            }
            if (NestBusy) Chat(NestLine(), 3f);
            else if (!Known(Visitors.Bird)) Chat(Pick(new[] { "A little bird. It tickles.", "Tweet. That's bird for hello.", "We have company." }), 2.6f);
            MaybeDayCat(t);
        }

        void StepShelter(float t, float since, PointF seat)
        {
            var c = bird;
            c.X = seat.X;
            c.Y = seat.Y;
            if (t > c.C)
            {
                c.C = t + 6 + Rand * 4;
                birdShakeT = t;
                for (int i = 0; i < 6; i++)
                {
                    float a = -Pi / 2 + (i - 2.5f) * 0.45f;
                    Emit(new Particle { Kind = Bit.Dew, X = c.X, Y = c.Y - 6, Vx = Cos(a) * 45, Vy = Sin(a) * 45, Size = 1.2f, Target = c.Y + 2, Life = 1.2f });
                }
            }
            if (rainK > 0.2f && !snowing && t > leafDripNext)
            {
                leafDripNext = t + 0.45f + Rand * 0.4f;
                Emit(new Particle { Kind = Bit.Dew, X = points.Tip.X + (Rand - 0.5f) * 5, Y = points.Tip.Y + 3, Vy = 6, Size = 1.1f, Target = seat.Y - 1, Life = 1.5f });
            }
            if (rainK < 0.12f && since > 4)
            {
                Emit(new Particle { Kind = Bit.Note, X = c.X - 6, Y = c.Y - 14, Vx = -10, Vy = -22, Vr = 0.3f, Life = 1.2f, Size = 8 });
                SendAway(c, t);
            }
            else if (since > 110 || Sleeping || nightK > 0.5f) SendAway(c, t);
        }

        void StepGroundBird(float t, float since, PointF seat)
        {
            var c = bird;
            float dir = -c.Side;
            float step = Smooth(1.2f, 1.5f, since);
            c.X = seat.X + 4 * dir * step;
            c.Y = seat.Y - Window(1.2f, 1.5f, since, 0.12f) * 2;
            birdCock = Window(0.3f, 1.1f, since, 0.2f) * 0.8f + Window(2.8f, 3.6f, since, 0.2f) * 0.6f;
            birdPeck = Window(2.2f, 2.65f, since, 0.12f);
            if (since > 2.05f) WormRetract(t);
            if (since > 2.7f && c.K == 0)
            {
                c.K = 1;
                Later(Tone == Tone.Dry ? "The worm wins this round." : Tone == Tone.Worry ? "Phew. The worm got away." : "Missed! The worm was too quick.", t + 0.3f, 2.6f);
            }
            if (since > 4) SendAway(c, t);
        }

        void DrawBirdNow(Graphics g, float t)
        {
            if (!bird.Active || BirdInNest) return;
            float facing = bird.State == 3 ? bird.To.X - bird.From.X : -bird.Side;
            float shake = Window(0, 0.45f, t - birdShakeT, 0.08f) * (bird.D == 1 ? 1 : 0);
            SpiritArt.DrawBird(g, bird.X, bird.Y, t, facing, bird.State != 2, bird.D == 1 ? shelterK : 0, birdPeck, birdCock, shake);
        }

        void StartRainFrog(float t)
        {
            if (frog.Active) return;
            frog.State = 1;
            frog.T0 = t;
            frog.D = 1;
            frog.A = 0;
            rainFrogNext = t + 180;
        }

        void StepFrog(float t, float dt)
        {
            var c = frog;
            float since = t - c.T0;
            bool rain = c.D == 1;
            float homeX = rain ? RainFrogX : GardenArt.FrogAt.X, homeY = rain ? RainFrogY : GardenArt.FrogAt.Y;
            if (c.State == 1)
            {
                float u = Clamp(since / (rain ? 2.4f : 1.6f), 0, 1);
                c.X = 22 + (homeX - 22) * u;
                c.Y = homeY - Math.Abs(Sin(u * Pi * (rain ? 3 : 2))) * 9;
                if (u >= 1)
                {
                    c.State = 2;
                    c.T0 = t;
                    c.C = -1;
                    Visit(Visitors.Frog);
                    if (rain) Chat(Tone == Tone.Dry ? "One of us loves this weather." : Tone == Tone.Worry ? "At least someone's happy in this rain." : "Look how happy the frog is!", 3f);
                    else if (!Known(Visitors.Frog)) Chat("Ribbit. The rain brought a friend.", 3f);
                }
            }
            else if (c.State == 2)
            {
                c.X = homeX;
                if (rain)
                {
                    float cycle = since % 3.2f;
                    int n = (int)(since / 3.2f);
                    float jump = cycle < 0.55f ? Sin(cycle / 0.55f * Pi) : 0;
                    c.Y = homeY - jump * 7;
                    if (cycle >= 0.55f && c.C != n)
                    {
                        c.C = n;
                        for (int i = 0; i < 3; i++) Emit(new Particle { Kind = Bit.Splash, X = homeX + (i - 1) * 4, Y = homeY + 0.5f, Life = 0.35f, Night = nightK });
                        for (int i = 0; i < 4; i++) Emit(new Particle { Kind = Bit.Dew, X = homeX + (Rand - 0.5f) * 6, Y = homeY - 1, Vx = (Rand - 0.5f) * 50, Vy = -30 - Rand * 25, Size = 1, Target = homeY + 1, Life = 1 });
                    }
                    c.K = cycle > 1.0f && cycle < 1.9f ? Sin((cycle - 1) / 0.9f * Pi) : 0;
                    if (c.K > 0.95f && t > c.B)
                    {
                        c.B = t + 0.6f;
                        Emit(new Particle { Kind = Bit.Note, X = homeX + 4, Y = homeY - 12, Vx = 8, Vy = -20, Vr = 0.3f, Life = 1.1f, Size = 7 });
                    }
                    c.A = jump > 0.1f || c.K > 0.2f ? 1 : 0;
                    if (rainK < 0.15f && since > 6 || since > 60 || snowing) { c.State = 3; c.T0 = t; c.K = 0; c.A = 0; }
                }
                else
                {
                    c.Y = homeY;
                    float phase = since % 2.6f;
                    c.K = phase < 0.9f ? Sin(phase / 0.9f * Pi) : 0;
                    if (since > 11 || Running) { c.State = 3; c.T0 = t; c.K = 0; }
                }
            }
            else if (c.State == 3)
            {
                float u = Clamp(since / 1.6f, 0, 1);
                c.X = homeX - (homeX - 10) * u;
                c.Y = homeY - Math.Abs(Sin(u * Pi * 2)) * 9;
                if (u >= 1) { c.State = 0; c.D = 0; }
            }
        }

        void StepButterfly(float t, float dt)
        {
            var c = butterfly;
            float push = WindPush;
            c.K += dt * (15 + 16 * push);
            if (c.State == 1)
            {
                float u = Clamp((t - c.T0) / 3.4f, 0, 1), e = Smooth(0, 1, u);
                c.X = c.From.X + (points.Tip.X - c.From.X) * e + Sin(u * 13) * 16 * (1 - u) + wind * windK * 12 * (1 - e);
                c.Y = c.From.Y + (points.Tip.Y - 5 - c.From.Y) * e + Cos(u * 9) * 12 * (1 - u) + Sin(t * 8) * 3 * push;
                c.A = 0;
                if (push > 0.5f && c.D == 0 && u > 0.2f && u < 0.9f) { BlowAway(c, t); return; }
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
                c.A = Smooth(0.2f, 0.55f, push);
                if (push > 0.8f && c.D < 2) { BlowAway(c, t); return; }
                if (t - c.T0 > 4.5f || Running || Sleeping) SendAway(c, t);
            }
            else if (c.State == 3) FlyOut(c, t, 3);
            else if (c.State == 5)
            {
                float u = Clamp((t - c.T0) / 1.8f, 0, 1);
                c.X = c.From.X + c.B * 60 * Smooth(0, 1, u);
                c.Y = c.From.Y - Sin(u * Pi) * 16 + 12 * u;
                c.C = Sin(u * Pi * 3) * 140 * (1 - u);
                if (u >= 1)
                {
                    c.T0 = t;
                    c.From = new PointF(c.X, c.Y);
                    if (c.D >= 2 || Running || Sleeping) { c.State = 3; c.To = new PointF(c.X + c.B * 200, -14); }
                    else c.State = 1;
                }
            }
        }

        void BlowAway(Critter c, float t)
        {
            c.State = 5;
            c.T0 = t;
            c.From = new PointF(c.X, c.Y);
            c.B = wind >= 0 ? 1 : -1;
            c.D++;
            if (c.D == 1) Chat(Tone == Tone.Dry ? "Windy. Bad day to be paper-thin." : "Hang on, little one!", 2.6f);
        }

        void DrawButterflyNow(Graphics g, float t)
        {
            var c = butterfly;
            if (!c.Active) return;
            SpiritArt.DrawButterfly(g, c.X, c.Y, t, c.State == 2, c.A, c.State == 5 ? c.C : 0, c.K);
        }

        void ApplyWeatherPose(SpiritPose p)
        {
            p.Wind += 2.6f * shelterK;
        }
    }
}
