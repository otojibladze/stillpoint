using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    sealed class Dream
    {
        public DateTime Night;
        public string[] Motifs = new string[0];
        public string Line = "";
        public bool Worried;
    }

    sealed class DayFacts
    {
        public DateTime Date;
        public bool Today, First;
        public int Breaks, DaysTogether, StarNumber, WeedsPulled;
        public double Sat, Longest, Interval;
        public IReadOnlyCollection<string> Seen = new string[0];
        public Keepsake NewKeepsakes;
        public Tone Tone;
        public bool Southern;
        public Func<string, Friend> Friend = s => null;
        public Func<string, bool> FirstEver = s => false;
    }

    static class Dreams
    {
        static readonly string[] Tiers =
        {
            "balloon|plane|traveler|duckling|hedgehog|cubs|fledged|froglets|meteors|comet|aurora|eclipse",
            "chair",
            "fox|owl|rabbit|bat|turtle|mouse|dragonfly|rainbow",
            "cat|squirrel|bird|frog|snail|snails|butterfly|bee|ladybug|moth|worm|flower",
        };

        static readonly string[] Fallbacks = { "moon", "sea", "cloud" };

        static string Fragment(string motif, Func<string, string> who)
        {
            switch (motif)
            {
                case Visitors.Fox: return $"{who(motif)} taught me how to dig";
                case Visitors.Owl: return $"{who(motif)} read me a bedtime story";
                case Visitors.Cat: return $"{who(motif)} and I napped on a warm cloud";
                case Visitors.Squirrel: return $"{who(motif)} buried me like an acorn";
                case Visitors.Rabbit: return $"{who(motif)} and I raced over the hills";
                case Visitors.Mouse: return $"{who(motif)} built a tiny house under my leaf";
                case Visitors.Bird: return $"I could fly with {who(motif)}";
                case Visitors.Frog: return $"{who(motif)} sang with the raindrops";
                case Visitors.Turtle: return $"{who(motif)} took me on a very slow trip";
                case Visitors.Snail: return $"{who(motif)} won a race against the wind";
                case Visitors.Snails: return "two snails got married in our garden";
                case Visitors.Butterfly: return "I had butterfly wings";
                case Visitors.Bee: return "the flowers were humming songs";
                case Visitors.Ladybug: return "a ladybug counted my freckles";
                case Visitors.Moth: return "the moon was a lamp and the moths came to dance";
                case Visitors.Bat: return "the bats taught me to sleep upside down";
                case Visitors.Worm: return "a worm showed me the tunnels under our garden";
                case Visitors.Dragonfly: return "a dragonfly zipped me across the pond";
                case Visitors.Balloon: return "a red balloon carried me over the hills";
                case Visitors.Plane: return "I folded a paper plane and flew it to you";
                case Visitors.Traveler: return "I went to see the sea with a pebble friend";
                case Visitors.Duckling: return "a duckling followed me everywhere";
                case Visitors.Hedgehog: return "a hedgehog curled up next to me to keep warm";
                case Visitors.Meteors: return "the shooting stars landed in our garden";
                case Visitors.Comet: return "I rode a comet across the sky";
                case Visitors.Aurora: return "the sky turned green and danced";
                case Visitors.Eclipse: return "the sun wore a black hat";
                case Visitors.Rainbow: return "I slid down a rainbow";
                case Visitors.Fledged: return "our chicks taught me to fly";
                case Visitors.Cubs: return "the fox cubs used me as a pillow";
                case Visitors.Froglets: return "tiny frogs hopped all over me";
                case "chair": return "you were stuck to a giant chair, so I rolled over to help";
                case "flower": return "my flower grew taller than the tree";
                case "moon": return "the moon came down to say hi";
                case "sea": return "I rolled all the way to the sea";
                default: return "I floated around like a cloud";
            }
        }

        public static Dream Of(DayFacts day)
        {
            var rnd = new Random(day.Date.Year * 400 + day.Date.DayOfYear * 7 + 3);
            var have = new HashSet<string>(day.Seen);
            if (day.Longest >= Math.Max(day.Interval * 2, 5400)) have.Add("chair");
            if (day.Breaks >= Garden.FlowerBreaks && day.Date.Month != 12 && day.Date.Month > 2) have.Add("flower");
            var picked = new List<string>();
            foreach (var tier in Tiers)
            {
                var options = tier.Split('|').Where(have.Contains).OrderBy(x => rnd.Next()).ToList();
                foreach (var m in options)
                    if (picked.Count < 2) picked.Add(m);
            }
            while (picked.Count < 2)
            {
                var fallback = Fallbacks[rnd.Next(Fallbacks.Length)];
                if (!picked.Contains(fallback)) picked.Add(fallback);
            }
            string Who(string species)
            {
                var f = day.Friend(species);
                return f != null && f.Named && !string.IsNullOrEmpty(f.Name) ? f.Name : "the " + species;
            }
            string a = Fragment(picked[0], Who), b = Fragment(picked[1], Who);
            return new Dream { Night = day.Date, Motifs = picked.ToArray(), Line = $"I dreamt {a}, and then {b}.", Worried = picked.Contains("chair") };
        }
    }

    static class Journal
    {
        public static DayFacts Facts(IReadOnlyList<Session> sessions, Session current, Album album, DateTime date, double interval, bool southern)
        {
            var day = date.Date;
            var list = sessions.Where(s => s.Start.Date == day).ToList();
            var done = list.Where(s => s != current).ToList();
            var before = Garden.Progress(sessions, current, day.AddDays(-1)).Earned;
            var after = Garden.Progress(sessions, current, day).Earned;
            var blooms = Constellation.Days(sessions, current);
            int star = blooms.IndexOf(day);
            int grown = Garden.WeedsGrown(sessions, current, day);
            var seen = album == null ? (IReadOnlyCollection<string>)new string[0] : album.On(day);
            return new DayFacts
            {
                Date = day,
                Today = day == DateTime.Today,
                First = sessions.Count > 0 && sessions.Min(s => s.Start.Date) == day,
                Breaks = done.Count,
                Sat = list.Sum(s => s.Seconds),
                Longest = list.Count == 0 ? 0 : list.Max(s => s.Seconds),
                Interval = interval,
                Seen = seen,
                NewKeepsakes = after & ~before,
                DaysTogether = Garden.DaysTogether(sessions, day),
                StarNumber = star < 0 ? 0 : star + 1,
                WeedsPulled = Math.Min(grown, done.Count),
                Tone = Character.ToneOf(Character.Of(Character.Score(sessions, current, day, interval))),
                Southern = southern,
                Friend = s => Friends.Of(album, s, day),
                FirstEver = s => album != null && album.DaysWith(s).FirstOrDefault() == day,
            };
        }

        static string Special(DayFacts d)
        {
            var x = d.Southern ? d.Date.AddMonths(6) : d.Date;
            if (d.Date.Month == 12 && d.Date.Day == 25) return "Christmas in the garden.";
            if (d.Date.Month == 1 && d.Date.Day == 1) return "A new year in the garden.";
            if (d.Date.Month == 12 && d.Date.Day == 31) return "Last day of the year.";
            if (x.Day == 1 && x.Month == 3) return "First day of spring.";
            if (x.Day == 1 && x.Month == 6) return "First day of summer.";
            if (x.Day == 1 && x.Month == 9) return "First day of autumn.";
            if (x.Day == 1 && x.Month == 12) return "First day of winter.";
            return null;
        }

        static string Breaks(DayFacts d, Random rnd)
        {
            int n = d.Breaks;
            string times = n == 1 ? "once" : n == 2 ? "twice" : n + " times";
            if (d.Sat <= 0) return d.Today ? null : "A day off. The garden was quiet without you.";
            if (d.Today) return n == 0 ? "So far today, one long sit." : $"So far today, you got up {times}.";
            if (d.Longest >= Math.Max(d.Interval * 2, 5400))
            {
                string len = Fmt.Hm(d.Longest);
                switch (d.Tone)
                {
                    case Tone.Cheer: return $"You sat {len} in one go. Let's not do that again, okay?";
                    case Tone.Dry: return $"{len} in one sitting. Your chair must love you.";
                    case Tone.Worry: return $"You sat {len} without a break. I worried.";
                    default: return $"One long sit today: {len}.";
                }
            }
            if (n >= 8) return d.Tone == Tone.Dry ? $"{n} breaks. Who are you and what did you do with my human?" : $"You got up {times}. I loved our rhythm today.";
            if (n == 0) return "One long sit, and no breaks.";
            return rnd.Next(2) == 0 ? $"You got up {times}." : $"{n} {(n == 1 ? "break" : "breaks")} today.";
        }

        static IEnumerable<string> Highlights(DayFacts d)
        {
            var seen = d.Seen;
            foreach (var story in Stories.All)
                if (seen.Contains(story.Id)) yield return story.Text;
            string Who(string s)
            {
                var f = d.Friend(s);
                return f != null && f.Named && !string.IsNullOrEmpty(f.Name) ? f.Name : "the " + s;
            }
            if (seen.Contains(Visitors.Cubs)) yield return $"{Visitors.Capital(Who(Visitors.Fox))} brought her cubs to visit.";
            if (seen.Contains(Visitors.Fledged)) yield return "Our chicks learned to fly!";
            if (seen.Contains(Visitors.Froglets)) yield return "Our tadpoles turned into tiny frogs.";
            if (seen.Contains(Visitors.Eclipse)) yield return "We saw an eclipse together.";
            if (seen.Contains(Visitors.Aurora)) yield return "The northern lights danced over the garden.";
            if (seen.Contains(Visitors.Comet)) yield return "A comet flew over us.";
            if (seen.Contains(Visitors.Meteors)) yield return "We watched shooting stars.";
            foreach (var s in Friends.Species)
            {
                if (!seen.Contains(s)) continue;
                var f = d.Friend(s);
                if (f == null) continue;
                if (f.Visits == Friends.NameAt) yield return $"The {s} came back again, so I named it {f.Name}.";
                else if (f.Visits == 20) yield return $"{f.Name} is family now.";
                else if (f.Visits == 10) yield return $"{f.Name} trusts us now.";
                else if (f.Visits == 1) yield return $"{Visitors.Capital(Visitors.Noun(s))} visited for the first time.";
            }
            foreach (var v in Visitors.Order.Where(v => Visitors.IsAnimal(v) && Array.IndexOf(Friends.Species, v) < 0 && seen.Contains(v) && d.FirstEver(v)))
                yield return $"{Visitors.Capital(Visitors.Noun(v))} visited for the first time.";
            if (seen.Contains(Visitors.Rainbow)) yield return "After the rain came a rainbow.";
            var named = Friends.Species.Where(seen.Contains).Select(s => d.Friend(s)).Where(f => f != null && f.Named && f.Visits != Friends.NameAt && f.Visits != 10 && f.Visits != 20 && f.Visits != 1).Select(f => f.Name).ToList();
            if (named.Count == 1) yield return $"{named[0]} came by.";
            else if (named.Count > 1) yield return $"{string.Join(", ", named.Take(named.Count - 1))} and {named.Last()} came by.";
            var others = seen.Where(v => Visitors.IsAnimal(v) && !(Array.IndexOf(Friends.Species, v) >= 0 && d.Friend(v)?.Named == true) && !d.FirstEver(v) && d.Friend(v)?.Visits != 1).ToList();
            var sentence = Visitors.Sentence(others);
            if (sentence != null) yield return sentence;
        }

        static IEnumerable<string> GardenNews(DayFacts d)
        {
            foreach (var (k, name) in new[] { (Keepsake.Moss, "moss on the rock"), (Keepsake.Lantern, "a stone lantern"), (Keepsake.Pond, "a pond"), (Keepsake.Baby, "a baby pebble") })
                if ((d.NewKeepsakes & k) != 0) yield return $"The garden got {name}.";
            if (d.StarNumber > 0)
            {
                if (d.StarNumber % Constellation.Size == 0) yield return "My flower bloomed, and the last star of our constellation lit up!";
                else yield return d.StarNumber == 1 ? "My flower bloomed, and our first star lit up in the sky." : "My flower bloomed, and a new star lit up in our sky.";
            }
            if (d.WeedsPulled > 0) yield return d.WeedsPulled == 1 ? "We pulled a weed." : "We pulled the weeds.";
            if (d.DaysTogether == 7) yield return "Our tree is one week old.";
            else if (d.DaysTogether == 30) yield return "Our tree is one month old.";
            else if (d.DaysTogether == 100) yield return "Our tree is 100 days old.";
            else if (d.DaysTogether == 365) yield return "Our tree is one year old.";
        }

        public static string Entry(DayFacts d, Dream dream)
        {
            var rnd = new Random(d.Date.Year * 400 + d.Date.DayOfYear);
            var parts = new List<string>();
            if (d.First) parts.Add("Today we met. I have a good feeling about you.");
            else
            {
                string special = Special(d);
                if (special != null) parts.Add(special);
            }
            string breaks = Breaks(d, rnd);
            if (breaks != null) parts.Add(breaks);
            parts.AddRange(Highlights(d).Take(2));
            parts.AddRange(GardenNews(d).Take(1));
            if (dream != null && !d.Today) parts.Add("That night I " + dream.Line.Substring(2));
            if (parts.Count == 0) return d.Today ? "Nothing yet today. I'm here when you're ready." : "A quiet day.";
            return string.Join(" ", parts);
        }
    }
}
