using System;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace Stillpoint
{
    enum EclipseKind { SolarTotal, SolarAnnular, SolarPartial, LunarTotal, LunarPartial, LunarPenumbral }

    sealed class Eclipse
    {
        public EclipseKind Kind;
        public DateTime Greatest;
        public float Magnitude;
        public double Lat, Lon;
        public double PartialHalf, TotalHalf;
        public bool Solar => Kind <= EclipseKind.SolarPartial;
    }

    sealed class Shower
    {
        public string Name;
        public int Month, Day;
        public float Zhr, Spread;
        public PointF Radiant;
    }

    struct Place
    {
        public double Lat, Lon;
        public bool Exact;
    }

    struct SkyState
    {
        public Shower Shower;
        public float ShowerLevel;
        public Eclipse Eclipse;
        public float MoonShadow, MoonDim, SunCover;
        public bool EclipseHere;
        public float Comet, CometProgress;
        public float Aurora;
        public bool AuroraReal;
    }

    static class SkyEvents
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static Eclipse E(EclipseKind kind, string utc, float magnitude, double lat, double lon, double partialHalf = 0, double totalHalf = 0) => new Eclipse
        {
            Kind = kind,
            Greatest = DateTime.SpecifyKind(DateTime.ParseExact(utc, "yyyy-MM-dd HH:mm", Inv), DateTimeKind.Utc),
            Magnitude = magnitude,
            Lat = lat,
            Lon = lon,
            PartialHalf = partialHalf,
            TotalHalf = totalHalf,
        };

        public static readonly Eclipse[] Eclipses =
        {
            E(EclipseKind.SolarAnnular, "2026-02-17 12:12", 0.963f, -64.7, 86.8),
            E(EclipseKind.LunarTotal, "2026-03-03 11:34", 1.151f, 6.0, -171.0, 103.5, 29.5),
            E(EclipseKind.SolarTotal, "2026-08-12 17:46", 1.039f, 65.2, -25.2),
            E(EclipseKind.LunarPartial, "2026-08-28 04:13", 0.930f, -9.0, -63.0, 99),
            E(EclipseKind.SolarAnnular, "2027-02-06 16:00", 0.928f, -31.3, -48.5),
            E(EclipseKind.LunarPenumbral, "2027-02-20 23:13", 0, 10.0, 15.0),
            E(EclipseKind.LunarPenumbral, "2027-07-18 16:03", 0, -22.0, 121.0),
            E(EclipseKind.SolarTotal, "2027-08-02 10:07", 1.079f, 25.5, 33.2),
            E(EclipseKind.LunarPenumbral, "2027-08-17 07:14", 0, -12.0, -108.0),
            E(EclipseKind.LunarPartial, "2028-01-12 04:13", 0.066f, 23.0, -61.0, 28),
            E(EclipseKind.SolarAnnular, "2028-01-26 15:08", 0.921f, 3.0, -51.5),
            E(EclipseKind.LunarPartial, "2028-07-06 18:20", 0.389f, -23.0, 86.0, 70.5),
            E(EclipseKind.SolarTotal, "2028-07-22 02:55", 1.056f, -15.6, 126.7),
            E(EclipseKind.LunarTotal, "2028-12-31 16:52", 1.246f, 23.0, 108.0, 104, 36),
            E(EclipseKind.SolarPartial, "2029-01-14 17:13", 0.871f, 63.7, -114.2),
            E(EclipseKind.SolarPartial, "2029-06-12 04:05", 0.458f, 66.8, -66.2),
            E(EclipseKind.LunarTotal, "2029-06-26 03:22", 1.844f, -23.0, -50.0, 110, 51),
            E(EclipseKind.SolarPartial, "2029-07-11 15:36", 0.230f, -64.3, -85.6),
            E(EclipseKind.SolarPartial, "2029-12-05 15:03", 0.891f, -67.5, 135.7),
            E(EclipseKind.LunarTotal, "2029-12-20 22:42", 1.117f, 23.0, 19.0, 107, 27),
            E(EclipseKind.SolarAnnular, "2030-06-01 06:28", 0.944f, 56.5, 80.1),
            E(EclipseKind.LunarPartial, "2030-06-15 18:33", 0.503f, -23.0, 82.0, 72),
            E(EclipseKind.SolarTotal, "2030-11-25 06:50", 1.047f, -43.6, 71.2),
            E(EclipseKind.LunarPenumbral, "2030-12-09 22:28", 0, 22.0, 21.0),
        };

        public static readonly Shower[] Showers =
        {
            new Shower { Name = "the Quadrantids", Month = 1, Day = 4, Zhr = 110, Spread = 0.6f, Radiant = new PointF(70, -40) },
            new Shower { Name = "the Lyrids", Month = 4, Day = 22, Zhr = 18, Spread = 1.2f, Radiant = new PointF(210, -50) },
            new Shower { Name = "the Eta Aquariids", Month = 5, Day = 6, Zhr = 50, Spread = 2.5f, Radiant = new PointF(30, 10) },
            new Shower { Name = "the Perseids", Month = 8, Day = 12, Zhr = 100, Spread = 2.2f, Radiant = new PointF(250, -40) },
            new Shower { Name = "the Orionids", Month = 10, Day = 21, Zhr = 20, Spread = 2.5f, Radiant = new PointF(240, 0) },
            new Shower { Name = "the Leonids", Month = 11, Day = 17, Zhr = 15, Spread = 1.2f, Radiant = new PointF(90, -30) },
            new Shower { Name = "the Geminids", Month = 12, Day = 14, Zhr = 150, Spread = 1.4f, Radiant = new PointF(130, -50) },
        };

        public static Place PlaceFor(double lat, double lon, DateTime local)
        {
            if (!double.IsNaN(lat) && !double.IsNaN(lon)) return new Place { Lat = lat, Lon = lon, Exact = true };
            double offset = TimeZoneInfo.Local.GetUtcOffset(local).TotalHours;
            return new Place { Lat = 40, Lon = offset * 15, Exact = false };
        }

        public static DateTime NightOf(DateTime local) => local.Hour < 12 ? local.Date.AddDays(-1) : local.Date;

        public static (Shower Shower, float Level) ShowerAt(DateTime local)
        {
            Shower best = null;
            float level = 0;
            foreach (var shower in Showers)
                for (int year = local.Year - 1; year <= local.Year + 1; year++)
                {
                    double days = (local - new DateTime(year, shower.Month, shower.Day, 2, 0, 0)).TotalDays;
                    float k = (float)(Math.Exp(-0.5 * days * days / (shower.Spread * shower.Spread)) * shower.Zhr / 150);
                    if (k > level) { level = k; best = shower; }
                }
            return level < 0.02f ? (null, 0) : (best, level);
        }

        public static Eclipse EclipseNear(DateTime utc, double hours) =>
            Eclipses.FirstOrDefault(e => Math.Abs((e.Greatest - utc).TotalHours) <= hours);

        public static Eclipse EclipseOnDay(DateTime localDate) =>
            Eclipses.FirstOrDefault(e => e.Greatest.ToLocalTime().Date == localDate.Date);

        public static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            double r = Math.PI / 180, a = lat1 * r, b = lat2 * r, d = (lon2 - lon1) * r;
            double c = Math.Sin(a) * Math.Sin(b) + Math.Cos(a) * Math.Cos(b) * Math.Cos(d);
            return Math.Acos(Math.Max(-1, Math.Min(1, c))) / r;
        }

        public static double MoonAltitude(Eclipse e, DateTime utc, Place place)
        {
            double hours = (utc - e.Greatest).TotalHours;
            return 90 - Distance(place.Lat, place.Lon, e.Lat, e.Lon - 14.5 * hours);
        }

        public static void Lunar(Eclipse e, DateTime utc, out float shadow, out float dim)
        {
            shadow = dim = 0;
            double minutes = Math.Abs((utc - e.Greatest).TotalMinutes);
            if (e.Kind == EclipseKind.LunarPenumbral)
            {
                if (minutes < 120) dim = (float)(0.4 * (1 - minutes / 120));
                return;
            }
            if (minutes >= e.PartialHalf + 60) return;
            dim = (float)(0.3 * (1 - Math.Min(1, Math.Max(0, minutes - e.PartialHalf) / 60)));
            if (minutes >= e.PartialHalf) return;
            if (e.Kind == EclipseKind.LunarTotal && minutes <= e.TotalHalf) { shadow = 1; return; }
            double reach = e.Kind == EclipseKind.LunarTotal ? 1 : Math.Min(1, e.Magnitude);
            double inner = e.Kind == EclipseKind.LunarTotal ? e.TotalHalf : 0;
            double u = 1 - (minutes - inner) / (e.PartialHalf - inner);
            shadow = (float)(reach * SpiritArt.Smooth(0, 1, (float)u));
            if (e.Kind == EclipseKind.LunarTotal) shadow = Math.Min(0.97f, shadow);
        }

        public static float SolarPeak(Eclipse e, Place place)
        {
            double d = Distance(place.Lat, place.Lon, e.Lat, e.Lon);
            if (d > 45) return 0;
            if (d < 1.5 && e.Kind != EclipseKind.SolarPartial) return 1;
            return (float)Math.Min(0.96, e.Magnitude * SpiritArt.Clamp((float)(1.1 - d / 45), 0.3f, 1));
        }

        public static float SolarCover(Eclipse e, DateTime utc, Place place)
        {
            double minutes = (utc - e.Greatest).TotalMinutes;
            if (Math.Abs(minutes) > 75) return 0;
            var local = utc.ToLocalTime();
            var sun = Sky.Sun(local.Date, place.Lat, place.Lon);
            if (sun.Valid && (local < sun.Rise.AddMinutes(10) || local > sun.Set.AddMinutes(-10))) return 0;
            float peak = SolarPeak(e, place);
            return (float)(peak * (1 - (minutes / 75) * (minutes / 75)));
        }

        public static bool LunarVisible(Eclipse e, Place place) =>
            MoonAltitude(e, e.Greatest, place) > 3 || MoonAltitude(e, e.Greatest.AddMinutes(-e.PartialHalf), place) > 3 || MoonAltitude(e, e.Greatest.AddMinutes(e.PartialHalf), place) > 3;

        public static bool SolarVisible(Eclipse e, Place place)
        {
            if (SolarPeak(e, place) < 0.12f) return false;
            var local = e.Greatest.ToLocalTime();
            var sun = Sky.Sun(local.Date, place.Lat, place.Lon);
            return !sun.Valid || (local > sun.Rise.AddMinutes(10) && local < sun.Set.AddMinutes(-10));
        }

        static uint Hash(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return x;
        }

        public static (float Level, float Progress) Comet(DateTime local)
        {
            double into = (local - CometStart(NightOf(local).Year).AddHours(18)).TotalDays;
            if (into < 0 || into >= 10) return (0, 0);
            float u = (float)(into / 10);
            return ((float)Math.Sqrt(Math.Sin(u * Math.PI)), u);
        }

        public static DateTime CometStart(int year) => new DateTime(year, 1, 1).AddDays(30 + (int)(Hash((uint)year * 7919u) % 300) - 1);

        public static bool MagicAurora(DateTime night) =>
            (night.Month >= 9 || night.Month <= 3) && Hash((uint)(night.Year * 1000 + night.DayOfYear)) % 90 == 7;

        public static double GeomagneticLatitude(double lat, double lon)
        {
            double r = Math.PI / 180, poleLat = 80.7 * r, poleLon = -72.7 * r;
            double s = Math.Sin(lat * r) * Math.Sin(poleLat) + Math.Cos(lat * r) * Math.Cos(poleLat) * Math.Cos(lon * r - poleLon);
            return Math.Asin(Math.Max(-1, Math.Min(1, s))) / r;
        }

        public static float RealAurora(float kp, Place place)
        {
            if (float.IsNaN(kp) || !place.Exact) return 0;
            double reach = 63 - 3.3 * kp, geomag = Math.Abs(GeomagneticLatitude(place.Lat, place.Lon));
            return geomag < reach ? 0 : SpiritArt.Clamp((float)((geomag - reach) / 6 + 0.35), 0, 1);
        }

        public static SkyState At(DateTime local, Place place, float kp)
        {
            var sky = new SkyState();
            var utc = local.ToUniversalTime();
            (sky.Shower, sky.ShowerLevel) = ShowerAt(local);
            var e = EclipseNear(utc, 4);
            if (e != null)
            {
                sky.Eclipse = e;
                if (e.Solar)
                {
                    sky.EclipseHere = SolarVisible(e, place);
                    sky.SunCover = sky.EclipseHere ? SolarCover(e, utc, place) : 0;
                }
                else
                {
                    sky.EclipseHere = LunarVisible(e, place);
                    if (sky.EclipseHere && MoonAltitude(e, utc, place) > 0) Lunar(e, utc, out sky.MoonShadow, out sky.MoonDim);
                }
            }
            (sky.Comet, sky.CometProgress) = Comet(local);
            float real = RealAurora(kp, place);
            sky.AuroraReal = real > 0;
            sky.Aurora = Math.Max(real, MagicAurora(NightOf(local)) ? 0.85f : 0);
            return sky;
        }
    }
}
