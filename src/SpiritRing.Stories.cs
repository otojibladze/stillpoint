using System;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        readonly Critter story = new Critter(), story2 = new Critter();
        string storyId;
        float storyNext = 40, curlT = -99;
        int storyTold;

        bool StoryBusy => story.Active;

        bool Once(int bit)
        {
            if ((storyTold & bit) != 0) return false;
            storyTold |= bit;
            return true;
        }

        void StepStories(float t, float dt, bool calm)
        {
            if (!story.Active && t > storyNext)
            {
                storyNext = t + 60;
                bool night = nightK > 0.7f;
                bool free = !AnyVisitor && !bird.Active && !butterfly.Active && !dragonfly.Active && !frog.Active && !snail.Active && !cat.Active && rainK < 0.3f && !Busy(t);
                bool awake = calm && !Sleeping && Daylight && !(Running && Drowsiness > 0.45f);
                if (free && (night || awake) && !state.SeenToday.Any(Stories.Is) && rng.NextDouble() < (state.StoriesSeen == 0 ? 0.15 : 0.05))
                {
                    var fits = Stories.All.Where(s => s.Night == night && Fits(s.Id)).ToList();
                    if (fits.Count > 0) StartStory(fits[rng.Next(fits.Count)].Id, t);
                }
            }
            if (!story.Active) return;
            float s = t - story.T0;
            switch (storyId)
            {
                case Visitors.Balloon: StepBalloon(t, s); break;
                case Visitors.Plane: StepPlane(t, s); break;
                case Visitors.Traveler: StepTraveler(t, s); break;
                case Visitors.Duckling: StepDucks(t, s); break;
                case Visitors.Hedgehog: StepHedgehog(t, dt, s); break;
            }
        }

        bool StartStory(string id, float t)
        {
            if (!Stories.Is(id)) return false;
            storyId = id;
            storyTold = 0;
            story.State = 1;
            story.T0 = t;
            story.K = 0;
            story.P.Gait = story.P.Run = 0;
            story2.State = 0;
            story2.P.Gait = story2.P.Run = 0;
            curlT = -99;
            StopBeat(t);
            return true;
        }

        void EndStory(float t)
        {
            story.State = story2.State = 0;
            storyNext = t + 90;
        }

        PointF BalloonAnchor => new PointF(points.Tip.X + 34, points.Tip.Y - 15);

        void StepBalloon(float t, float s)
        {
            var c = story;
            var anchor = BalloonAnchor;
            if (s < 7)
            {
                float u = s / 7, e = Smooth(0, 1, u);
                c.X = -25 + (anchor.X + 25) * e;
                c.Y = 46 + (anchor.Y - 46) * e + Sin(u * 9) * 6 * (1 - u);
                return;
            }
            if (Once(1))
            {
                Visit(Visitors.Balloon);
                Chat(Tone == Tone.Dry ? "A balloon. I'm a party now." : Tone == Tone.Worry ? "A balloon! Is someone missing it?" : "A balloon! I'm keeping it.", 3.6f);
            }
            if (Sleeping && s < 40) c.T0 = t - 40;
            if (s < 40)
            {
                c.X = anchor.X + wind * 3 + Sin(t * 1.3f) * 2;
                c.Y = anchor.Y + Sin(t * 1.7f) * 1.5f;
                c.From = new PointF(c.X, c.Y);
                return;
            }
            if (Once(2)) Chat(Tone == Tone.Dry ? "And there it goes. Story of my life." : "Bye, balloon!", 2.6f);
            float v = Clamp((s - 40) / 8, 0, 1);
            c.X = c.From.X + 70 * v * v + Sin(v * 6) * 4;
            c.Y = c.From.Y - (c.From.Y + 40) * Smooth(0, 1, v);
            if (v >= 1) EndStory(t);
        }

        float BalloonHeld(float t)
        {
            if (storyId != Visitors.Balloon || !story.Active) return 0;
            float s = t - story.T0;
            return Smooth(6.6f, 7.4f, s) * (1 - Smooth(40, 40.6f, s));
        }

        void StepPlane(float t, float s)
        {
            var c = story;
            var rest = StoryArt.PlaneRest;
            if (s < 5)
            {
                float u = s / 5;
                c.X = 330 - (330 - rest.X - 4) * u;
                c.Y = 40 + (rest.Y - 40) * Smooth(0, 1, u) + Sin(u * 8) * 6 * (1 - u);
                c.K = 0.15f + 0.25f * Cos(u * 8) * (1 - u);
                return;
            }
            if (Once(1))
            {
                for (int i = 0; i < 4; i++) Emit(new Particle { Kind = Bit.Puff, X = rest.X + (Rand - 0.5f) * 10, Y = rest.Y, Vx = (Rand - 0.5f) * 30, Vy = -6, Life = 0.6f, Size = 2 + Rand });
                Visit(Visitors.Plane);
                Chat(Tone == Tone.Dry ? "Paper plane. It says: take a break. Even the mail nags." : Tone == Tone.Worry ? "A paper plane. It says: take a break. Listen to it." : "A paper plane! It says: take a break.", 3.8f);
            }
            if (s < 30)
            {
                c.X = rest.X - 4 * Smooth(5, 5.4f, s);
                c.Y = rest.Y;
                c.K = 0.06f;
                return;
            }
            float v = Clamp((s - 30) / 4, 0, 1);
            c.X = rest.X - 4 - 150 * v;
            c.Y = rest.Y - 150 * Smooth(0, 1, v) + Sin(v * 7) * 4;
            c.K = -0.3f * Smooth(0, 0.3f, v);
            if (v >= 1) EndStory(t);
        }

        void StepTraveler(float t, float s)
        {
            var c = story;
            var rest = StoryArt.TravelerRest;
            if (s < 5)
            {
                float u = Clamp(s / 4.8f, 0, 1);
                c.X = -20 + (rest.X + 20) * u;
                c.Y = rest.Y;
                c.A = s < 4.8f ? Math.Abs(Sin(s / 0.55f * Pi)) : 0;
                c.Side = 1;
                return;
            }
            if (Once(1))
            {
                Visit(Visitors.Traveler);
                Chat(Tone == Tone.Dry ? "A traveling pebble. Going to the sea. Show-off." : "A traveling pebble! It's on its way to the sea.", 3.6f);
            }
            if (s < 33)
            {
                c.X = rest.X;
                c.A = 0;
                if (s > 6.5f && s < 7.4f) c.A = Math.Abs(Sin((s - 6.5f) / 0.45f * Pi)) * 0.5f;
                return;
            }
            if (Once(2)) Chat("Safe travels, little one!", 2.6f);
            float v = Clamp((s - 33) / 4.6f, 0, 1);
            c.Side = -1;
            c.X = rest.X - (rest.X + 25) * v;
            c.A = v < 1 ? Math.Abs(Sin((s - 33) / 0.55f * Pi)) : 0;
            if (v >= 1) EndStory(t);
        }

        SpiritPose TravelerPose(float t, float night)
        {
            var c = story;
            float s = t - c.T0;
            var p = StoryArt.TravelerPose(night);
            p.Dy = -6 * c.A;
            p.Sy = 1 + 0.06f * c.A;
            p.Sx = 1 - 0.03f * c.A;
            p.LookX = c.Side;
            p.LookY = 0;
            bool napping = s > 12 && s < 30;
            if (napping) { p.Eyes = Eyes.Closed; p.Zzz = Smooth(13, 14, s) * (1 - Smooth(29, 30, s)); p.Mouth = Mouth.Dot; }
            else if (s > 5 && s < 9) p.Mouth = Mouth.Grin;
            else
            {
                float blink = (t + 0.7f) % 3.3f;
                if (blink < 0.16f) p.EyeOpen = Math.Abs(blink - 0.08f) / 0.08f;
            }
            return p;
        }

        void StepDucks(float t, float s)
        {
            var kid = story;
            var mom = story2;
            kid.Y = StoryArt.DuckY + 1;
            mom.Y = StoryArt.DuckY;
            float before = kid.X, momBefore = mom.X;
            if (s < 5) { kid.X = 320 - 110 * Smooth(0, 1, s / 5); kid.Side = -1; }
            else if (s < 12)
            {
                kid.X = 210;
                kid.Side = s < 7.5f || s > 9.5f ? -1 : 1;
                if (Once(1)) Chat(Tone == Tone.Dry ? "A duckling. Alone. That's not right." : "Peep? Are you lost, little one?", 3.2f, thought: false);
            }
            else if (s < 15.5f) { kid.X = 210 - 120 * Smooth(0, 1, (s - 12) / 3.5f); kid.Side = -1; }
            else if (s < 17) kid.X = 90;
            else { kid.X = 90 - 116 * Clamp((s - 17) / 7, 0, 1); kid.Side = -1; }
            if (s >= 8)
            {
                mom.State = 1;
                if (s < 12) { mom.X = -30 + 100 * Smooth(0, 1, (s - 8) / 4); mom.Side = 1; }
                else if (s < 17) mom.X = 70;
                else { mom.X = 70 - 110 * Clamp((s - 17) / 7, 0, 1); mom.Side = -1; }
                if (s >= 12 && Once(2))
                {
                    Visit(Visitors.Duckling);
                    Chat(Tone == Tone.Dry ? "Oh good, Mom's here. Crisis over." : "There's your mom!", 2.8f);
                }
                if (s >= 15.6f && Once(4))
                    for (int i = 0; i < 3; i++) Emit(new Particle { Kind = Bit.Heart, X = 80 + (Rand - 0.5f) * 10, Y = StoryArt.DuckY - 16, Vx = (Rand - 0.5f) * 10, Vy = -20, Life = 1.6f, Size = 3.4f });
            }
            kid.P.Gait += Math.Abs(kid.X - before) * 0.9f;
            mom.P.Gait += Math.Abs(mom.X - momBefore) * 0.6f;
            kid.P.Run += ((Math.Abs(kid.X - before) > 0.01f ? 1 : 0) - kid.P.Run) * 0.2f;
            mom.P.Run += ((Math.Abs(mom.X - momBefore) > 0.01f ? 1 : 0) - mom.P.Run) * 0.2f;
            float peepAt = s < 12 ? (s - 5.5f) % 1.35f : 99;
            kid.K = s > 5.5f && peepAt >= 0 && peepAt < 0.3f ? Sin(peepAt / 0.3f * Pi) : 0;
            if (kid.K > 0.95f && t > kid.C)
            {
                kid.C = t + 0.5f;
                Emit(new Particle { Kind = Bit.Note, X = kid.X - 4 * kid.Side, Y = kid.Y - 12, Vx = -kid.Side * 8, Vy = -18, Vr = 0.3f, Life = 0.9f, Size = 5 });
            }
            mom.K = s > 11.2f && s < 11.8f || s > 15.8f && s < 16.4f ? 1 : 0;
            if (mom.K > 0 && t > mom.C)
            {
                mom.C = t + 0.7f;
                Emit(new Particle { Kind = Bit.Note, X = mom.X + 10 * mom.Side, Y = mom.Y - 20, Vx = mom.Side * 8, Vy = -20, Vr = 0.3f, Life = 1.1f, Size = 7 });
            }
            if (s > 24) EndStory(t);
        }

        void StepHedgehog(float t, float dt, float s)
        {
            var c = story;
            bool curled = t - curlT < 3;
            c.K += ((curled ? 1 : 0) - c.K) * Math.Min(1, dt * (curled ? 9 : 2.5f));
            if (c.K > 0.05f) { c.T0 += dt; s = t - c.T0; }
            if (pointerIn && t - pointerT < 1 && Dist(pointer, c.X, c.Y - 5) < 16) curlT = t;
            float before = c.X;
            c.Y = StoryArt.HedgehogY;
            c.Side = -1;
            if (s < 14) c.X = 320 - 150 * (s / 14);
            else if (s < 18) c.X = 170;
            else c.X = 170 - 200 * Clamp((s - 18) / 16, 0, 1);
            c.P.Gait += Math.Abs(c.X - before) * 1.6f;
            c.P.Run += ((Math.Abs(c.X - before) > 0.01f ? 1 : 0) - c.P.Run) * 0.2f;
            c.A = s > 14 && s < 18 ? Sin(t * 18) : 0;
            if (s > 2 && Once(1) && !Sleeping) Chat(Tone == Tone.Dry ? "A hedgehog. Snack patrol. Respect." : "A hedgehog! Shh, it's looking for snacks.", 3.2f);
            if (s > 14 && Once(2)) Visit(Visitors.Hedgehog);
            if (s > 34) EndStory(t);
        }

        void PokeStory(float t)
        {
            if (storyId == Visitors.Hedgehog && story.Active) curlT = t;
        }

        bool StoryFocus(out PointF at)
        {
            at = PointF.Empty;
            if (!story.Active) return false;
            switch (storyId)
            {
                case Visitors.Traveler: at = new PointF(story.X, story.Y - 12); break;
                case Visitors.Duckling: at = new PointF(story.X, story.Y - 6); break;
                case Visitors.Hedgehog: at = new PointF(story.X, story.Y - 5); break;
                default: at = new PointF(story.X, story.Y); break;
            }
            return true;
        }

        void ApplyStory(SpiritPose p, float t)
        {
            if (!story.Active || Sleeping) return;
            float held = BalloonHeld(t);
            p.Wind += 0.8f * held;
            if (held > 0.5f && p.Mouth == Mouth.Dot) p.Mouth = Mouth.Smile;
            float s = t - story.T0;
            if (storyId == Visitors.Duckling && s > 5 && s < 12) p.Brow = Brow.Worried;
            else if (p.Brow == Brow.None) p.Brow = Brow.Curious;
        }

        void DrawStories(Graphics g, float t, float night, bool air)
        {
            if (!story.Active) return;
            var c = story;
            switch (storyId)
            {
                case Visitors.Balloon:
                    if (!air) return;
                    float held = BalloonHeld(t);
                    var knot = new PointF(c.X, c.Y + 10.5f);
                    var hang = new PointF(c.X - 2 - wind * 2, c.Y + 32);
                    var end = held > 0.5f ? points.Tip : hang;
                    StoryArt.DrawCord(g, end, knot, held > 0.5f ? 2 + wind * 2 : 1.5f, night);
                    StoryArt.DrawBalloon(g, c.X, c.Y, Sin(t * 1.3f) * 0.08f + wind * 0.05f, night);
                    break;
                case Visitors.Plane:
                    if (air) StoryArt.DrawPlane(g, c.X, c.Y, c.K, -1, night);
                    break;
                case Visitors.Traveler:
                    if (!air) StoryArt.DrawTraveler(g, c.X, c.Y, TravelerPose(t, night), t, night);
                    break;
                case Visitors.Duckling:
                    if (air) return;
                    if (story2.State != 0) StoryArt.DrawDuck(g, story2.X, story2.Y, story2.Side, story2.P.Gait * 0.5f, story2.P.Run, night, false, story2.K);
                    StoryArt.DrawDuck(g, c.X, c.Y, c.Side, c.P.Gait * 0.7f, c.P.Run, night, true, c.K);
                    break;
                case Visitors.Hedgehog:
                    if (!air) StoryArt.DrawHedgehog(g, c.X, c.Y, c.Side, c.P.Gait * 0.5f, c.P.Run, c.K, c.A, night);
                    break;
            }
        }
    }
}
