using System;
using System.Linq;

namespace Stillpoint
{
    enum CubStage { None, Small, Playful, Big }

    enum PondStage { None, Spawn, Tadpoles, Froglets }

    struct Caches
    {
        public int Buried, Sprouts, Spot;
        public float Growth;
        public bool Autumn, Winter;
    }

    static class Wildlife
    {
        public const int MaxCaches = 4;

        static DateTime Shift(DateTime date, bool southern) => southern ? date.AddMonths(6) : date;

        static float Frac(DateTime d, DateTime a, DateTime b) => SpiritArt.Clamp((float)((d - a).TotalDays / (b - a).TotalDays), 0, 1);

        public static (CubStage Stage, float Progress) Cubs(DateTime date, bool southern)
        {
            var d = Shift(date, southern);
            DateTime At(int month, int day) => new DateTime(d.Year, month, day);
            if (d < At(4, 20) || d >= At(7, 10)) return (CubStage.None, 0);
            if (d < At(5, 15)) return (CubStage.Small, Frac(d, At(4, 20), At(5, 15)));
            if (d < At(6, 15)) return (CubStage.Playful, Frac(d, At(5, 15), At(6, 15)));
            return (CubStage.Big, Frac(d, At(6, 15), At(7, 10)));
        }

        public static int CubCount(DateTime date) => 2 + date.Year % 2;

        public static (PondStage Stage, float Progress) Pond(DateTime date, bool southern)
        {
            var d = Shift(date, southern);
            DateTime At(int month, int day) => new DateTime(d.Year, month, day);
            if (d < At(3, 1) || d >= At(7, 1)) return (PondStage.None, 0);
            if (d < At(4, 1)) return (PondStage.Spawn, Frac(d, At(3, 1), At(4, 1)));
            if (d < At(6, 10)) return (PondStage.Tadpoles, Frac(d, At(4, 1), At(6, 10)));
            return (PondStage.Froglets, Frac(d, At(6, 10), At(7, 1)));
        }

        public static Caches Squirrel(Album album, DateTime date, bool southern)
        {
            var d = Shift(date.Date, southern);
            var result = new Caches { Spot = -1 };
            if (album == null || d.Month >= 6 && d.Month <= 8) return result;
            int year = d.Month >= 9 ? d.Year : d.Year - 1;
            var autumn = new DateTime(year, 9, 1);
            var winter = new DateTime(year, 12, 1);
            var spring = new DateTime(year + 1, 3, 1);
            var summer = new DateTime(year + 1, 6, 1);
            var days = album.DaysWith(Visitors.Squirrel).Select(x => Shift(x, southern)).ToList();
            int Count(DateTime from, DateTime to) => days.Count(x => x >= from && x < to);
            if (d < winter)
            {
                int before = Count(autumn, d);
                result.Autumn = true;
                result.Buried = Math.Min(MaxCaches, Count(autumn, d.AddDays(1)));
                result.Spot = before % MaxCaches;
                return result;
            }
            int buried = Math.Min(MaxCaches, Count(autumn, winter));
            if (d < spring)
            {
                int left = Math.Max(0, buried - Count(winter, d));
                result.Winter = true;
                result.Buried = Math.Max(0, buried - Count(winter, d.AddDays(1)));
                result.Spot = left - 1;
                return result;
            }
            int remaining = Math.Max(0, buried - Count(winter, spring));
            var rise = new DateTime(year + 1, 3, 10);
            if (d < rise) result.Buried = remaining;
            else
            {
                result.Sprouts = remaining;
                result.Growth = Frac(d, rise, summer);
            }
            return result;
        }
    }
}
