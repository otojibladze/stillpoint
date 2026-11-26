using System;
using System.Collections.Generic;

namespace Stillpoint
{
    static class Voice
    {
        static readonly Dictionary<Tone, string[]> Free = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[] { "Do clouds ever sit down?", "Nothing to do. Perfect.", "I like this sand.", "Roll your shoulders. I'll wait.", "Water. Have you had some today?", "You're doing great, by the way.", "Breaks look good on you.", "I could get used to this." },
            [Tone.Plain] = new[] { "I've sat here for 3000 years. You don't have to.", "Do clouds ever sit down?", "I wonder what the fox does all day.", "Sand has a lot of patience.", "Some days I count the grains. Not today.", "Water. Have you had some today?" },
            [Tone.Dry] = new[] { "I've sat here for 3000 years. You don't have to.", "Oh look, you're standing. Historic.", "Nothing to do. Like my career.", "I'm a rock. Being dramatic is all I have.", "Take your time. I'm not going anywhere. Literally.", "Fun fact: I have no legs. You do." },
            [Tone.Worry] = new[] { "Is your back okay? Honestly?", "You stood up! I was getting worried.", "Drink some water. Please.", "I keep thinking about your posture.", "Stay up a bit longer. For me." },
        };

        static readonly Dictionary<Tone, string[]> Sitting = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[] { "Blink a few times. Screens are dry.", "Still here. Still cheering for you.", "Unclench your jaw.", "How's your back?", "You've got this. Break soon." },
            [Tone.Plain] = new[] { "Blink a few times. Screens are dry.", "Still here. Still sitting.", "Unclench your jaw.", "How's your back?" },
            [Tone.Dry] = new[] { "Still sitting. Bold strategy.", "Your chair loves you. Too much.", "I'd stretch if I had a spine.", "Another hour? Sure. Why not.", "Blinking is free, you know." },
            [Tone.Worry] = new[] { "How long has it been? Too long.", "Your shoulders are up again.", "Please blink. I'm begging.", "I'm not nagging. I'm worried.", "When did you last stand up?" },
        };

        static readonly Dictionary<Tone, string[]> Petted = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[] { "Mmm. That's nice.", "Purr... wait, I'm a rock.", "Best human ever.", "More please." },
            [Tone.Plain] = new[] { "Mmm. That's nice.", "Purr... wait, I'm a rock." },
            [Tone.Dry] = new[] { "Fine. You may continue.", "Don't tell the fox I like this.", "Purr. Happy now?" },
            [Tone.Worry] = new[] { "Oh, I needed that.", "Thank you. I was tense." },
        };

        static readonly Dictionary<Tone, string[]> Poked = new Dictionary<Tone, string[]>
        {
            [Tone.Cheer] = new[] { "Hehe.", "That tickles.", "Hi!", "Boop." },
            [Tone.Plain] = new[] { "Hehe.", "Hi.", "Boop." },
            [Tone.Dry] = new[] { "Rude.", "Yes? Can I help you?", "I felt that.", "Boop. Very mature." },
            [Tone.Worry] = new[] { "Ah! Oh, it's you.", "You startled me.", "Hi. Everything okay?" },
        };

        static string Pick(Dictionary<Tone, string[]> bank, Tone tone, Random random)
        {
            var list = bank[tone];
            return list[random.Next(list.Length)];
        }

        public static string Thought(Tone tone, bool sitting, Random random) => Pick(sitting ? Sitting : Free, tone, random);

        public static string Pet(Tone tone, Random random) => Pick(Petted, tone, random);

        public static string Poke(Tone tone, Random random) => Pick(Poked, tone, random);

        public static string Back(Tone tone, int minutes)
        {
            if (minutes < 5 && tone == Tone.Dry) return "Back already? That was barely a break.";
            switch (tone)
            {
                case Tone.Cheer: return $"Welcome back. {minutes} min break. Nice!";
                case Tone.Dry: return $"Back. {minutes} min. I'll allow it.";
                case Tone.Worry: return $"Welcome back. {minutes} minutes. Feel better?";
                default: return $"Welcome back. {minutes} min break.";
            }
        }

        public static string Meteors(Shower shower, Tone tone) =>
            tone == Tone.Dry ? $"Shooting stars. It's {shower.Name}. Fine, make a wish." : $"It's {shower.Name} tonight. Make a wish!";

        public static string Comet(string name, Tone tone) =>
            tone == Tone.Dry ? $"A comet. I'm naming it {name}. After me. Obviously." : $"A comet! I'm naming it {name}.";

        public static string Aurora(bool real) =>
            real ? "Northern lights! They're real tonight. Look north if your sky is dark." : "Is that... the northern lights? Over our garden?";

        public static string Eclipse(Eclipse e, bool here)
        {
            if (!here) return e.Solar ? "There's a solar eclipse today, but only far from here." : "There's a lunar eclipse tonight, on the other side of the world.";
            switch (e.Kind)
            {
                case EclipseKind.LunarTotal: return "The moon is turning red. A total lunar eclipse!";
                case EclipseKind.LunarPartial: return "Something is taking a bite out of the moon. A lunar eclipse!";
                case EclipseKind.LunarPenumbral: return "The moon looks a little tired tonight. A faint eclipse.";
                default: return "Is it getting darker? It's a solar eclipse! Don't look at the sun. I can, I'm a rock.";
            }
        }
    }
}
