using System;

namespace Stillpoint
{
    struct SunTimes
    {
        public DateTime Rise, Set;
        public bool Valid;
    }

    static class Sky
    {
        static readonly DateTime NewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
        const double SynodicMonth = 29.530588853;

        public static float MoonPhase(DateTime local)
        {
            double days = (local.ToUniversalTime() - NewMoon).TotalDays;
            double phase = days / SynodicMonth % 1;
            return (float)(phase < 0 ? phase + 1 : phase);
        }

        public static float MoonLight(float phase) => (float)((1 - Math.Cos(phase * 2 * Math.PI)) / 2);

        public static SunTimes Sun(DateTime date, double lat, double lon)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon)) return default(SunTimes);
            double gamma = 2 * Math.PI / 365 * (date.DayOfYear - 1);
            double eqTime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma) - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));
            double decl = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma) - 0.006758 * Math.Cos(2 * gamma)
                + 0.000907 * Math.Sin(2 * gamma) - 0.002697 * Math.Cos(3 * gamma) + 0.00148 * Math.Sin(3 * gamma);
            double latR = lat * Math.PI / 180;
            double cosHa = Math.Cos(90.833 * Math.PI / 180) / (Math.Cos(latR) * Math.Cos(decl)) - Math.Tan(latR) * Math.Tan(decl);
            if (cosHa > 1 || cosHa < -1) return default(SunTimes);
            double ha = Math.Acos(cosHa) * 180 / Math.PI;
            var midnight = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            return new SunTimes
            {
                Rise = midnight.AddMinutes(720 - 4 * (lon + ha) - eqTime).ToLocalTime(),
                Set = midnight.AddMinutes(720 - 4 * (lon - ha) - eqTime).ToLocalTime(),
                Valid = true,
            };
        }

        public static float Darkness(DateTime now, SunTimes sun)
        {
            if (!sun.Valid)
            {
                float h = (float)now.TimeOfDay.TotalHours;
                return Math.Max(SpiritArt.Smooth(20.5f, 21.5f, h), 1 - SpiritArt.Smooth(5.5f, 6.5f, h));
            }
            float evening = SpiritArt.Smooth(-10, 55, (float)(now - sun.Set).TotalMinutes);
            float morning = SpiritArt.Smooth(-10, 55, (float)(sun.Rise - now).TotalMinutes);
            return Math.Max(evening, morning);
        }

        public static float Glow(DateTime now, SunTimes sun)
        {
            if (!sun.Valid) return 0;
            return Math.Max(Bump((float)(now - sun.Set).TotalMinutes, -70, 5, 45), Bump((float)(now - sun.Rise).TotalMinutes, -45, 5, 60));
        }

        static float Bump(float x, float start, float peak, float end) =>
            x <= start || x >= end ? 0 : x < peak ? SpiritArt.Smooth(start, peak, x) : 1 - SpiritArt.Smooth(peak, end, x);
    }
}
