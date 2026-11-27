using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    struct StarDay
    {
        public DateTime Day;
        public int Breaks;
    }

    sealed class StarShape
    {
        public string Name;
        public PointF[] Stars;
        public int[] Links;
    }

    static class Constellation
    {
        public const int Size = 12;
        public static readonly RectangleF Box = new RectangleF(134, 22, 64, 34);

        static PointF P(float x, float y) => new PointF(x, y);

        public static readonly StarShape[] Shapes =
        {
            new StarShape
            {
                Name = "The Little {0}",
                Stars = new[] { P(0.14f, 0.70f), P(0.22f, 0.45f), P(0.40f, 0.34f), P(0.62f, 0.36f), P(0.78f, 0.52f), P(0.74f, 0.78f), P(0.50f, 0.90f), P(0.28f, 0.88f), P(0.48f, 0.18f), P(0.56f, 0.04f), P(0.40f, 0.06f), P(0.72f, 0.10f) },
                Links = new[] { 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 0, 2, 8, 8, 9, 9, 10, 9, 11 },
            },
            new StarShape
            {
                Name = "The Paper Kite",
                Stars = new[] { P(0.10f, 0.40f), P(0.25f, 0.06f), P(0.40f, 0.40f), P(0.25f, 0.82f), P(0.25f, 0.40f), P(0.47f, 0.62f), P(0.56f, 0.48f), P(0.65f, 0.62f), P(0.74f, 0.48f), P(0.83f, 0.62f), P(0.92f, 0.48f), P(0.99f, 0.60f) },
                Links = new[] { 0, 1, 1, 2, 2, 3, 3, 0, 0, 4, 4, 2, 1, 4, 4, 3, 2, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11 },
            },
            new StarShape
            {
                Name = "The Fox's Tail",
                Stars = new[] { P(0.05f, 0.85f), P(0.15f, 0.70f), P(0.25f, 0.52f), P(0.37f, 0.36f), P(0.50f, 0.22f), P(0.63f, 0.14f), P(0.76f, 0.14f), P(0.88f, 0.22f), P(0.95f, 0.36f), P(0.90f, 0.50f), P(0.80f, 0.52f), P(0.72f, 0.44f) },
                Links = new[] { 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11 },
            },
            new StarShape
            {
                Name = "The Lantern",
                Stars = new[] { P(0.40f, 0.02f), P(0.20f, 0.28f), P(0.60f, 0.28f), P(0.28f, 0.40f), P(0.52f, 0.40f), P(0.52f, 0.70f), P(0.28f, 0.70f), P(0.40f, 0.55f), P(0.40f, 0.85f), P(0.25f, 0.98f), P(0.55f, 0.98f), P(0.85f, 0.20f) },
                Links = new[] { 0, 1, 0, 2, 1, 2, 1, 3, 2, 4, 3, 4, 4, 5, 5, 6, 6, 3, 6, 8, 5, 8, 8, 9, 8, 10 },
            },
        };

        public static List<DateTime> Days(IReadOnlyList<Session> sessions, Session current) =>
            sessions.Where(s => s != current).GroupBy(s => s.Start.Date).Where(g => g.Count() >= Garden.FlowerBreaks).Select(g => g.Key).OrderBy(d => d).ToList();

        public static List<StarDay> Stars(IReadOnlyList<Session> sessions, Session current) =>
            sessions.Where(s => s != current).GroupBy(s => s.Start.Date).Where(g => g.Count() >= Garden.FlowerBreaks).OrderBy(g => g.Key).Select(g => new StarDay { Day = g.Key, Breaks = g.Count() }).ToList();

        public static (int Shape, int Lit, int First) Progress(int count, DateTime? lastDay, DateTime today)
        {
            if (count <= 0) return (0, 0, 0);
            int done = count / Size, lit = count % Size;
            if (lit == 0 && lastDay >= today.Date.AddDays(-1)) return ((done - 1) % Shapes.Length, Size, (done - 1) * Size);
            return (done % Shapes.Length, lit, done * Size);
        }

        public static string Name(int shape, string stoneName) => string.Format(Shapes[shape % Shapes.Length].Name, string.IsNullOrEmpty(stoneName) ? "Pebble" : stoneName);

        public static PointF At(int shape, int index)
        {
            var p = Shapes[shape % Shapes.Length].Stars[index];
            return new PointF(Box.X + p.X * Box.Width, Box.Y + p.Y * Box.Height);
        }

        public static void Draw(Graphics g, int shape, int lit, float t, float alpha, float newest)
        {
            if (alpha < 0.02f) return;
            var s = Shapes[shape % Shapes.Length];
            using (var pen = new Pen(Alpha(Hex(0xBFD0F0), 0.4f * alpha), 0.6f))
                for (int i = 0; i + 1 < s.Links.Length; i += 2)
                    if (s.Links[i] < lit && s.Links[i + 1] < lit) g.DrawLine(pen, At(shape, s.Links[i]), At(shape, s.Links[i + 1]));
            for (int i = 0; i < s.Stars.Length; i++)
            {
                var p = At(shape, i);
                if (i >= lit)
                {
                    FillEllipse(g, Alpha(Hex(0xC9D3EA), 0.16f * alpha), p.X, p.Y, 0.55f, 0.55f);
                    continue;
                }
                float twinkle = 0.75f + 0.25f * Sin(t * 1.7f + i * 2.1f);
                float pulse = i == lit - 1 ? newest * (0.5f + 0.5f * Sin(t * 3)) : 0;
                FillRadial(g, p.X, p.Y, 5.5f + pulse * 2.5f, 5.5f + pulse * 2.5f, Alpha(Hex(0xFFF1C9), (0.5f + 0.3f * pulse) * alpha * twinkle), Alpha(Hex(0xFFF1C9), 0));
                FillEllipse(g, Alpha(Hex(0xFFF8E6), alpha * twinkle), p.X, p.Y, 1.45f + 0.3f * pulse, 1.45f + 0.3f * pulse);
            }
        }
    }
}
