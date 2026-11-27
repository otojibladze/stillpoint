using System;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        string recall;
        float recallAt, eyeRollT = -99, ponderT = -99, worryT = -99, begT = -99, begNext = 12, fledgeT = -99, nestCheckT = -99, nestProgress;
        NestStage nestStage;
        bool fledgeShown;
        DateTime giftDay;
        bool GiftLeft => giftDay == Clock().Date;

        Tone Tone => Character.ToneOf(state.Temper);
        float HopScale => Nest.Fragile(nestStage) ? 0.3f : 1;
        bool NestShown => nestStage != NestStage.None;
        bool NestBusy => nestStage >= NestStage.Building && nestStage <= NestStage.Fledglings;
        bool BirdInNest => NestShown && bird.State == 2 && bird.D == 0;
        float LadyTop => NestShown ? 1.2f * Pi : GardenArt.LadybugSeat;
        float SongEvery => TrustOf(Visitors.Bird) >= Trust.Friendly ? 1.3f : 2.2f;
        bool FledgeDue => nestStage == NestStage.Empty && nestProgress < 0.02f && !state.Fledged && !fledgeShown;
        int Flown(float t) => fledgeT < 0 ? 0 : Math.Min(3, (int)((t - fledgeT) / 1.8f));

        public void Recall(string text)
        {
            recall = text;
            recallAt = Now + 5;
        }

        bool SpeakMemory(float t, bool calm)
        {
            if (recall == null || t < recallAt || !calm || Sleeping) return false;
            Chat(recall, 4.8f, thought: true);
            recall = null;
            nextAct = t + 9;
            return true;
        }

        Friend FriendNow(string species)
        {
            state.Friends.TryGetValue(species, out var known);
            if (state.SeenToday.Contains(species)) return known;
            int visits = (known?.Visits ?? 0) + 1;
            return new Friend { Species = species, Name = known?.Name ?? "", Visits = visits, First = known?.First ?? Clock().Date, Trust = Friends.TrustFor(visits) };
        }

        Trust TrustOf(string species) => FriendNow(species)?.Trust ?? Trust.Wary;

        bool Known(string species) => FriendNow(species)?.Named == true && !string.IsNullOrEmpty(FriendNow(species).Name);

        void OnVisit(string visitor)
        {
            if (Array.IndexOf(Friends.Species, visitor) < 0 || state.SeenToday.Contains(visitor)) return;
            var f = FriendNow(visitor);
            if (string.IsNullOrEmpty(f.Name)) return;
            string line = Friends.Milestone(f, Tone);
            if (line != null) Later(line, Now + 3.2f, 3.8f);
            else if (f.Named) Later(Tone == Tone.Dry ? $"Oh. {f.Name} again." : Tone == Tone.Worry ? $"{f.Name}! You came back." : $"{f.Name}'s back!", Now + 1.6f, 2.6f);
        }

        bool TemperAct(float t)
        {
            double r = rng.NextDouble();
            switch (state.Temper)
            {
                case Temper.Sunny: if (r < 0.35) { humT = t; return true; } break;
                case Temper.Sarcastic: if (r < 0.32) { eyeRollT = t; return true; } break;
                case Temper.Worried: if (r < 0.3) { worryT = t; return true; } break;
                case Temper.Thoughtful: if (r < 0.25) { ponderT = t; return true; } break;
            }
            return false;
        }

        void ApplyTemper(SpiritPose p, float t)
        {
            if (Sleeping) return;
            switch (state.Temper)
            {
                case Temper.Sunny:
                    if (p.Mouth == Mouth.Dot) p.Mouth = Mouth.Smile;
                    p.Blush = Math.Max(p.Blush, 0.5f);
                    break;
                case Temper.Sarcastic:
                    if (p.Eyes == Eyes.Open) p.EyeOpen = Math.Min(p.EyeOpen, 0.8f);
                    break;
                case Temper.Worried:
                    if (Running && p.Brow == Brow.None) p.Brow = Brow.Worried;
                    break;
            }
            float roll = Window(0, 1.4f, t - eyeRollT, 0.3f);
            if (roll > 0)
            {
                float a = Clamp((t - eyeRollT) / 1.4f, 0, 1) * Pi;
                p.LookX += (-Cos(a) - p.LookX) * roll;
                p.LookY += (-0.95f * Sin(a) - p.LookY) * roll;
                if (p.Eyes == Eyes.Open) p.EyeOpen = Math.Min(p.EyeOpen, 1 - 0.3f * roll);
                if (roll > 0.5f) { p.Brow = Brow.Curious; p.Mouth = Mouth.Flat; }
            }
            float ponder = Window(0, 2.8f, t - ponderT, 0.45f);
            if (ponder > 0)
            {
                p.LookX += (0.7f - p.LookX) * ponder;
                p.LookY += (-0.85f - p.LookY) * ponder;
                if (ponder > 0.5f) p.Mouth = Mouth.Flat;
            }
            float worry = Window(0, 2.4f, t - worryT, 0.4f);
            if (worry > 0)
            {
                p.LookY += (0.9f - p.LookY) * worry;
                p.LookX *= 1 - worry;
                if (worry > 0.5f) { p.Brow = Brow.Worried; p.Mouth = Mouth.Wavy; }
            }
        }

        void StepLife(float t, float dt)
        {
            if (t - nestCheckT > 5)
            {
                nestCheckT = t;
                (nestStage, nestProgress) = Nest.On(Clock(), state.Southern);
                if (season == Season.Winter) nestStage = NestStage.None;
            }
            if (FledgeDue && fledgeT < 0 && t > 10 && nightK < 0.4f && rainK < 0.2f && !Running && !Sleeping && t - stretchT > 12)
            {
                fledgeT = t;
                Chat("Look! The chicks are leaving the nest!", 3.2f);
            }
            if (fledgeT > 0 && !fledgeShown && t - fledgeT > 6.6f)
            {
                fledgeShown = true;
                Visit(Visitors.Fledged);
                Later("They flew! Our chicks can fly!", t + 0.3f, 3.6f);
                zoomT = t + 0.4f;
            }
            if (t > begNext && (nestStage == NestStage.Chicks || nestStage == NestStage.Fledglings))
            {
                begT = t;
                begNext = t + 7 + Rand * 9;
                Emit(new Particle { Kind = Bit.Note, X = points.Head.X + 6, Y = points.Head.Y - 14, Vx = 8, Vy = -18, Vr = 0.3f, Life = 1f, Size = 6 });
            }
        }

        float Beg(float t) => Math.Max(BirdInNest ? 1 : 0, Math.Max(Window(0, 1.6f, t - begT, 0.25f), petK));

        bool NestHush()
        {
            if (!Nest.Fragile(nestStage)) return false;
            Say(nestStage == NestStage.Eggs ? "Shh! The eggs!" : "Shh, the chicks are asleep.", 1.8f);
            return true;
        }

        string NestLine()
        {
            var f = FriendNow(Visitors.Bird);
            string who = f != null && f.Named ? f.Name : "The bird";
            switch (nestStage)
            {
                case NestStage.Building: return $"{who} brought another twig. Our nest is growing!";
                case NestStage.Eggs: return $"{who} is back to keep the eggs warm.";
                case NestStage.Chicks: return "Dinner time for the chicks!";
                default: return "Flying lessons today.";
            }
        }

        void DrawLife(Graphics g, float t, float night)
        {
            if ((GiftLeft || state.DugToday && TrustOf(Visitors.Squirrel) >= Trust.Friendly && !squirrel.Active) && season != Season.Winter)
                CritterArt.DrawAcorn(g, GardenArt.GiftAt.X, GardenArt.GiftAt.Y, 1, night);
            if (!NestShown) return;
            var at = GardenArt.StoneSurface(Shape, GardenArt.NestSeat, 0.95f);
            float angle = SurfaceAngle(GardenArt.NestSeat);
            var stage = nestStage;
            float progress = nestProgress;
            int chicks = 3;
            bool farewell = FledgeDue || fledgeT > 0 && !fledgeShown;
            if (farewell)
            {
                stage = NestStage.Fledglings;
                progress = 1;
                chicks = 3 - Flown(t);
            }
            float flap = stage == NestStage.Fledglings ? Math.Max(farewell ? 1 : 0, Window(0, 1.6f, t % 9, 0.3f) * Smooth(0.3f, 1, progress)) : 0;
            Action inside = null;
            if (BirdInNest)
                inside = () =>
                {
                    float since = t - bird.T0;
                    float feed = stage == NestStage.Chicks || stage == NestStage.Fledglings ? Math.Max(0, Sin(since * 3.2f)) : 0;
                    float sit = stage == NestStage.Eggs ? 3.5f : 0;
                    var st = g.Save();
                    g.ScaleTransform(1 / 1.35f, 1 / 1.35f);
                    g.TranslateTransform(2, -9 + sit + feed * 1.5f);
                    g.RotateTransform(feed * 18);
                    SpiritArt.DrawBird(g, 0, 0, t, -bird.Side, false);
                    g.Restore(st);
                };
            CritterArt.DrawNest(g, at.X, at.Y, angle, stage, progress, t, Beg(t), flap, chicks, night, inside);
            if (fledgeT > 0)
                for (int i = 0; i < 3; i++)
                {
                    float u = (t - fledgeT - i * 1.8f) / 2.8f;
                    if (u <= 0 || u >= 1) continue;
                    float side = i == 1 ? 1 : -1;
                    float x = at.X + (i - 1) * 5 + side * 120 * u * u, y = at.Y - 10 - 100 * u + Sin(u * 9) * 4;
                    var st = g.Save();
                    g.TranslateTransform(x, y);
                    g.ScaleTransform(0.75f, 0.75f);
                    SpiritArt.DrawBird(g, 0, 0, t + i, side, true);
                    g.Restore(st);
                }
        }
    }
}
