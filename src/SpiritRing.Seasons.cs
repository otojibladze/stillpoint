using System;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        static readonly PointF[] FrogletSpots = { new PointF(51, 187.5f), new PointF(91, 189), new PointF(97, 183.5f) };
        readonly Critter[] cubs = { new Critter(), new Critter(), new Critter() };
        CubStage cubStage;
        PondStage pondStage;
        float cubProgress, pondProgress, seasonCheckT = -99;
        int cubCount = 2;
        bool cubsTold;

        bool Pond => (state.Keepsakes & Keepsake.Pond) != 0;
        bool CubsOut => fox.Active && fox.C == 0 && cubStage != CubStage.None && (Trust)(int)fox.D >= Trust.Curious;

        void StepSeasons(float t, float dt)
        {
            if (t - seasonCheckT > 5)
            {
                seasonCheckT = t;
                var now = Clock();
                (cubStage, cubProgress) = Wildlife.Cubs(now, state.Southern);
                cubCount = Wildlife.CubCount(now);
                (pondStage, pondProgress) = Wildlife.Pond(now, state.Southern);
                if (season == Season.Winter || snowGround) pondStage = PondStage.None;
            }
            StepCubs(t, dt);
            if (Pond && pondStage != PondStage.None && !Sleeping && nightK < 0.5f && t > viewT + 3)
            {
                string day = Clock().ToString("yyyyMMdd");
                if (pondStage == PondStage.Spawn && Tell("spawn" + day, null)) Chat("Frogspawn in our pond! Tadpoles soon.", 3.2f);
                else if (pondStage == PondStage.Tadpoles && Tell("tadpoles" + day, null)) Chat("Tadpoles! Our pond is wiggling.", 3f);
                else if (pondStage == PondStage.Froglets && Tell("froglets" + day, Visitors.Froglets)) Chat("Our tadpoles turned into tiny frogs!", 3.2f);
            }
        }

        void StepCubs(float t, float dt)
        {
            bool on = CubsOut;
            for (int i = 0; i < cubs.Length; i++) cubs[i].State = on && i < cubCount ? 1 : 0;
            if (!on) { cubsTold = false; return; }
            float home = fox.A;
            for (int i = 0; i < cubCount; i++)
            {
                var c = cubs[i];
                var p = c.P;
                float before = c.X, lift = 0;
                c.Y = GardenArt.FoxAt.Y + 2;
                p.Turn = 0;
                p.Tilt = 0;
                p.Yawn = 0;
                if (fox.State == 1) { c.X = fox.X - 14 - i * 11; c.Side = 1; p.Sit = 0; }
                else if (fox.State == 3) { c.X = fox.X + 14 + i * 11; c.Side = -1; p.Sit = 0; }
                else if (cubStage == CubStage.Small)
                {
                    float bx = home + (i == 0 ? -20 : i == 1 ? 15 : 28), w = t * 0.5f + i * 2;
                    c.X = bx + Sin(w) * 5;
                    c.Side = Cos(w) >= 0 ? 1 : -1;
                    float roll = (t + i * 2.3f) % 7;
                    if (roll < 0.9f) { p.Turn = roll / 0.9f * Pi * 2 * c.Side; lift = Sin(roll / 0.9f * Pi) * 3; }
                    float rest = (t + i * 3.1f) % 9;
                    p.Sit = Toward(p.Sit, rest > 6 ? 1 : 0, dt * 4);
                    p.Yawn = rest > 7 && rest < 7.8f ? Sin((rest - 7) / 0.8f * Pi) : 0;
                }
                else if (cubStage == CubStage.Playful)
                {
                    p.Sit = 0;
                    if (i < 2)
                    {
                        float wc = home + 22, k = (t * 1.1f) % 2.6f;
                        bool mine = i == 0 ? k < 0.7f : k > 1.3f && k < 2.0f;
                        float u = mine ? Clamp((i == 0 ? k : k - 1.3f) / 0.7f, 0, 1) : 0;
                        c.Side = i == 0 ? 1 : -1;
                        c.X = wc + (i == 0 ? -1 : 1) * (10 - 8 * Sin(u * Pi));
                        lift = Sin(u * Pi) * 7;
                        if ((t * 0.37f + i) % 6 < 0.8f) { float r = (t * 0.37f + i) % 6 / 0.8f; p.Turn = r * Pi * 2 * c.Side; }
                    }
                    else
                    {
                        float k = (t + 0.6f) % 3;
                        float u = k < 0.75f ? k / 0.75f : 0;
                        c.Side = 1;
                        c.X = home - 33 + 9 * Sin(u * Pi * 0.5f);
                        lift = Sin(u * Pi) * 6;
                        p.Tilt = 0.35f * Sin(u * Pi);
                    }
                }
                else
                {
                    p.Sit = 0;
                    float bx = home + (i == 0 ? -30 : i == 1 ? 20 : 34), k = (t + i * 1.5f) % 4.5f;
                    c.Side = i == 0 ? -1 : 1;
                    float jump = k > 0.6f && k < 1.4f ? (k - 0.6f) / 0.8f : 0;
                    c.X = bx + c.Side * 6 * Smooth(0, 1, jump);
                    lift = Sin(jump * Pi) * 16;
                    p.Tilt = jump > 0 ? 0.7f * Smooth(0.45f, 1, jump) : k < 0.6f ? -0.15f : 0;
                    if (jump > 0.97f && c.K < t - 1)
                    {
                        c.K = t;
                        Emit(new Particle { Kind = Bit.Puff, X = c.X + c.Side * 4, Y = c.Y - 1, Vx = c.Side * 10, Vy = -5, Life = 0.5f, Size = 2 });
                    }
                }
                p.Run = Toward(p.Run, Math.Abs(c.X - before) > 0.05f && lift < 0.5f ? 1 : 0, dt * 8);
                p.Gait += Math.Abs(c.X - before) * 1.1f;
                p.Blink = (t + i * 1.3f) % 3.7f < 0.14f ? 1 : 0;
                c.A = lift;
            }
            if (fox.State == 2 && !cubsTold)
            {
                cubsTold = true;
                bool first = !state.SeenToday.Contains(Visitors.Cubs);
                Visit(Visitors.Cubs);
                var f = FriendNow(Visitors.Fox);
                string who = f != null && f.Named ? f.Name : "The fox";
                if (first && !Sleeping) Chat(Tone == Tone.Dry ? $"{who} brought the kids. Chaos." : $"{who} brought her cubs!", 3.2f);
            }
        }

        void DrawCubs(Graphics g, float t, float night)
        {
            if (!CubsOut) return;
            float age, scale, k = cubProgress;
            if (cubStage == CubStage.Small) { age = 0.1f + 0.3f * k; scale = 0.42f + 0.05f * k; }
            else if (cubStage == CubStage.Playful) { age = 0.45f + 0.35f * k; scale = 0.5f + 0.06f * k; }
            else { age = 0.85f + 0.15f * k; scale = 0.6f + 0.06f * k; }
            for (int i = 0; i < cubCount; i++)
                if (cubs[i].Active) WildArt.DrawCub(g, cubs[i].X, cubs[i].Y - cubs[i].A, t, cubs[i].Side, cubs[i].P, age, scale, night);
        }

        void DrawPondLife(Graphics g, float t, float night)
        {
            if (!Pond || pondStage == PondStage.None) return;
            if (pondStage == PondStage.Spawn) { WildArt.DrawSpawn(g, 59, 184.6f, t, pondProgress, night); return; }
            if (pondStage == PondStage.Tadpoles)
                for (int i = 0; i < 6; i++)
                {
                    float dir = i % 2 == 0 ? 1 : -1, ph = i * 1.7f + t * (0.22f + 0.04f * i) * dir;
                    float rx = Math.Min(19, 6 + 2.4f * i), ry = Math.Min(4, 1.4f + 0.45f * i);
                    float x = GardenArt.PondAt.X + Cos(ph) * rx, y = GardenArt.PondAt.Y + 0.4f + Sin(ph) * ry;
                    float angle = (float)Math.Atan2(Cos(ph) * ry * dir, -Sin(ph) * rx * dir);
                    WildArt.DrawTadpole(g, x, y, angle, t + i, 0.55f + 0.5f * pondProgress, Smooth(0.65f, 1, pondProgress), night);
                }
            else
                for (int i = 0; i < FrogletSpots.Length; i++)
                {
                    float k = (t + i * 2.2f) % 6.5f, hop = k < 0.5f ? Sin(k / 0.5f * Pi) * 3 : 0;
                    float croak = k > 3 && k < 3.6f ? Sin((k - 3) / 0.6f * Pi) * 0.6f : 0;
                    SpiritArt.DrawFrog(g, FrogletSpots[i].X, FrogletSpots[i].Y - hop, t, croak, 0, 0.32f);
                }
        }
    }
}
