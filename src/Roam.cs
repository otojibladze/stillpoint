using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    struct RoamTime
    {
        public double Left, Full, ToNext;
        public bool Running, Shown;
    }

    struct RoamGap
    {
        public DateTime At;
        public double Sat, Gap;
    }

    static class Roam
    {
        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        public static List<RoamGap> ParseGaps(string text)
        {
            var list = new List<RoamGap>();
            foreach (var part in (text ?? "").Split(';'))
            {
                var bits = part.Split('|');
                if (bits.Length == 3 && DateTime.TryParseExact(bits[0], TimeFormat, Inv, System.Globalization.DateTimeStyles.None, out var at)
                    && double.TryParse(bits[1], System.Globalization.NumberStyles.Float, Inv, out var sat) && double.TryParse(bits[2], System.Globalization.NumberStyles.Float, Inv, out var gap))
                    list.Add(new RoamGap { At = at, Sat = sat, Gap = gap });
            }
            return list;
        }

        public static string AddGap(string text, RoamGap gap) =>
            string.Join(";", ParseGaps(text).Where(g => g.At.Date == gap.At.Date).Concat(new[] { gap })
                .Select(g => g.At.ToString(TimeFormat, Inv) + "|" + Math.Round(g.Sat).ToString(Inv) + "|" + Math.Round(g.Gap).ToString(Inv)));

        public const double Every = 1800, Gives = 300;

        public static double Earned(double sat) => Math.Floor(sat / Every) * Gives;

        public static RoamTime Today(IReadOnlyList<Session> sessions, Session current, DateTime now, DateTime from, IReadOnlyList<RoamGap> gaps = null)
        {
            double sat = 0, left = 0, full = 0;
            DateTime? lastEnd = null;
            void Earn(double seconds)
            {
                double before = Earned(sat);
                sat += seconds;
                left += Earned(sat) - before;
                full = left;
            }
            foreach (var s in sessions.Where(s => s.Start.Date == now.Date && s.Start >= from).OrderBy(s => s.Start))
            {
                if (lastEnd != null) left = Math.Max(0, left - (s.Start - lastEnd.Value).TotalSeconds);
                double done = 0;
                foreach (var g in (gaps ?? new RoamGap[0]).Where(g => g.At >= s.Start && g.At <= s.End).OrderBy(g => g.At))
                {
                    double part = Math.Max(0, Math.Min(s.Seconds, g.Sat) - done);
                    Earn(part);
                    done += part;
                    left = Math.Max(0, left - g.Gap);
                }
                Earn(Math.Max(0, s.Seconds - done));
                lastEnd = s == current ? (DateTime?)null : s.End;
            }
            if (lastEnd != null && current == null) left = Math.Max(0, left - (now - lastEnd.Value).TotalSeconds);
            return new RoamTime
            {
                Left = left,
                Full = full,
                ToNext = sat % Every / Every,
                Running = current != null,
                Shown = sat > 0 || current != null,
            };
        }
    }
}
