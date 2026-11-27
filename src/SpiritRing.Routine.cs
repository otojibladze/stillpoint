using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        static readonly int[] StreakMarks = { 3, 5, 8, 12 };
        Beat beat;
        float beatT = -99, beatLen, routineNext = 6, viewT = -99, lastStepT = -99, dewNext;
        bool beatEndSaid;
        float startleT = -99, reunionT = -99, danceT = -99, partyT = -99, danceAt = -1, partyAt = -1, weedPopT = -99, heartNext;
        int shownWeeds = -1, partyBreaks, danceStreak;
        bool reunionDone, wowSaid, beatSpoke;
        readonly HashSet<string> beatsDone = new HashSet<string>();

        bool BeatActive(float t) => beat != Beat.None && t >= beatT && t < beatT + beatLen;
        bool ReactionActive(float t) => t - startleT < 1.1f || t - reunionT < 7.6f || t - danceT < 3.4f || t - partyT < 4.2f;
        bool Busy(float t) => BeatActive(t) || ReactionActive(t);

        Moment MomentNow() => new Moment
        {
            Hour = (float)Clock().TimeOfDay.TotalHours,
            Night = nightK,
            Clouds = cloudK,
            Glow = Sky.Glow(Clock(), state.Sun),
            Running = Running,
            Wet = rainK > 0.2f,
            Cold = season == Season.Winter || snowGround,
        };

        void StopBeat(float t)
        {
            if (BeatActive(t)) beatLen = Math.Min(beatLen, t - beatT + 0.4f);
        }

        bool TryBeat(float t, bool calm)
        {
            if (!calm || Busy(t) || t < routineNext) return false;
            var due = Routine.Due(MomentNow()).Where(b => b == Beat.Late || !Running || Drowsiness < 0.45f).ToList();
            if (due.Count == 0) return false;
            string day = Clock().ToString("yyyyMMdd");
            var fresh = due.FirstOrDefault(b => !beatsDone.Contains(day + b));
            Beat pick;
            if (fresh != Beat.None && t > viewT + 1.5f) pick = fresh;
            else if (t >= nextAct && rng.NextDouble() < 0.3) pick = due[rng.Next(due.Count)];
            else return false;
            bool first = beatsDone.Add(day + pick);
            beat = pick;
            beatT = t;
            beatLen = Routine.Length(pick);
            beatEndSaid = false;
            beatSpoke = first;
            wowSaid = false;
            if (first) Chat(Voice.Routine(pick, Tone), Math.Min(3.6f, beatLen - 0.4f), thought: pick == Beat.Count || pick == Beat.Nap || pick == Beat.Stars);
            routineNext = t + 45 + Rand * 30;
            nextAct = t + beatLen + 8 + Rand * 6;
            return true;
        }

        void StepRoutine(float t, float dt)
        {
            if (t - lastStepT > 3) viewT = t;
            lastStepT = t;
            if (shownWeeds < 0 || state.Weeds > shownWeeds) shownWeeds = state.Weeds;
            if (state.Weeds < shownWeeds && t > weedPopT && t > rakeT + 2.2f) PullWeed(t);
            if (!reunionDone && known && !Sleeping && t > viewT + 1.2f && !petting && t - stretchT > 3 && !Busy(t))
            {
                reunionDone = true;
                int days = lastEnd == default(DateTime) ? 0 : (Clock().Date - lastEnd.Date).Days;
                if (days >= 2) StartReunion(t, days);
                else if (shownWeeds > 0) Later(Voice.Weeds(shownWeeds), t + 0.5f, 3.6f);
            }
            if (danceAt > 0 && t > danceAt) { danceAt = -1; danceT = t; Chat(Voice.Streak(danceStreak, Tone), 3f); }
            if (partyAt > 0 && t > partyAt)
            {
                partyAt = -1;
                partyT = t;
                zoomT = t + 0.2f;
                Chat(Voice.Party(partyBreaks, Tone), 3.4f);
                for (int i = 0; i < 46; i++)
                    Emit(new Particle { Kind = Bit.Confetti, X = 40 + Rand * 220, Y = 18 + Rand * 50, Vx = (Rand - 0.5f) * 30, Vy = 30 + Rand * 30, Vr = (Rand - 0.5f) * 8, Size = 1.4f + Rand, Hue = rng.Next(5), Target = 196 + Rand * 34, Life = 7 + Rand * 2 });
            }
            if ((t - reunionT > 3.6f && t - reunionT < 6.4f || t - partyT < 3) && t > heartNext)
            {
                heartNext = t + 0.32f;
                Emit(new Particle { Kind = Bit.Heart, X = SX + (Rand - 0.5f) * 60, Y = 104, Vx = (Rand - 0.5f) * 14, Vy = -34, Vr = (Rand - 0.5f) * 0.6f, Life = 1.4f, Size = 4.5f + Rand * 2 });
            }
            if (t - danceT < 3.2f && t > noteT)
            {
                noteT = t + 0.45f;
                Emit(new Particle { Kind = Bit.Note, X = SX + (Rand < 0.5f ? -40 : 34), Y = 112, Vx = (Rand - 0.5f) * 20, Vy = -26, Vr = 0.3f, Life = 1.6f, Size = 11 });
            }
            if (!BeatActive(t)) return;
            float s = t - beatT;
            if (beat == Beat.Dew && s > 2.3f && s < 4.1f && t > dewNext)
            {
                dewNext = t + 0.07f;
                var from = Rand < 0.5f ? points.Tip : new PointF(SX + (Rand - 0.5f) * R * 1.2f, points.Head.Y - 2);
                float side = from.X < SX ? -1 : 1;
                Emit(new Particle { Kind = Bit.Dew, X = from.X, Y = from.Y, Vx = side * (25 + Rand * 45), Vy = -40 - Rand * 40, Size = 1.5f + Rand * 0.8f, Target = Ground + 2 + Rand * 14, Life = 2 });
            }
            if (beat == Beat.Shiver && s > 3.2f && t > dewNext)
            {
                dewNext = t + 10;
                Emit(new Particle { Kind = Bit.Vapor, X = SX + 4, Y = Ground - R * 0.6f, Vx = 8, Vy = -12, Life = 2, Size = 4 });
            }
            float endAt = beat == Beat.Count ? 6.6f : beat == Beat.Nap ? 9.55f : -1;
            if (endAt > 0 && s > endAt && !beatEndSaid)
            {
                beatEndSaid = true;
                if (beatSpoke) Chat(Voice.RoutineEnd(beat, Tone), 2.4f);
            }
        }

        void MeteorSeen(float t)
        {
            if (beat != Beat.Stars || !BeatActive(t) || wowSaid) return;
            wowSaid = true;
            startleT = t + 0.3f;
            Later(Tone == Tone.Dry ? "A shooting star. Fine. Wish made." : "Did you see that? A shooting star!", t + 0.3f, 2.8f);
        }

        void StartReunion(float t, int days)
        {
            StopBeat(t);
            reunionT = t;
            var (sulk, happy) = Voice.Reunion(Tone, days);
            Say(sulk, 3.2f);
            Later(happy, t + 3.5f, 3.2f);
            if (shownWeeds > 0) Later(Voice.Weeds(shownWeeds), t + 7.4f, 3.6f);
            nextAct = Math.Max(nextAct, t + 12);
        }

        void PullWeed(float t)
        {
            int i = shownWeeds - 1;
            shownWeeds--;
            weedPopT = t + 0.9f;
            if (i < 0 || i >= GardenArt.WeedSpots.Length) return;
            var at = GardenArt.WeedSpots[i];
            for (int k = 0; k < (snowGround || season == Season.Winter ? 0 : 3); k++)
                Emit(new Particle { Kind = Bit.Leaf, X = at.X, Y = at.Y - 4, Vx = (Rand - 0.5f) * 50, Vy = -50 - Rand * 30, Vr = 4, Life = 1.6f, Size = 3.4f, Target = at.Y + 2 });
            for (int k = 0; k < 5; k++)
                Emit(new Particle { Kind = Bit.Dirt, X = at.X, Y = at.Y - 1, Vx = (Rand - 0.5f) * 40, Vy = -35 - Rand * 25, Life = 1.2f, Size = 0.6f + Rand * 0.5f, Target = at.Y + Rand * 3, Night = nightK });
            Emit(new Particle { Kind = Bit.Puff, X = at.X, Y = at.Y - 2, Vx = 0, Vy = -6, Life = 0.7f, Size = 3 });
            Later(Voice.WeedPulled(shownWeeds), t + 0.4f, 2.4f);
        }

        void OnBreak(float t, RingState next)
        {
            if (!known) return;
            if (next.TotalBreaks > state.TotalBreaks && IsParty(next.TotalBreaks)) { partyBreaks = next.TotalBreaks; partyAt = t + 11.4f; }
            else if (next.Streak > state.Streak && Array.IndexOf(StreakMarks, next.Streak) >= 0)
            {
                danceAt = t + 11.4f;
                danceStreak = next.Streak;
            }
        }

        static bool IsParty(int breaks) => breaks == 10 || breaks == 25 || breaks == 50 || breaks == 100 || breaks == 200 || breaks == 300 || breaks == 500 || breaks == 750 || breaks >= 1000 && breaks % 500 == 0;

        void Blend(SpiritPose p, float k, float lookX, float lookY, float tilt, float dy, float sx, float sy, Eyes eyes, Mouth mouth)
        {
            if (k <= 0.001f) return;
            Morph(p, k, eyes, mouth);
            p.LookX += (lookX - p.LookX) * k;
            p.LookY += (lookY - p.LookY) * k;
            p.Tilt += tilt * k;
            p.Dy += dy * k;
            p.Sx += (sx - 1) * k;
            p.Sy += (sy - 1) * k;
        }

        void ApplyRoutine(SpiritPose p, float t)
        {
            if (Sleeping) return;
            if (BeatActive(t)) ApplyBeat(p, t - beatT, Window(0, beatLen, t - beatT, 0.45f));
            float st = t - startleT;
            if (st >= 0 && st < 1.1f)
            {
                float k = Window(0, 1.1f, st, 0.12f), h = Sin(Clamp(st / 0.55f, 0, 1) * Pi);
                Blend(p, k, 0, -0.2f, 0, -12 * h, 1 - 0.05f * h, 1 + 0.1f * h, Eyes.Open, Mouth.O);
                if (k > 0.5f) { p.EyeOpen = 1; p.EyeScale = 1 + 0.22f * k; p.Yawn = 0.3f; p.Brow = Brow.Curious; }
            }
            float rt = t - reunionT;
            if (rt >= 0 && rt < 7.6f)
            {
                Blend(p, Window(0, 3.4f, rt, 0.4f), 1, 0.25f, 0.07f, 0, 1, 1, Eyes.Open, Mouth.Flat);
                if (rt < 3.3f) { p.EyeOpen = Math.Min(p.EyeOpen, 0.6f); p.Brow = Brow.Worried; }
                float u = rt - 3.6f;
                float hop = u > 0 && u < 2.4f ? Math.Abs(Sin(u / 0.8f * Pi)) : 0;
                Blend(p, Window(3.6f, 7.6f, rt, 0.35f), 0, 0, 0, -10 * hop, 1 - 0.03f * hop, 1 + 0.05f * hop, Eyes.Happy, Mouth.Grin);
                if (rt > 3.6f) p.Blush = Math.Max(p.Blush, 0.7f);
            }
            float dt0 = t - danceT;
            if (dt0 >= 0 && dt0 < 3.4f)
                Blend(p, Window(0, 3.4f, dt0, 0.3f), 0, 0, 0.14f * Sin(dt0 * 7), -Math.Abs(Sin(dt0 * 7)) * 5, 1, 1, Eyes.Happy, Mouth.Grin);
            float pt = t - partyT;
            if (pt >= 0 && pt < 4.2f)
            {
                float hop = pt < 1.6f ? Math.Abs(Sin(pt / 0.8f * Pi)) : 0;
                Blend(p, Window(0, 4.2f, pt, 0.3f), 0, -0.4f, 0.06f * Sin(pt * 5), -12 * hop, 1, 1 + 0.04f * hop, Eyes.Happy, Mouth.Grin);
                p.Blush = Math.Max(p.Blush, 0.7f);
            }
        }

        void ApplyBeat(SpiritPose p, float s, float k)
        {
            switch (beat)
            {
                case Beat.Dew:
                    float up = Sin(Clamp(s / 2, 0, 1) * Pi);
                    if (s < 2.1f) { Blend(p, k, 0, -0.3f, 0, 0, 1 - 0.09f * up, 1 + 0.16f * up, Eyes.Squeeze, Mouth.O); if (k > 0.5f) p.Yawn = 0.6f * up; }
                    else
                    {
                        float w = Window(2.2f, 4.2f, s, 0.2f);
                        Blend(p, k, 0, 0, 0.1f * Sin(s * 28) * w, 0, 1 + 0.03f * Sin(s * 28 + 1) * w, 1, Eyes.Happy, Mouth.Smile);
                        p.Wind += 1.4f * Sin(s * 28) * w;
                    }
                    break;
                case Beat.Shiver:
                    Blend(p, k, 0, 0.2f, 0.04f * Sin(s * 45), 0, 1 + 0.02f * Sin(s * 52), 0.95f, Eyes.Squeeze, Mouth.Wavy);
                    if (k > 0.5f) p.Brow = Brow.Worried;
                    break;
                case Beat.Rain:
                    bool lick = s > 3 && s < 5.5f;
                    Blend(p, k, 0.2f, -1, 0, 0, 1, 1, lick ? Eyes.Happy : Eyes.Open, lick ? Mouth.Tongue : Mouth.O);
                    break;
                case Beat.Sunbathe:
                    Blend(p, k, -0.7f, -0.7f, -0.07f, 0, 1, 1 + 0.015f * Sin(s * 1.4f), Eyes.Happy, Mouth.Smile);
                    p.Blush += (0.75f - p.Blush) * k;
                    break;
                case Beat.Clouds:
                    Blend(p, k, 0.8f * Sin(s * 0.5f), -0.85f, 0, 0, 1, 1, Eyes.Open, s > 4 && s < 6 ? Mouth.O : Mouth.Dot);
                    if (k > 0.5f) p.Brow = Brow.Curious;
                    break;
                case Beat.Count:
                    bool counting = s < 6.6f;
                    float nod = counting && s % 0.5f < 0.15f ? 1.2f : 0;
                    float sigh = Window(6.6f, 8.5f, s, 0.3f);
                    Blend(p, k, counting ? -0.7f + 1.4f * Clamp(s / 6.5f, 0, 1) : 0, counting ? 0.9f : 0, 0, nod, 1, 1 - 0.04f * sigh, Eyes.Open, counting ? Mouth.Dot : Mouth.Flat);
                    if (k > 0.5f && counting) p.Brow = Brow.Curious;
                    break;
                case Beat.Nap:
                    float close = Smooth(0, 2, s) * (1 - Smooth(9.4f, 9.6f, s));
                    float jump = s > 9.5f && s < 10.1f ? Sin((s - 9.5f) / 0.6f * Pi) : 0;
                    if (s < 9.5f)
                    {
                        Blend(p, k, 0, 0.3f * close, 0.07f * close, 0, 1 + 0.02f * close, 1 - 0.04f * close, close > 0.5f ? Eyes.Closed : Eyes.Open, Mouth.Dot);
                        if (close < 0.5f) p.EyeOpen = Math.Min(p.EyeOpen, 1 - close);
                        p.Zzz = Math.Max(p.Zzz, Smooth(2, 3, s) * (1 - Smooth(9.2f, 9.5f, s)) * k);
                    }
                    else
                    {
                        Blend(p, k, 0, -0.1f, 0, -8 * jump, 1, 1 + 0.06f * jump, Eyes.Open, Mouth.O);
                        if (k > 0.5f) { p.EyeOpen = 1; p.EyeScale = 1 + 0.15f * jump; p.Brow = Brow.Curious; }
                    }
                    break;
                case Beat.Sunset:
                    bool dreamy = s > 4 && s < 7;
                    Blend(p, k, -0.35f, -0.2f, 0.03f * Sin(s * 0.8f), 0, 1, 1 - 0.02f * Window(4, 7, s, 0.5f), dreamy ? Eyes.Happy : Eyes.Open, Mouth.Smile);
                    p.Blush += (0.65f - p.Blush) * k;
                    break;
                case Beat.Stars:
                    Blend(p, k, 0.7f * Sin(s * 0.35f), -1, 0, s % 0.9f < 0.2f ? 0.8f : 0, 1, 1, Eyes.Open, Mouth.O);
                    if (k > 0.5f) { p.Yawn = 0.22f; p.EyeScale = 1.08f; }
                    break;
                case Beat.Bedtime:
                case Beat.Late:
                    float yawn = Window(0.4f, 3, s, 0.6f);
                    Blend(p, k, 0, 0.3f, -0.05f * yawn, 0, 1, 1 + 0.05f * yawn, yawn > 0.3f ? Eyes.Closed : Eyes.Open, yawn > 0.3f ? Mouth.O : Mouth.Flat);
                    if (k > 0.5f) { p.Yawn = Math.Max(p.Yawn, yawn); p.Brow = Brow.Sleepy; if (yawn < 0.3f) p.EyeOpen = Math.Min(p.EyeOpen, 0.5f); }
                    break;
            }
        }
    }
}
