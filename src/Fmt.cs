using System;

namespace Stillpoint
{
    static class Fmt
    {
        public static string Clock(double seconds)
        {
            long t = (long)Math.Max(0, seconds);
            return $"{t / 3600}:{t / 60 % 60:00}:{t % 60:00}";
        }

        public static string Hm(double seconds)
        {
            long minutes = (long)Math.Max(0, seconds) / 60;
            return minutes < 60 ? $"{minutes}m" : $"{minutes / 60}h {minutes % 60:00}m";
        }

        public static string Words(double seconds)
        {
            long total = (long)Math.Round(Math.Max(0, seconds) / 60);
            long hours = total / 60, minutes = total % 60;
            string h = hours == 1 ? "1 hour" : hours + " hours";
            string m = minutes == 1 ? "1 minute" : minutes + " minutes";
            if (hours == 0) return m;
            return minutes == 0 ? h : h + " " + m;
        }
    }
}
