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

    static class Roam
    {
        public const double Every = 1800, Gives = 300;

        public static double Earned(double sat) => Math.Floor(sat / Every) * Gives;

        public static RoamTime Today(IReadOnlyList<Session> sessions, Session current, DateTime now, DateTime from)
        {
            double sat = 0, left = 0, full = 0;
            DateTime? lastEnd = null;
            foreach (var s in sessions.Where(s => s.Start.Date == now.Date && s.Start >= from).OrderBy(s => s.Start))
            {
                if (lastEnd != null) left = Math.Max(0, left - (s.Start - lastEnd.Value).TotalSeconds);
                double before = Earned(sat);
                sat += s.Seconds;
                left += Earned(sat) - before;
                full = left;
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
