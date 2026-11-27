using System;
using System.Drawing;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        float dayCatNext = 60, foxChaseNext = 90, pairNext = 10, catAlert, owlHootT = -99;

        bool DayCatStalking => cat.D == 1 && cat.State >= 1 && cat.State <= 3;
        bool Prey => bird.State == 2 && bird.D == 0;

        void MaybeDayCat(float t)
        {
            if (cat.Active || !Daylight || rainK > 0.2f || Sleeping || Running || t < dayCatNext || Rand > 0.3f) return;
            StartDayCat(t);
        }

        void StartDayCat(float t)
        {
            if (!Fits(Visitors.Cat)) return;
            if (cat.Active) return;
            var p = cat.P;
            cat.State = 1;
            cat.D = 1;
            cat.T0 = t;
            cat.Side = 1;
            cat.X = GardenArt.CatAt.X + 95;
            cat.A = 0;
            cat.K = 0;
            p.Curl = p.Sit = p.Hide = p.Tilt = p.Flick = p.Walk = p.Yawn = p.Ear = p.Blink = 0;
            p.Paw = 0;
            p.Run = 1;
            dayCatNext = t + 300;
        }

        void LeaveCat(float t)
        {
            cat.State = cat.D == 1 ? 7 : 5;
            cat.T0 = t;
            cat.Side = -1;
            cat.From = new PointF(cat.X, 0);
            cat.P.Paw = 0;
        }

        void StepCat(float t, float dt)
        {
            if (cat.D == 1) { StepDayCat(t, dt); return; }
            bool want = nightK > 0.7f && Sleeping && Habitats.Fits(Visitors.Cat, LiveEnv());
            var p = cat.P;
            float s = t - cat.T0, home = GardenArt.CatAt.X, away = home + 95;
            switch (cat.State)
            {
                case 0:
                    if (!want) return;
                    cat.State = 1; cat.T0 = t; cat.Side = 1; cat.X = away; cat.A = 0; p.Curl = 0; p.Blink = 0; p.Sit = p.Hide = p.Tilt = p.Flick = p.Ear = 0;
                    break;
                case 1:
                    if (!want) { LeaveCat(t); break; }
                    float u = Clamp(s / 5.5f, 0, 1);
                    float x = away + (home - away) * (1 - (1 - u) * (1 - u));
                    p.Gait += Math.Abs(x - cat.X) * 0.45f;
                    cat.X = x;
                    p.Run = 1 - Smooth(0.8f, 1, u);
                    if (u >= 1) { cat.State = 2; cat.T0 = t; }
                    break;
                case 2:
                    if (!want) { cat.State = 4; cat.T0 = t - (1 - p.Curl) * 1.6f; break; }
                    p.Run = 0;
                    p.Blink = Window(0.4f, 1.1f, s, 0.25f);
                    p.Curl = Smooth(0.9f, 2.8f, s);
                    if (s > 2.8f) { cat.State = 3; Visit(Visitors.Cat); }
                    break;
                case 3:
                    float near = mouse.Active && mouse.Y > 196 ? Math.Abs(mouse.X - cat.X) : 999;
                    catAlert += ((near < 60 ? 1 : 0) - catAlert) * Math.Min(1, dt * 3);
                    p.Curl = 1 - 0.2f * catAlert;
                    p.Blink = 1 - 0.6f * catAlert;
                    float burst = (t * 0.5f) % 2;
                    p.Ear = near < 85 && burst < 0.5f ? Math.Abs(Sin(burst * Pi * 4)) : 0;
                    if (!want) { cat.State = 4; cat.T0 = t; p.Ear = 0; }
                    break;
                case 4:
                    p.Curl = 1 - Smooth(0, 1.6f, s);
                    p.Blink = s < 1.6f ? 0 : Window(1.9f, 2.4f, s, 0.15f);
                    if (s > 2.6f) LeaveCat(t);
                    break;
                case 5:
                    float v = Clamp(s / 4.5f, 0, 1);
                    float nx = cat.From.X + (away - cat.From.X) * v * v;
                    p.Gait += Math.Abs(nx - cat.X) * 0.45f;
                    cat.X = nx;
                    p.Run = Smooth(0, 0.15f, v);
                    p.Curl = Math.Max(0, p.Curl - dt * 2);
                    if (v >= 1) cat.State = 0;
                    break;
            }
        }

        void StepDayCat(float t, float dt)
        {
            var p = cat.P;
            float s = t - cat.T0, home = GardenArt.CatAt.X, away = home + 95;
            if ((nightK > 0.6f || Sleeping || rainK > 0.35f) && cat.State < 7) LeaveCat(t);
            switch (cat.State)
            {
                case 1:
                    float u = Clamp(s / 5f, 0, 1);
                    float x = away + (home - away) * (1 - (1 - u) * (1 - u));
                    p.Gait += Math.Abs(x - cat.X) * 0.45f;
                    cat.X = x;
                    p.Run = 1 - Smooth(0.8f, 1, u);
                    if (u >= 1) { cat.State = 2; cat.T0 = t; Visit(Visitors.Cat); }
                    break;
                case 2:
                    p.Run = 0;
                    p.Sit = Smooth(0, 0.6f, s);
                    p.Tilt += ((Prey ? 0.85f : 0) * Smooth(0.4f, 1, s) - p.Tilt) * Math.Min(1, dt * 5);
                    p.Flick = Prey ? 1 : 0.3f;
                    p.Yawn = Prey && s > 1.6f && s < 2.8f ? Math.Abs(Sin(t * 28)) * 0.7f : 0;
                    if (s > 4.2f) { cat.State = Prey ? 3 : 6; cat.T0 = t; p.Yawn = 0; }
                    break;
                case 3:
                    if (cat.K == 0)
                    {
                        cat.K = 1;
                        Chat(Tone == Tone.Worry ? "Bird! Watch out!" : Tone == Tone.Dry ? "Here we go." : Tone == Tone.Plain ? "Uh oh." : "Don't you dare, cat.", 2.4f);
                    }
                    p.Sit = 1 - Smooth(0, 0.5f, s);
                    p.Hide = Smooth(0, 0.7f, s);
                    p.Tilt += (0.45f - p.Tilt) * Math.Min(1, dt * 5);
                    p.Walk = s > 0.9f ? 1 : 0;
                    if (!Prey) { cat.State = 5; cat.T0 = t; p.Walk = 0; break; }
                    if (s > 2.3f)
                    {
                        cat.State = 4;
                        cat.T0 = t;
                        cat.From = new PointF(cat.X, 0);
                        p.Walk = 0;
                        SendAway(bird, t);
                        for (int i = 0; i < 2; i++) Emit(new Particle { Kind = Bit.Note, X = bird.X + 4, Y = bird.Y - 12, Vx = (i - 0.5f) * 20, Vy = -26, Vr = 0.4f, Life = 0.9f, Size = 7 });
                    }
                    break;
                case 4:
                    float k = Clamp(s / 0.85f, 0, 1);
                    cat.X = cat.From.X - 24 * k;
                    cat.A = Sin(k * Pi) * 13;
                    p.Hide = 0.35f * (1 - k);
                    p.Run = 1 - k;
                    p.Gait = 1.4f;
                    p.Tilt = 0.5f * (1 - k);
                    if (k >= 1)
                    {
                        cat.State = 5;
                        cat.T0 = t;
                        cat.A = 0;
                        for (int i = 0; i < 4; i++) Emit(new Particle { Kind = Bit.Puff, X = cat.X + (i - 1.5f) * 5, Y = GardenArt.CatAt.Y - 1, Vx = (i - 1.5f) * 14, Vy = -6, Life = 0.6f, Size = 2.4f });
                        Later(Tone == Tone.Dry ? "Smooth. Very smooth." : Tone == Tone.Worry ? "Phew. Everyone's okay." : Tone == Tone.Plain ? "Nice try, cat." : "Missed! Good.", t + 0.8f, 2.6f);
                    }
                    break;
                case 5:
                    p.Run = 0;
                    p.Hide = Math.Max(0, p.Hide - dt * 3);
                    p.Sit = Smooth(0, 0.5f, s);
                    p.Tilt = s < 1.6f ? 0.9f * Window(0, 1.6f, s, 0.3f) : 0;
                    p.Paw = s > 1.8f && s < 4.6f ? 1 : 0;
                    p.Flick = 0.3f;
                    if (s > 5.2f) LeaveCat(t);
                    break;
                case 6:
                    p.Run = 0;
                    p.Sit = s < 4 ? Smooth(0, 0.6f, s) : 1 - Smooth(4, 4.6f, s);
                    p.Paw = s > 0.8f && s < 3.6f ? 1 : 0;
                    p.Curl = Smooth(4.2f, 5.6f, s) * (1 - Smooth(17, 18.4f, s));
                    p.Blink = Smooth(5, 6, s) * (1 - Smooth(16.6f, 17.2f, s));
                    if (s > 19) LeaveCat(t);
                    break;
                case 7:
                    float v = Clamp(s / 4.5f, 0, 1);
                    float nx = cat.From.X + (away - cat.From.X) * v * v;
                    p.Gait += Math.Abs(nx - cat.X) * 0.45f;
                    cat.X = nx;
                    p.Run = Smooth(0, 0.15f, v);
                    p.Sit = Math.Max(0, p.Sit - dt * 3);
                    p.Hide = Math.Max(0, p.Hide - dt * 3);
                    p.Curl = Math.Max(0, p.Curl - dt * 3);
                    p.Tilt = Math.Max(0, p.Tilt - dt * 3);
                    p.Blink = 0;
                    p.Flick = 0;
                    p.Yawn = 0;
                    cat.A = 0;
                    if (v >= 1) { cat.State = 0; cat.D = 0; }
                    break;
            }
        }

        void DrawCatNow(Graphics g, float t)
        {
            if (!cat.Active) return;
            float y = GardenArt.CatAt.Y - cat.A - Math.Abs(Cos(cat.P.Gait)) * 0.6f * cat.P.Run;
            SpiritArt.DrawCat(g, cat.X, y, t, cat.Side, cat.P);
        }

        void MaybeFoxChase(float t)
        {
            if (!Fits(Visitors.Fox)) return;
            if (fox.Active || t < foxChaseNext || nightK > 0.7f || Sleeping || rainK > 0.3f || Rand > 0.25f) return;
            foxChaseNext = t + 300;
            var c = fox;
            c.State = 1;
            c.T0 = t;
            c.Side = 1;
            c.X = -24;
            c.Y = GardenArt.FoxAt.Y;
            c.A = Math.Min(squirrel.X - 30, 186);
            c.B = 6.5f;
            c.C = 1;
            c.D = (int)TrustOf(Visitors.Fox);
            c.K = 0;
            c.P.Sit = c.P.Curl = c.P.Hide = c.P.Flick = c.P.Tilt = c.P.Blink = c.P.Ear = c.P.Yawn = 0;
            Chat(Tone == Tone.Dry ? "Uh oh. Squirrel, run." : Tone == Tone.Worry ? "Oh no, a fox! Squirrel, go!" : Tone == Tone.Plain ? "A fox. The squirrel won't stay for this." : "A fox! Run, little squirrel!", 2.8f);
        }

        void PairUp(float t)
        {
            if (t < pairNext || nightK < 0.7f || rainK > 0.4f || story.Active) return;
            pairNext = t + 8;
            if (moth.Active) return;
            if (owl.State == 2 && t - owl.T0 > 3 && Rand < 0.35f) { StartMoth(t); pairNext = t + 90; }
            else if (bat.Active && Rand < 0.4f) { StartMoth(t); pairNext = t + 90; }
        }

        void MothFlee(float t, PointF from)
        {
            var c = moth;
            if (c.State != 2) return;
            c.State = 3;
            c.T0 = t;
            c.D = 1;
            c.From = new PointF(c.X, c.Y);
            c.To = new PointF(c.X + (c.X >= from.X ? 1 : -1) * 150, c.Y - 60);
        }

        void MothDodge(float t)
        {
            if (moth.State == 2) moth.B = t;
        }
    }
}
