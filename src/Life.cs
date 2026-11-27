using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    enum Temper { Sunny, Easygoing, Thoughtful, Sarcastic, Worried }

    enum Tone { Cheer, Plain, Dry, Worry }

    static class Character
    {
        public static float Score(IReadOnlyList<Session> sessions, Session current, DateTime today, double interval)
        {
            if (interval <= 0) return 0;
            double sum = 0, weights = 0;
            var recent = sessions.Where(s => s != current && s.Seconds >= 60 && s.Start.Date <= today.Date && s.Start.Date > today.Date.AddDays(-21));
            foreach (var day in recent.GroupBy(s => s.Start.Date))
            {
                double n = day.Count();
                double good = day.Count(s => s.Seconds <= interval * 1.15) / n;
                double bad = day.Count(s => s.Seconds > interval * 2) / n;
                double score = Math.Max(-1, Math.Min(1, good * 1.2 - 0.2 - bad * 1.6));
                double weight = Math.Pow(0.88, (today.Date - day.Key).TotalDays);
                sum += score * weight;
                weights += weight;
            }
            if (weights <= 0) return 0;
            return (float)(sum / weights * Math.Min(1, weights / 3));
        }

        public static Temper Of(float score) =>
            score >= 0.5f ? Temper.Sunny : score >= 0.15f ? Temper.Easygoing : score > -0.15f ? Temper.Thoughtful : score > -0.5f ? Temper.Sarcastic : Temper.Worried;

        public static Temper Settle(float score, Temper current)
        {
            var next = Of(score);
            return next != current && Of(score - 0.06f) == next && Of(score + 0.06f) == next ? next : current;
        }

        public static Tone ToneOf(Temper temper) =>
            temper == Temper.Sunny || temper == Temper.Easygoing ? Tone.Cheer : temper == Temper.Thoughtful ? Tone.Plain : temper == Temper.Sarcastic ? Tone.Dry : Tone.Worry;

        public static string Label(Temper temper) => temper.ToString();

        public static string Describe(Temper temper) =>
            temper == Temper.Sunny ? "Calm and cheerful" : temper == Temper.Easygoing ? "Relaxed" : temper == Temper.Thoughtful ? "Quiet and curious"
            : temper == Temper.Sarcastic ? "Dry and witty" : "Worried about you";

        public static string Shift(Temper temper) =>
            temper == Temper.Sunny ? "I feel lighter lately. All those breaks are rubbing off on me."
            : temper == Temper.Easygoing ? "Things feel easy lately. I like our rhythm."
            : temper == Temper.Thoughtful ? "I've been thinking a lot lately. About sand, mostly."
            : temper == Temper.Sarcastic ? "I've developed a sense of humor. A dry one. Wonder why."
            : "I worry about you, you know. All this sitting.";
    }

    enum Trust { Wary, Curious, Friendly, Trusting, Family }

    sealed class Friend
    {
        public string Species, Name;
        public int Visits;
        public DateTime First;
        public Trust Trust;
        public bool Named => Visits >= Friends.NameAt;
        public string Call => Named ? Name : "the " + Species;
        public string Title => Named ? Name + " the " + Species : Visitors.Capital(Species);
    }

    static class Friends
    {
        public const int NameAt = 3;
        static readonly int[] Levels = { 1, 3, 6, 10, 20 };
        public static readonly string[] Species = { Visitors.Fox, Visitors.Owl, Visitors.Cat, Visitors.Squirrel, Visitors.Bird, Visitors.Frog, Visitors.Turtle, Visitors.Snail };

        static readonly Dictionary<string, string[]> Names = new Dictionary<string, string[]>
        {
            [Visitors.Fox] = new[] { "Ember", "Rusty", "Maple", "Saffron", "Biscuit", "Juniper", "Tango", "Paprika" },
            [Visitors.Owl] = new[] { "Sage", "Professor", "Luna", "Willow", "Ollie", "Mochi", "Pepper", "Atlas" },
            [Visitors.Cat] = new[] { "Miso", "Smudge", "Noodle", "Pumpkin", "Tofu", "Shadow", "Biscotti", "Sir Whiskers" },
            [Visitors.Squirrel] = new[] { "Hazel", "Nutmeg", "Pip", "Chestnut", "Scout", "Bramble", "Nibbles", "Acorn" },
            [Visitors.Bird] = new[] { "Chirp", "Sunny", "Wren", "Melody", "Twig", "Piccolo", "Kiwi", "Clementine" },
            [Visitors.Frog] = new[] { "Puddle", "Lily", "Bubbles", "Fig", "Moss", "Hopper" },
            [Visitors.Turtle] = new[] { "Sheldon", "Tank", "Admiral", "Bean", "Clover", "Marlowe" },
            [Visitors.Snail] = new[] { "Shelly", "Turbo", "Sprinkle", "Professor Slow", "Dot", "Bramble" },
        };

        public static Trust TrustFor(int visits)
        {
            var trust = Trust.Wary;
            for (int i = 0; i < Levels.Length; i++)
                if (visits >= Levels[i]) trust = (Trust)i;
            return trust;
        }

        public static int Needed(Trust trust) => (int)trust + 1 < Levels.Length ? Levels[(int)trust + 1] : Levels[Levels.Length - 1];

        public static float ToNext(Friend f)
        {
            if (f.Trust == Trust.Family) return 1;
            int from = Levels[(int)f.Trust], to = Needed(f.Trust);
            return (f.Visits - from) / (float)(to - from);
        }

        public static string Word(Trust trust) => trust.ToString();

        public static Friend Of(Album album, string species, DateTime asOf)
        {
            var days = album?.DaysWith(species).Where(d => d <= asOf.Date).ToList() ?? new List<DateTime>();
            if (days.Count == 0) return null;
            var pool = Names.TryGetValue(species, out var list) ? list : new[] { "Friend" };
            int pick = (int)((uint)(days[0].Year * 372 + days[0].DayOfYear * 31 + species.Length * 7 + species[0]) % (uint)pool.Length);
            return new Friend { Species = species, Name = pool[pick], Visits = days.Count, First = days[0], Trust = TrustFor(days.Count) };
        }

        public static List<Friend> All(Album album, DateTime asOf) =>
            Species.Select(s => Of(album, s, asOf)).Where(f => f != null).OrderByDescending(f => f.Visits).ToList();

        public static string Milestone(Friend f, Tone tone)
        {
            switch (f.Visits)
            {
                case NameAt:
                    return tone == Tone.Dry ? $"The {f.Species} again. Fine, it gets a name. {f.Name}." : $"The {f.Species} came back again. I'm calling it {f.Name}.";
                case 6: return tone == Tone.Dry ? $"{f.Name} keeps showing up. I think we have a fan." : $"{f.Name} is getting used to us.";
                case 10: return $"{f.Name} trusts us now. Look how close.";
                case 20: return tone == Tone.Dry ? $"{f.Name} is family now. Don't tell anyone I said that." : $"{f.Name} is family now.";
                default: return null;
            }
        }
    }

    enum NestStage { None, Building, Eggs, Chicks, Fledglings, Empty }

    static class Nest
    {
        public static (NestStage Stage, float Progress) On(DateTime date, bool southern)
        {
            var d = southern ? date.AddMonths(6) : date;
            DateTime At(int month, int day) => new DateTime(d.Year, month, day);
            float Frac(DateTime a, DateTime b) => SpiritArt.Clamp((float)((d - a).TotalDays / (b - a).TotalDays), 0, 1);
            if (d < At(3, 10) || d >= At(10, 1)) return (NestStage.None, 0);
            if (d < At(4, 1)) return (NestStage.Building, Frac(At(3, 10), At(4, 1)));
            if (d < At(4, 19)) return (NestStage.Eggs, Frac(At(4, 1), At(4, 19)));
            if (d < At(5, 7)) return (NestStage.Chicks, Frac(At(4, 19), At(5, 7)));
            if (d < At(5, 21)) return (NestStage.Fledglings, Frac(At(5, 7), At(5, 21)));
            return (NestStage.Empty, Frac(At(5, 21), At(10, 1)));
        }

        public static bool Fragile(NestStage stage) => stage == NestStage.Eggs || stage == NestStage.Chicks;
    }

    sealed class Memory
    {
        public string Id, Text;
        public int Rank;
    }

    static class Memories
    {
        static readonly (int Rank, string Words, Func<DateTime, DateTime> Back)[] Ago =
        {
            (5, "Two years ago today", d => d.AddYears(-2)),
            (4, "A year ago today", d => d.AddYears(-1)),
            (3, "100 days ago", d => d.AddDays(-100)),
            (2, "A month ago today", d => d.AddMonths(-1)),
            (1, "A week ago today", d => d.AddDays(-7)),
        };

        public static Memory Today(IReadOnlyList<Session> sessions, Session current, Album album, DateTime today, double interval, Temper temper, Func<string, Friend> friend)
        {
            today = today.Date;
            var tone = Character.ToneOf(temper);
            var found = new List<Memory>();
            var done = sessions.Where(s => s != current && s.Seconds >= 60).ToList();
            foreach (var (rank, words, back) in Ago)
            {
                var day = back(today);
                foreach (var m in On(day, rank, words, done, album, interval, tone, friend, today)) found.Add(m);
            }
            return found.OrderByDescending(m => m.Rank).FirstOrDefault();
        }

        static IEnumerable<Memory> On(DateTime day, int agoRank, string ago, List<Session> done, Album album, double interval, Tone tone, Func<string, Friend> friend, DateTime today)
        {
            string id(string what) => what + "@" + day.ToString("yyyy-MM-dd");
            Memory make(string what, int kind, string text) => new Memory { Id = id(what), Rank = agoRank * 10 + kind, Text = text };
            var before = done.Where(s => s.Start.Date <= day).ToList();
            if (before.Count == 0) yield break;

            if (before.Min(s => s.Start.Date) == day)
                yield return make("met", 9, Together(agoRank, tone));

            var onDay = before.Where(s => s.Start.Date == day).ToList();
            if (onDay.Count > 0)
            {
                double longest = onDay.Max(s => s.Seconds);
                if (longest >= Math.Max(interval * 2, 2 * 3600) && longest >= before.Max(s => s.Seconds))
                    yield return make("longest", 7, Longest(ago, Fmt.Hm(longest), tone));
                int breaks = onDay.Count;
                if (breaks >= 6 && breaks >= before.GroupBy(s => s.Start.Date).Max(g => g.Count()))
                    yield return make("busiest", 5, tone == Tone.Dry ? $"{ago} you got up {breaks} times. Show-off." : $"{ago} you got up {breaks} times in one day. Legend.");
                var firstFlower = before.GroupBy(s => s.Start.Date).Where(g => g.Count() >= Garden.FlowerBreaks).Select(g => g.Key).DefaultIfEmpty(DateTime.MaxValue).Min();
                if (firstFlower == day) yield return make("flower", 6, $"{ago} my first flower bloomed.");
            }

            var progress = Garden.Progress(before, null, day);
            foreach (var (since, name) in new[] { (progress.MossSince, "moss"), (progress.LanternSince, "the stone lantern"), (progress.PondSince, "the pond"), (progress.BabySince, "a baby pebble") })
                if (since == day) yield return make("keepsake:" + name, 6, $"{ago} the garden got {name}.");

            if (album == null) yield break;
            foreach (var species in Friends.Species)
            {
                var days = album.DaysWith(species).ToList();
                if (days.Count == 0 || days[0] != day) continue;
                var f = friend?.Invoke(species);
                string who = f != null && f.Named ? f.Name : "the " + species;
                yield return make("first:" + species, 6, $"{ago}, {who} came by for the first time.");
            }

            var seen = album.On(day);
            if (seen.Contains(Visitors.Meteors))
            {
                var shower = SkyEvents.ShowerAt(day.AddHours(22)).Shower;
                yield return make("meteors", 8, $"{ago} we watched {(shower != null ? shower.Name : "a meteor shower")} together.");
            }
            if (seen.Contains(Visitors.Eclipse)) yield return make("eclipse", 8, $"{ago} we saw an eclipse together.");
            if (seen.Contains(Visitors.Aurora)) yield return make("aurora", 8, $"{ago} the northern lights danced over our garden.");
            if (seen.Contains(Visitors.Comet)) yield return make("comet", 8, $"{ago} a comet flew over us.");
            if (seen.Contains(Visitors.Fledged)) yield return make("fledged", 8, $"{ago} our chicks learned to fly.");
            foreach (var story in Stories.All)
                if (seen.Contains(story.Id)) yield return make(story.Id, 8, $"{ago} {story.Memory}.");
        }

        static string Together(int agoRank, Tone tone)
        {
            switch (agoRank)
            {
                case 5: return "Two years together today. You're stuck with me.";
                case 4: return tone == Tone.Dry ? "One year together today. Best year of my life. Also my only one." : "One year together today. Thank you for every break.";
                case 3: return "100 days together. I counted. I'm a rock, I have time.";
                case 2: return tone == Tone.Dry ? "A month of you. Time flies when you're a rock." : "One month together today!";
                default: return tone == Tone.Dry ? "A whole week of me. Lucky you." : "One week together today. I like it here.";
            }
        }

        static string Longest(string ago, string length, Tone tone) =>
            tone == Tone.Cheer ? $"{ago} you sat for {length} straight. Let's not do that again, okay?"
            : tone == Tone.Dry ? $"{ago}: {length} without getting up. Never again."
            : tone == Tone.Worry ? $"{ago} you sat {length} in a row. I still think about it."
            : $"{ago} you sat for {length} straight. Your back remembers too.";
    }
}
