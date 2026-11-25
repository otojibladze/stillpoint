using System;

namespace Stillpoint
{
    static class Messages
    {
        static readonly string[] Gentle =
        {
            "Your chair called. It needs a break from you.",
            "Even monks get up sometimes.",
            "Your spine has filed a polite complaint.",
            "Downward dog is not a typo. Go do one.",
            "Plants photosynthesize. You should at least stand.",
            "Inhale. Exhale. Now do it standing up.",
            "Your feet asked if they still have a job.",
            "A tiny stretch now saves a big sigh later.",
            "Your water bottle is feeling ignored.",
            "Stand up. Your future self says thank you.",
            "The window has a view. Go check on it.",
            "Your shoulders are up by your ears again.",
            "Blink. Then blink at something far away.",
            "Mountain pose works better when you are vertical.",
            "Quick: name three things that are not a screen.",
            "Your calves are bored. Give them a walk.",
            "This is your gentle, very polite nudge.",
            "Roll your shoulders. They have been patient.",
            "One minute of standing. You can do anything for a minute.",
            "Your body is not a desk accessory.",
            "Time to remind your knees how they bend.",
            "Go refill something. Water, tea, your soul.",
            "Your posture just asked for a day off.",
            "Stretch like a cat that just woke up.",
            "The floor misses your footsteps.",
            "A short walk is basically meditation with legs.",
            "Your eyes would love a little horizon.",
            "Breathe like nobody is watching.",
            "Up you go. The screen will not run away.",
            "Your hips are whispering. Listen to them.",
            "Unclench your jaw. Yes, right now.",
            "Little break, big difference.",
            "Your chair is warm enough. Let it cool down.",
            "Wiggle your toes. Then take them for a walk.",
            "This reminder was brought to you by your spine.",
            "Sun salutation? Even half of one counts.",
            "Your neck says hello. And also ouch.",
            "Stand up and pretend you meant to all along.",
            "Shake it out. Hands, arms, worries.",
            "Your breath has been waiting for you.",
        };

        static readonly string[] Louder =
        {
            "Your legs are writing their resignation letter.",
            "At this point the chair is part of your body.",
            "Namaste... seated? That is not how it works.",
            "Your hip flexors would like a word.",
            "Breaking news: local person forgets legs exist.",
            "Even your screen needs a break from your face.",
            "Two hours. Your chair is getting attached.",
            "Your spine has escalated the complaint to management.",
            "Your feet have started a support group.",
            "The plant on your desk moved more than you today.",
            "Your blood would like to visit your legs again.",
            "You have been sitting so long the dust is jealous.",
            "Your chakras are fine. Your knees are not.",
            "Warrior pose. Not desk warrior pose.",
            "Somewhere a yoga mat is crying.",
            "Your posture has entered the shrimp phase.",
            "Your body is sending a strongly worded email.",
            "Two hours of sitting. Two minutes of moving. Deal?",
            "Your legs just asked who you are.",
            "Even a sloth takes more walks than this.",
            "Your chair called again. It wants personal space.",
            "Your shoulders are now officially earrings.",
            "You are one hour away from becoming furniture.",
            "Your inner peace needs outer movement.",
            "Mindfulness tip: notice that you have legs.",
            "The cat stretched nine times. You stretched zero.",
            "Your spine is shaped like a question mark. Answer it.",
            "Stand up before your chair learns your name.",
            "Your circulation has left the building.",
            "Your lower back is drafting a dramatic speech.",
            "Two hours. The universe recommends a walk.",
            "Breathe in calm, breathe out chair.",
            "Your water glass is empty and so is your patience.",
            "Today's lesson: legs are not decorative.",
            "Your keyboard has seen enough of you for now.",
            "Get up and do something your body will brag about.",
            "Your heart wants a little cardio gossip.",
            "Your muscles are forgetting what you look like.",
            "The tree pose is calling. Answer standing.",
            "Plot twist: you can stand up right now.",
        };

        static readonly string[] Drama =
        {
            "Your chair has started charging rent.",
            "Archaeologists may soon discover you here.",
            "Your yoga mat misses you. It said so.",
            "Day 1 of becoming a desk. Please stand.",
            "Your last stretch is now a legend. Make a new one.",
            "The tree pose is easier when you are not rooted to a chair.",
            "Your legs have filed for divorce.",
            "Your spine has hired a lawyer.",
            "Scientists are studying how long you can sit.",
            "Your chair and you are now legally one person.",
            "Moss has started growing on your north side.",
            "Your feet sent a search party. They found nothing.",
            "This is no longer sitting. This is a lifestyle.",
            "Your posture is now an abstract art piece.",
            "Your enlightenment is delayed due to sitting.",
            "Even the cursor needs to stretch at this point.",
            "Your chair is writing your biography.",
            "Your hips have gone into airplane mode.",
            "Your legs are now just for decoration. Prove them wrong.",
            "The yoga gods are watching. They are disappointed.",
            "Your body would like to speak to the manager.",
            "Fossil record update: human, seated, still typing.",
            "Your chair has asked for a raise.",
            "Even meditation retreats have walking breaks.",
            "Your knees have forgotten the word bend.",
            "Your shadow left to go for a walk without you.",
            "You have achieved one with the chair. Now achieve one with the floor.",
            "Your back is composing a sad opera.",
            "This chair has seen things. Let it rest.",
            "Your step counter thinks you moved away.",
            "Breaking: chair declares independence from you.",
            "Your inner child wants to run around. So does your outer one.",
            "Gravity is winning. Fight back. Stand up.",
            "Your spine just sent this message in all caps.",
            "Legend says you once had legs.",
            "Your chair has started calling you roommate.",
            "Om... om... omg please stand up.",
            "Your muscles have formed a union.",
            "The only thing still moving is the clock.",
            "Stand up. Do it for the chair. It is tired too.",
        };

        public static string For(double sessionSeconds, Random random)
        {
            var pool = sessionSeconds < 2 * 3600 ? Gentle : sessionSeconds < 3 * 3600 ? Louder : Drama;
            return pool[random.Next(pool.Length)];
        }

        public static string[] All => Concat(Gentle, Louder, Drama);

        static string[] Concat(params string[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var all = new string[n];
            int i = 0;
            foreach (var p in parts) { p.CopyTo(all, i); i += p.Length; }
            return all;
        }
    }
}
