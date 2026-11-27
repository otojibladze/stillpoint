using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    sealed class TalkContext
    {
        public Tone Tone;
        public bool Running, Rain, Snow, Fog, Windy;
        public double SessionSeconds, Interval, RoamLeft;
        public int BreaksToday, BreaksYesterday, Streak, TotalBreaks, Weeds, Stars, TreeDays = -1, FlowerNeeds = -1;
        public Keepsake Keepsakes;
        public DateTime Now;
        public Season Season;
        public string Name = "";
        public IReadOnlyCollection<string> SeenToday = new string[0];
        public IReadOnlyCollection<Friend> Friends = new Friend[0];
    }

    static class Talk
    {
        static string S(int n, string one, string many) => n == 1 ? "1 " + one : n + " " + many;

        static IEnumerable<(string Key, string Text, float Weight)> Facts(TalkContext c)
        {
            string day = c.Now.ToString("yyyyMMdd");
            string K(string k) => "f:" + k + day;
            bool dry = c.Tone == Tone.Dry, worry = c.Tone == Tone.Worry;
            if (c.BreaksToday > 0 && c.BreaksYesterday > 0)
            {
                if (c.BreaksToday > c.BreaksYesterday) yield return (K("vs"), dry ? $"{S(c.BreaksToday, "break", "breaks")} today. Yesterday's {c.BreaksYesterday} is jealous." : $"{S(c.BreaksToday, "break", "breaks")} today. That beats yesterday's {c.BreaksYesterday}.", 3);
                else if (c.Now.Hour >= 14 && c.Now.Hour < 20) yield return (K("vs"), $"{S(c.BreaksToday, "break", "breaks")} so far. Yesterday you had {c.BreaksYesterday}. Plenty of day left.", 2);
            }
            if (!c.Running && c.Streak >= 2) yield return (K("streak" + c.Streak), dry ? $"{c.Streak} good breaks in a row. Suspicious. Keep it up." : $"That's {c.Streak} good breaks in a row. I'm proud of us.", 3);
            if (c.Running && c.Interval > 0 && c.SessionSeconds >= c.Interval * 0.8)
                yield return (K("seat" + (int)(c.SessionSeconds / 1800)), dry ? $"{Fmt.Hm(c.SessionSeconds)} in that chair. It's starting to like you too much." : worry ? $"{Fmt.Hm(c.SessionSeconds)} already. Please get up soon." : $"{Fmt.Hm(c.SessionSeconds)} in this seat. Your legs want a vote.", 3);
            if (c.BreaksToday < Garden.FlowerBreaks && c.Now.Hour < 21)
            {
                int left = Garden.FlowerBreaks - c.BreaksToday;
                yield return (K("bloom"), c.Season == Season.Winter ? $"{S(left, "more break", "more breaks")} today and I build a snowman. And a new star goes up." : $"{S(left, "more break", "more breaks")} today and my flower blooms. Then a new star lights up.", 3);
            }
            if ((c.Keepsakes & Keepsake.Lantern) == 0 && Garden.LanternBreaks - c.TotalBreaks is int l && l > 0 && l <= 15)
                yield return (K("lantern"), $"{S(l, "more break", "more breaks")} and the garden gets a stone lantern.", 3);
            else if ((c.Keepsakes & Keepsake.Pond) == 0 && Garden.PondBreaks - c.TotalBreaks is int p && p > 0 && p <= 20)
                yield return (K("pond"), $"{S(p, "more break", "more breaks")} until we get a pond. I've always wanted a pond.", 3);
            if (c.TreeDays == 0) yield return (K("tree"), "I planted our tree today. It's tiny. Like a hopeful hair.", 2);
            else if (c.TreeDays > 0) yield return (K("tree"), dry ? $"Our tree is {S(c.TreeDays, "day", "days")} old. It grows faster than my patience." : $"Our tree is {S(c.TreeDays, "day", "days")} old now. A little taller every day.", 2);
            if (c.Stars > 0 && c.Stars % Constellation.Size != 0)
            {
                int lit = c.Stars % Constellation.Size;
                yield return (K("stars"), $"{S(lit, "star", "stars")} in our constellation. {Constellation.Size - lit} more and I get to name it.", 2);
            }
            if (c.Weeds > 0) yield return (K("weeds" + c.Weeds), $"{S(c.Weeds, "weed", "weeds")} left. Each break pulls one.", 3);
            if (c.FlowerNeeds > 0) yield return (K("plant" + c.FlowerNeeds), c.FlowerNeeds == 1 ? "One more break and the seed you planted blooms." : $"The seed you planted needs {c.FlowerNeeds} more breaks to bloom.", 3);
            if (c.Running && c.RoamLeft >= 300) yield return (K("roam" + (int)(c.RoamLeft / 300)), $"You've saved {(int)(c.RoamLeft / 60)} minutes of free roam. A walk would be nice.", 2);
            var named = c.Friends.Where(f => f.Named && !string.IsNullOrEmpty(f.Name)).ToList();
            foreach (var f in named.Where(f => c.SeenToday.Contains(f.Species)).Take(2))
                yield return (K("seen" + f.Species), dry ? $"{f.Name} stopped by today. We're popular." : $"It was nice to see {f.Name} today.", 2);
            var missing = named.Where(f => !c.SeenToday.Contains(f.Species)).OrderByDescending(f => f.Visits).FirstOrDefault();
            if (missing != null) yield return (K("miss" + missing.Species), $"{missing.Name} hasn't come by today. Maybe later.", 1.5f);
            var close = named.Where(f => f.Trust != Trust.Family).OrderBy(f => Friends.Needed(f.Trust) - f.Visits).FirstOrDefault();
            if (close != null && Friends.Needed(close.Trust) - close.Visits <= 2)
                yield return (K("trust" + close.Species), $"{close.Name} is close to trusting us more. A couple more visits.", 2);
            int animals = c.SeenToday.Count(Visitors.IsAnimal);
            if (animals >= 4) yield return (K("busy"), $"{animals} visitors today. Busy garden.", 2);
            if (c.Rain) yield return (K("rain"), dry ? "Rain. Nature's way of saying stay inside. You already do." : "Rainy day. A good day for a stretch by the window.", 2);
            if (c.Snow) yield return (K("snow"), "Snow makes everything so quiet.", 2);
            if (c.Fog) yield return (K("fog"), "Foggy. I can barely see the hills.", 2);
            if (c.Windy) yield return (K("wind"), dry ? "Windy. I'm the only one here who isn't moving." : "Windy out there. The tree is dancing.", 2);
            var dow = c.Now.DayOfWeek;
            if (dow == DayOfWeek.Monday && c.Now.Hour < 13) yield return (K("dow"), dry ? "Monday. My least favorite day. Of the ones I know about." : "Monday. A slow start is allowed.", 1.5f);
            else if (dow == DayOfWeek.Friday && c.Now.Hour >= 12) yield return (K("dow"), "Friday afternoon. Almost there.", 1.5f);
            else if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) yield return (K("dow"), dry ? "It's the weekend. Are you even supposed to be here?" : "Weekend. Don't forget to rest a little.", 1.5f);
            if (!string.IsNullOrEmpty(c.Name) && c.Name != "Pebble") yield return (K("name"), $"{c.Name}. I still like my name.", 1);
        }

        static readonly Dictionary<Tone, string[]> Free = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[]
            {
                "Do clouds ever sit down?", "Nothing to do. Perfect.", "I like this sand.", "You're doing great, by the way.", "Breaks look good on you.", "I could get used to this.",
                "If I had arms, I'd wave.", "Today smells like sand and good decisions.", "I like it when you're up and about.", "Fun fact: rocks love company.",
                "Wiggle your toes. Trust me.", "Look at you, standing like a pro.", "Every break makes the garden a little happier.", "I'm humming on the inside.",
                "A good stretch is like a hug from yourself.", "We make a good team, you and me.",
                "Look up. The sky is doing something nice.", "Every step counts. Even the small ones.", "You moving makes me happy. Weird, I know.", "I saved you a sunny spot.", "Some fresh air would suit you.", "Today is going well. I can tell.",
            },
            [Tone.Plain] = new[]
            {
                "I've sat here for 3000 years. You don't have to.", "Do clouds ever sit down?", "I wonder what the fox does all day.", "Sand has a lot of patience.",
                "Some days I count the grains. Not today.", "Rocks don't get bored. We get thoughtful.", "I wonder if the moon knows about us.", "The wind keeps changing its mind.",
                "I think the snail is the real boss here.", "A break is a small holiday.", "I like the quiet between visitors.", "Somewhere, another rock is thinking about me. Probably.",
                "The garden looks different every hour if you look.", "Time moves strangely for a rock.",
                "The hills look closer when the air is clear.", "Every grain of sand was a mountain once.", "I think the moss is planning something.", "A slow day is still a day.", "Listen. You can hear the wind in the grass.", "Some rocks travel. I prefer to stay.",
            },
            [Tone.Dry] = new[]
            {
                "I've sat here for 3000 years. You don't have to.", "Oh look, you're standing. Historic.", "Nothing to do. Like my career.", "I'm a rock. Being dramatic is all I have.",
                "Take your time. I'm not going anywhere. Literally.", "Fun fact: I have no legs. You do.", "My hobbies include sitting and also sitting.", "I'd roll my eyes, but I'd have to roll everything.",
                "The squirrel thinks it owns the place. Cute.", "I'm not lazy. I'm geologically patient.", "Another day of being a rock. Nailed it.", "If you need me, I'll be right here. Forever.",
                "The fox gave me a look today. I gave it a look back.", "I'm one break away from being impressed.",
                "I've been here since before chairs were invented. Overrated.", "Wow, legs. Must be nice.", "I'd go for a walk too. If I could. Which I can't.", "Look at you, using your knees like a professional.", "The snail moves faster than my career.", "I'm not saying I'm the best rock. I'm saying it quietly.",
            },
            [Tone.Worry] = new[]
            {
                "Is your back okay? Honestly?", "You stood up! I was getting worried.", "Drink some water. Please.", "I keep thinking about your posture.", "Stay up a bit longer. For me.",
                "How are your eyes? Be honest.", "I like seeing you move around.", "Did you eat something today?", "Go look out a window. Just for a minute.",
                "Your shoulders look lighter now. Good.", "Small breaks add up. I counted.", "I worry less when you're up.",
                "Stretch your back. Gently.", "You look a little tired. Rest your eyes.", "Fresh air helps. Even a little.", "Thank you for taking care of yourself.", "Take it slow. There's no rush.", "Have a snack if you're hungry. Please.",
            },
        };

        static readonly Dictionary<Tone, string[]> Sitting = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[]
            {
                "Blink a few times. Screens are dry.", "Still here. Still cheering for you.", "How's your back?", "You've got this. Break soon.",
                "Quick shoulder roll? I'll do one too. In spirit.", "You're focused. I like it. Don't forget to blink.", "Sit up tall for a second. Nice.",
                "Nice focus. Your eyes deserve a break soon.", "You're on a roll. Don't forget your body.", "Look away from the screen for a moment. Ahh.", "Big work energy today.",
            },
            [Tone.Plain] = new[]
            {
                "Blink a few times. Screens are dry.", "Still here. Still sitting.", "How's your back?", "I'll let you know when it's time.",
                "Working hard. I'll keep watch.", "The clouds are moving. You're not. Just noting it.",
                "The garden is keeping busy without you.", "I'm counting the minutes. Quietly.", "Your screen is bright. The sky is brighter.", "A break is coming. I can feel it.",
            },
            [Tone.Dry] = new[]
            {
                "Still sitting. Bold strategy.", "Your chair loves you. Too much.", "I'd stretch if I had a spine.", "Another hour? Sure. Why not.", "Blinking is free, you know.",
                "We're both sitting. But I'm a rock. What's your excuse?", "Your posture called. It wants a word.", "Plot twist: you stand up.",
                "I've seen glaciers move faster.", "Your chair filed for overtime.", "Fun game: stand up. That's the game.", "Even I'm getting stiff watching you.",
            },
            [Tone.Worry] = new[]
            {
                "How long has it been? Too long.", "Your shoulders are up again.", "Please blink. I'm begging.", "I'm not nagging. I'm worried.", "When did you last stand up?",
                "Your neck must be tired. Mine would be. If I had one.", "Just a short walk soon? Please?",
                "Are your eyes dry? They look dry.", "Your back is curving. I can see it from here.", "Breathe. Slowly. There you go.", "Remember water? Your body does.",
            },
        };

        static readonly string[] Tips =
        {
            "Eye trick: look at something far away for 20 seconds.", "Roll your wrists. Both ways. Ten times.", "Drop your shoulders. Lower. There.", "Breathe in for 4, out for 6. I'll wait.",
            "Feet flat on the floor. Nice and steady.", "A sip of water would be good right now.", "Unclench your jaw. I saw that.", "Tilt your head to each side. Slowly.",
            "Sit back in your chair. Let it hold you for a bit.", "Stretch your fingers wide, then make a fist. Twice.",
        };

        static readonly string[] Morning = { "Mornings are my favorite. Everything is new.", "The sand is still cool from the night.", "Morning light makes the garden look brand new.", "Did you have breakfast? Rocks don't, but you should." };
        static readonly string[] Afternoon = { "The afternoon slump is real. A short walk helps.", "Halfway through the day. Nice work so far.", "The shadows are getting longer.", "Afternoons are for slow thoughts." };
        static readonly string[] Evening = { "The light is going golden.", "Evening already. Where did the day go?", "The garden gets quiet in the evening.", "Wind down soon. Tomorrow is another day." };
        static readonly string[] Late = { "It's getting late. Screens make sleep harder.", "Night garden. Everything is resting but us.", "The moon is keeping me company. And you." };

        static readonly Dictionary<Season, string[]> Seasonal = new Dictionary<Season, string[]>
        {
            [Season.Spring] = new[] { "Spring. Everything is waking up.", "I can smell new grass.", "The birds are busy this time of year." },
            [Season.Summer] = new[] { "Summer. My top gets warm by noon.", "Long days. More time for breaks.", "Summer nights are the best for stars." },
            [Season.Autumn] = new[] { "Autumn leaves keep landing on me. I don't mind.", "The squirrel is hiding acorns again.", "Crisp air. Good for a walk." },
            [Season.Winter] = new[] { "Winter. I'm a very cold rock right now.", "Snow is just quiet rain.", "Short days. Catch the light while it's here." },
        };

        static string[] DayPart(DateTime now) => now.Hour >= 5 && now.Hour < 11 ? Morning : now.Hour < 17 && now.Hour >= 11 ? Afternoon : now.Hour < 22 && now.Hour >= 17 ? Evening : Late;

        static readonly Dictionary<Tone, string[]> Petted = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[] { "Mmm. That's nice.", "Purr... wait, I'm a rock.", "Best human ever.", "More please.", "Right there. Perfect.", "I'm glowing. Can you tell?", "You have warm hands.", "This is my favorite part of the day." },
            [Tone.Plain] = new[] { "Mmm. That's nice.", "Purr... wait, I'm a rock.", "Thank you.", "That's the spot.", "I'll allow it.", "Smooth, right? Three thousand years of polishing." },
            [Tone.Dry] = new[] { "Fine. You may continue.", "Don't tell the fox I like this.", "Purr. Happy now?", "I'm only letting you because I can't move.", "This changes nothing between us.", "Okay. A little to the left." },
            [Tone.Worry] = new[] { "Oh, I needed that.", "Thank you. I was tense.", "That helps. Really.", "You're kind to me.", "I feel calmer now.", "Stay a moment?" },
        };

        static readonly Dictionary<Tone, string[]> Poked = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[] { "Hehe.", "That tickles.", "Hi!", "Boop.", "Again! No, wait.", "Hello to you too!", "Poke war? You'll lose." },
            [Tone.Plain] = new[] { "Hehe.", "Hi.", "Boop.", "Yes?", "I'm awake. Mostly.", "Hello there." },
            [Tone.Dry] = new[] { "Rude.", "Yes? Can I help you?", "I felt that.", "Boop. Very mature.", "Do I poke you?", "I'm filing a complaint.", "Wow. Touching a rock. Bold." },
            [Tone.Worry] = new[] { "Ah! Oh, it's you.", "You startled me.", "Hi. Everything okay?", "Oh! Hello.", "Is something wrong?", "I'm here. I'm here." },
        };

        static (string Key, string Text) Choose(List<(string Key, string Text, float Weight)> options, Func<string, int> ago, Random rnd)
        {
            var pool = options.Where(o => ago(o.Key) == int.MaxValue).ToList();
            if (pool.Count == 0)
            {
                var lines = options.Where(o => !o.Key.StartsWith("f:")).OrderByDescending(o => ago(o.Key)).ToList();
                pool = lines.Take(Math.Max(1, lines.Count / 3)).ToList();
            }
            float total = pool.Sum(o => o.Weight), r = (float)rnd.NextDouble() * total;
            foreach (var o in pool)
                if ((r -= o.Weight) <= 0) return (o.Key, o.Text);
            var last = pool[pool.Count - 1];
            return (last.Key, last.Text);
        }

        static IEnumerable<(string Key, string Text, float Weight)> Lines(string kind, IEnumerable<string> lines, float weight) =>
            lines.Select(l => (kind + ":" + Hash(l), l, weight));

        public static (string Key, string Text) Next(TalkContext c, Func<string, int> ago, Random rnd)
        {
            var options = Facts(c).ToList();
            var bank = c.Running ? Sitting : Free;
            options.AddRange(Lines("m", bank[c.Tone], 1));
            var shared = bank[c.Tone == Tone.Plain ? Tone.Cheer : Tone.Plain];
            options.AddRange(Lines("m", shared.Except(bank[c.Tone]), 0.4f));
            if (c.Running) options.AddRange(Lines("t", Tips, 0.8f));
            options.AddRange(Lines("d", DayPart(c.Now), 0.7f));
            options.AddRange(Lines("s", Seasonal[c.Season], 0.5f));
            return Choose(options, ago, rnd);
        }

        public static (string Key, string Text) Pet(Tone tone, Func<string, int> ago, Random rnd) => Choose(Lines("p", Petted[tone], 1).ToList(), ago, rnd);

        public static (string Key, string Text) Poke(Tone tone, Func<string, int> ago, Random rnd) => Choose(Lines("k", Poked[tone], 1).ToList(), ago, rnd);

        public static string Hash(string text)
        {
            uint h = 2166136261;
            foreach (char ch in text) h = (h ^ ch) * 16777619;
            return h.ToString("x8");
        }
    }
}
