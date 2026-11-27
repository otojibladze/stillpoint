using System;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        const float DreamSeconds = 11;
        static readonly PointF DreamAt = new PointF(92, 60);
        float dreamT = -99, dreamNext = 6;
        bool dreamTold;

        public event Action<DateTime> DreamShared;

        float DreamK(float t)
        {
            float s = t - dreamT;
            return s < 0 || s > DreamSeconds ? 0 : Smooth(0, 0.8f, s) * (1 - Smooth(DreamSeconds - 0.8f, DreamSeconds, s));
        }

        void StepDreams(float t)
        {
            var d = state.Dream;
            bool asleep = Sleeping && nightK > 0.7f && d != null && !plain;
            if (asleep && t > dreamNext && t - dreamT > DreamSeconds)
            {
                dreamT = t;
                dreamNext = t + DreamSeconds + 30 + Rand * 25;
            }
            if (!asleep)
            {
                if (t - dreamT < DreamSeconds - 0.8f) dreamT = t - DreamSeconds + 0.8f;
                dreamNext = Math.Max(dreamNext, t + 5);
            }
            float hour = (float)Clock().TimeOfDay.TotalHours;
            if (!dreamTold && d != null && !state.DreamTold && !Sleeping && hour >= 5 && hour < 12 && t > viewT + 3 && known && state.Chat != Chattiness.Quiet)
            {
                dreamTold = true;
                Later(Tone == Tone.Dry ? "Weird dream last night. " + d.Line : d.Worried ? "Funny dream last night. " + d.Line : d.Line, t + 1.5f, 4.8f);
                DreamShared?.Invoke(d.Night);
            }
        }

        void ApplyDreamFace(SpiritPose p, float t)
        {
            float k = DreamK(t);
            if (k < 0.3f || state.Dream == null) return;
            if (state.Dream.Worried)
            {
                p.Mouth = Mouth.Wavy;
                p.Brow = Brow.Worried;
            }
            else
            {
                p.Mouth = Mouth.Smile;
                p.Blush = Math.Max(p.Blush, 0.5f);
            }
        }

        void DrawDream(Graphics g, float t, float night)
        {
            float k = DreamK(t);
            var d = state.Dream;
            if (k <= 0 || d == null) return;
            var from = new PointF(points.Head.X - 2, points.Head.Y - 6);
            TouchArt.DrawBubble(g, DreamAt.X, DreamAt.Y, k, night, from);
            float s = t - dreamT, end = 1 - Smooth(DreamSeconds - 1.1f, DreamSeconds - 0.7f, s);
            float a = Smooth(0.6f, 1.1f, s) * end, b = Smooth(1.5f, 2f, s) * end;
            if (d.Motifs.Length > 0) TouchArt.DrawDreamIcon(g, d.Motifs[0], DreamAt.X - 12, DreamAt.Y + 2, t, a);
            if (d.Motifs.Length > 1) TouchArt.DrawDreamIcon(g, d.Motifs[1], DreamAt.X + 13, DreamAt.Y + 2, t + 1.3f, b);
            for (int i = 0; i < 3; i++)
            {
                float tw = Math.Max(0, Sin(t * 2.2f + i * 2.1f)) * k;
                FillEllipse(g, Alpha(Hex(0xE8C46A), 0.8f * tw), DreamAt.X - 20 + i * 18, DreamAt.Y - 13 + (i % 2) * 22, 0.9f, 0.9f);
            }
        }

        void DrawConstellation(Graphics g, float t, float night)
        {
            var stars = state.Stars;
            if (stars == null || stars.Count == 0) return;
            var (shape, lit, _) = Constellation.Progress(stars.Count, stars[stars.Count - 1].Day, Clock());
            float alpha = Smooth(0.3f, 0.8f, night) * (1 - cloudK * 0.85f) * (1 - fogK);
            float newest = stars[stars.Count - 1].Day == Clock().Date ? 1 : 0;
            Constellation.Draw(g, shape, lit, t, alpha, newest);
        }

        void StepConstellation(float t)
        {
            var stars = state.Stars;
            if (stars == null || stars.Count == 0 || nightK < 0.7f || cloudK > 0.7f || Sleeping || t < viewT + 4) return;
            var today = Clock().Date;
            var last = stars[stars.Count - 1].Day;
            if (last < today.AddDays(-1)) return;
            var (shape, lit, _) = Constellation.Progress(stars.Count, last, Clock());
            string night = SkyEvents.NightOf(Clock()).ToString("yyyyMMdd");
            if (lit == Constellation.Size && Tell("constellation" + night, null))
            {
                Chat($"Our constellation is complete! I'm calling it {Constellation.Name(shape, state.Name)}.", 4.4f);
                zoomT = t + 0.5f;
            }
            else if (lit < Constellation.Size && Tell("star" + night, null))
                Chat(stars.Count == 1 ? "Look up. Our first star is shining." : "Look up. A new star tonight.", 3.2f);
        }

        string StarTip(PointF q)
        {
            var stars = state.Stars;
            if (stars == null || stars.Count == 0 || nightK < 0.4f || cloudK > 0.7f) return null;
            var (shape, lit, first) = Constellation.Progress(stars.Count, stars[stars.Count - 1].Day, Clock());
            for (int i = 0; i < lit && first + i < stars.Count; i++)
            {
                var p = Constellation.At(shape, i);
                if (Dist(q, p.X, p.Y) > 5) continue;
                var s = stars[first + i];
                string text = $"Star of {s.Day:d MMM}: {s.Breaks} breaks";
                return lit == Constellation.Size ? Constellation.Name(shape, state.Name) + "\n" + text : text + $"\n{lit} of {Constellation.Size} stars";
            }
            return null;
        }
    }
}
