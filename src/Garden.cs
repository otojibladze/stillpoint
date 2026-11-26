using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Stillpoint
{
    enum Season { Spring, Summer, Autumn, Winter }

    [Flags]
    enum Keepsake { None = 0, Moss = 1, Lantern = 2, Pond = 4, Baby = 8 }

    static class Visitors
    {
        public const string Butterfly = "butterfly", Bird = "bird", Snail = "snail", Frog = "frog", Cat = "cat", Rainbow = "rainbow";
        public static readonly string[] Order = { Butterfly, Bird, Snail, Frog, Cat, Rainbow };

        public static string Label(string id) => id == Rainbow ? "Rain, then a rainbow" : char.ToUpperInvariant(id[0]) + id.Substring(1);

        public static IEnumerable<string> Sorted(IEnumerable<string> ids) => Order.Where(ids.Contains);

        public static string Sentence(IEnumerable<string> ids)
        {
            var animals = Sorted(ids).Where(v => v != Rainbow).Select(v => "a " + v).ToList();
            if (animals.Count == 0) return null;
            string list = animals.Count == 1 ? animals[0]
                : string.Join(", ", animals.Take(animals.Count - 1)) + " and " + animals[animals.Count - 1];
            return char.ToUpperInvariant(list[0]) + list.Substring(1) + " stopped by.";
        }
    }

    sealed class GardenDay
    {
        public DateTime Date;
        public int Breaks;
        public double SitSeconds, LongestSeconds;
        public Season Season;
        public Keepsake Keepsakes;
        public HashSet<string> Visitors = new HashSet<string>();
        public float Clouds, Rain;
        public bool Today;
        public bool Empty => SitSeconds <= 0;
    }

    sealed class KeepsakeProgress
    {
        public int Streak, BestStreak, Breaks, Days;
        public DateTime? MossSince, LanternSince, PondSince, BabySince;

        public Keepsake Earned =>
            (MossSince != null ? Keepsake.Moss : 0) | (LanternSince != null ? Keepsake.Lantern : 0)
            | (PondSince != null ? Keepsake.Pond : 0) | (BabySince != null ? Keepsake.Baby : 0);
    }

    static class Weather
    {
        public static float Clouds(double sitSeconds, double interval) =>
            interval <= 0 ? 0 : SpiritArt.Smooth(0.75f, 1.5f, (float)(sitSeconds / interval));

        public static float Rain(double sitSeconds, double interval) =>
            interval <= 0 ? 0 : SpiritArt.Smooth(1.75f, 2.5f, (float)(sitSeconds / interval));
    }

    static class Garden
    {
        public const int FlowerBreaks = 4, MossStreak = 7, LanternBreaks = 50, PondBreaks = 100, BabyDays = 30;

        public static Season SeasonOf(DateTime d) =>
            d.Month == 12 || d.Month <= 2 ? Season.Winter : d.Month <= 5 ? Season.Spring : d.Month <= 8 ? Season.Summer : Season.Autumn;

        public static int BreaksOn(IReadOnlyList<Session> sessions, Session current, DateTime day) =>
            sessions.Count(s => s != current && s.Start.Date == day.Date);

        public static KeepsakeProgress Progress(IReadOnlyList<Session> sessions, Session current, DateTime asOf)
        {
            var done = sessions.Where(s => s != current && s.Start.Date <= asOf.Date).OrderBy(s => s.Start).ToList();
            var p = new KeepsakeProgress { Breaks = done.Count };
            if (done.Count >= LanternBreaks) p.LanternSince = done[LanternBreaks - 1].Start.Date;
            if (done.Count >= PondBreaks) p.PondSince = done[PondBreaks - 1].Start.Date;
            var days = done.Select(s => s.Start.Date).Distinct().OrderBy(d => d).ToList();
            p.Days = days.Count;
            if (days.Count >= BabyDays) p.BabySince = days[BabyDays - 1];
            int run = 0;
            DateTime previous = DateTime.MinValue;
            foreach (var d in days)
            {
                run = previous != DateTime.MinValue && d == previous.AddDays(1) ? run + 1 : 1;
                previous = d;
                p.BestStreak = Math.Max(p.BestStreak, run);
                if (run >= MossStreak && p.MossSince == null) p.MossSince = d;
            }
            p.Streak = previous >= asOf.Date.AddDays(-1) ? run : 0;
            return p;
        }

        public static GardenDay Day(IReadOnlyList<Session> sessions, Session current, DateTime day, Album album, double interval)
        {
            var list = sessions.Where(s => s.Start.Date == day.Date).ToList();
            var g = new GardenDay
            {
                Date = day.Date,
                Season = SeasonOf(day),
                Breaks = list.Count(s => s != current),
                SitSeconds = list.Sum(s => s.Seconds),
                LongestSeconds = list.Count == 0 ? 0 : list.Max(s => s.Seconds),
                Keepsakes = Progress(sessions, current, day).Earned,
                Today = day.Date == DateTime.Today,
            };
            if (album != null) g.Visitors = new HashSet<string>(album.On(day));
            g.Clouds = Weather.Clouds(g.LongestSeconds, interval);
            g.Rain = Weather.Rain(g.LongestSeconds, interval);
            if (g.Visitors.Contains(Visitors.Rainbow)) g.Rain = Math.Max(g.Rain, 0.4f);
            return g;
        }

        public static string Rhythm(GardenDay day, double interval)
        {
            if (day.Empty) return "A quiet day away from the screen.";
            if (day.Breaks == 0) return "One long sit so far. A short walk would feel good.";
            double average = day.SitSeconds / (day.Breaks + 1) / 60;
            if (day.LongestSeconds <= interval * 1.15)
                return $"Good rhythm today. You got up about every {Math.Max(1, (int)Math.Round(average))} minutes.";
            if (day.LongestSeconds <= interval * 2)
                return "A steady day, with a couple of longer stretches.";
            return "Some long stretches today. Tomorrow, try getting up a little sooner.";
        }
    }

    public sealed class Album
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        readonly Dictionary<DateTime, HashSet<string>> days = new Dictionary<DateTime, HashSet<string>>();

        public bool Dirty { get; set; }

        public IReadOnlyCollection<string> On(DateTime day) =>
            days.TryGetValue(day.Date, out var set) ? (IReadOnlyCollection<string>)set : new string[0];

        public bool Add(DateTime day, string visitor)
        {
            if (!days.TryGetValue(day.Date, out var set)) days[day.Date] = set = new HashSet<string>();
            if (!set.Add(visitor)) return false;
            Dirty = true;
            return true;
        }

        public static Album Parse(IEnumerable<string> lines)
        {
            var album = new Album();
            foreach (var raw in lines ?? new string[0])
            {
                var parts = raw.Trim().Split(',');
                if (parts.Length < 2 || !DateTime.TryParseExact(parts[0], "yyyy-MM-dd", Inv, DateTimeStyles.None, out var date)) continue;
                foreach (var v in parts[1].Split('|'))
                    if (Visitors.Order.Contains(v)) album.Add(date, v);
            }
            album.Dirty = false;
            return album;
        }

        public string Format() =>
            string.Concat(days.OrderBy(d => d.Key).Where(d => d.Value.Count > 0)
                .Select(d => d.Key.ToString("yyyy-MM-dd", Inv) + "," + string.Join("|", Visitors.Sorted(d.Value)) + "\r\n"));
    }
}
