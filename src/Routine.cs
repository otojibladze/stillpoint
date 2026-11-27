using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    enum Beat { None, Dew, Shiver, Rain, Sunbathe, Clouds, Count, Nap, Sunset, Stars, Bedtime, Late }

    struct Moment
    {
        public float Hour, Night, Clouds, Glow;
        public bool Running, Wet, Cold;
    }

    static class Routine
    {
        static readonly (Beat Beat, float From, float To, float Length, Func<Moment, bool> Fits)[] Plan =
        {
            (Beat.Dew, 5.5f, 10, 5.2f, m => !m.Cold && !m.Wet && m.Night < 0.5f),
            (Beat.Shiver, 5.5f, 10, 4f, m => m.Cold && !m.Wet && m.Night < 0.6f),
            (Beat.Rain, 7, 20, 8, m => m.Wet && m.Night < 0.5f),
            (Beat.Sunbathe, 10, 15, 10, m => m.Night < 0.3f && m.Clouds < 0.45f && !m.Wet),
            (Beat.Clouds, 10, 16, 9, m => m.Night < 0.3f && m.Clouds >= 0.45f && !m.Wet),
            (Beat.Count, 12, 14.5f, 8.5f, m => m.Night < 0.4f),
            (Beat.Nap, 14, 16.5f, 12, m => !m.Running && m.Night < 0.4f),
            (Beat.Sunset, 12, 22, 10, m => m.Glow > 0.35f && !m.Wet),
            (Beat.Stars, 17, 22, 10, m => m.Night > 0.75f && m.Clouds < 0.5f && !m.Wet),
            (Beat.Bedtime, 21, 22, 6, m => !m.Running),
            (Beat.Late, 23, 28, 5, m => m.Running),
        };

        public static IEnumerable<Beat> Due(Moment m)
        {
            foreach (var b in Plan)
            {
                bool inWindow = m.Hour >= b.From && m.Hour < b.To || m.Hour + 24 >= b.From && m.Hour + 24 < b.To;
                if (inWindow && b.Fits(m)) yield return b.Beat;
            }
        }

        public static float Length(Beat beat) => Plan.FirstOrDefault(b => b.Beat == beat).Length;
    }
}
