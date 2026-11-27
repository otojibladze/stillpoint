using System;
using System.Globalization;
using System.Linq;

namespace Stillpoint
{
    sealed class Release
    {
        public string Version;
        public DateTime Date;
        public string[] Notes;
    }

    static class Changes
    {
        static Release R(string version, string date, params string[] notes) =>
            new Release { Version = version, Date = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture), Notes = notes };

        public static Release Of(string version) => All.FirstOrDefault(r => r.Version == version);

        public static readonly Release[] All =
        {
            R("3.14.0", "2026-11-27", "Pebble talks about your real day: breaks versus yesterday, the next keepsake, your tree, stars, the seed you planted, today's visitors, the weather and the time of day.", "Many more lines, health tips while you sit, and Pebble no longer repeats itself.", "A bird sitting on Pebble flies away when you click or pet the stone."),
            R("3.13.3", "2026-11-27", "Animals in the garden pictures no longer pile on top of each other."),
            R("3.13.2", "2026-11-27", "Check for updates finds a new version right away."),
            R("3.13.1", "2026-11-27", "Raking the sand is gone. Double-click the sand to plant a seed.", "The update message in Settings sits at the bottom right."),
            R("3.13.0", "2026-11-27", "Pebble dreams at night about your day, and tells you about it in the morning.", "Pebble keeps a journal. Read it on the Garden page.", "Every day your flower blooms lights a star. Twelve make a constellation, and Pebble names it.", "Touch the garden: drag to rake the sand, double-click to plant a seed that grows with your breaks, click the tree to shake it."),
            R("3.12.1", "2026-11-27", "Free roam remembers the time you used when you press Resume."),
            R("3.12.0", "2026-11-27", "New animals: a rabbit at dawn and dusk, bats on summer nights, earthworms after rain, and a little mouse at night.", "Animals notice each other: the cat stalks the bird, a fox scares off the squirrel, the owl hoots at the moth, the mouse hides from the cat.", "Seasons bring stories: fox cubs in spring, the squirrel buries acorns in autumn and digs them up in winter, frogspawn turns into tadpoles in the pond.", "Weather matters: birds hide under Pebble's leaf in the rain, the frog loves puddles, butterflies struggle on windy days."),
            R("3.11.1", "2026-11-27", "Free roam time also starts fresh after the computer shuts down or restarts."),
            R("3.11.0", "2026-11-27", "New Free roam timer next to the main timer: every 30 minutes you sit earns 5 minutes away.", "It counts down while you're up and starts fresh each day."),
            R("3.10.0", "2026-11-27", "Pebble has a daily routine: morning stretch, sunbathing, naps, sunsets and stargazing.", "A tree grows in the garden as your days together add up.", "Weeds grow while you're away. Each break pulls one.", "Pebble reacts more: wakes with a start, sulks a little when you come back, cheers good streaks and big milestones.", "Rare surprises: a balloon, a paper plane, a traveling pebble, a lost duckling, a hedgehog. Read them under Stories in the Garden."),
            R("3.9.6", "2026-11-27", "New What's new list next to Check for updates.", "Clearer update messages."),
            R("3.9.5", "2026-11-27", "History is one page: calendar on top, the day's sessions below."),
            R("3.9.4", "2026-11-27", "History shows today first. Other days are in the calendar."),
            R("3.9.3", "2026-11-27", "History scrolls as one page."),
            R("3.9.2", "2026-11-27", "Smoother page switching and scrolling.", "Garden mode label lines up with its switch."),
            R("3.9.1", "2026-11-26", "The cat walks in and curls up instead of sliding.", "Main switch now says Garden mode or Simple mode.", "Removed Your sessions from Settings."),
            R("3.9.0", "2026-11-26", "The stone gets a personality from your habits.", "It remembers special days with you.", "Animals get names as they get to know you.", "A bird builds a nest in spring.", "Real meteor showers, eclipses, a comet and northern lights.", "Fireflies and leaves fly in front of and behind the stone."),
            R("3.8.3", "2026-11-26", "Animals follow day, night and season. No butterflies at night."),
            R("3.8.2", "2026-11-26", "Scrollbar on long pages.", "Settings no longer cuts off the last row."),
            R("3.8.1", "2026-11-26", "Pages slide up and down inside the window."),
            R("3.8.0", "2026-11-26", "New side bar. Garden, History and Settings open in the main window.", "Fixed a red X after pressing Stop."),
            R("3.7.0", "2026-11-26", "New visitors: ladybug, bee, squirrel, owl, moth, fox, snails and turtle.", "Smoother stone faces."),
            R("3.6.3", "2026-11-26", "Stone sounds removed. The reminder sound stays."),
            R("3.6.2", "2026-11-26", "Petting the stone looks smoother."),
            R("3.6.1", "2026-11-26", "Cleaner stone edges."),
            R("3.6.0", "2026-11-26", "Prettier stone and garden.", "Dark mode.", "Colors that follow the season."),
            R("3.5.0", "2026-11-26", "Garden gets its own page.", "Simple mode turns Pebble into a plain timer.", "Default reminder is now 30 minutes."),
            R("3.4.0", "2026-11-26", "Signs of what happened while you were away.", "Real moon, sunrise and sunset for your town.", "Optional real weather."),
            R("3.3.1", "2026-11-26", "Fix for updates."),
            R("3.3.0", "2026-11-26", "Updates install by themselves at a quiet moment."),
            R("3.2.0", "2026-11-26", "Stillpoint is now Pebble.", "Window size setting."),
            R("3.1.1", "2026-11-26", "The stone's eyes move smoothly."),
            R("3.1.0", "2026-11-26", "A living garden: seasons, weather, visitors and keepsakes.", "Give your stone a name.", "Optional desktop buddy.", "Evening summary of your day."),
            R("3.0.1", "2026-11-26", "Smoother stone animation."),
            R("3.0.0", "2026-11-26", "Meet the stone: a little friend who lives in your timer."),
            R("1.8.1", "2026-11-26", "New Settings look."),
            R("1.8.0", "2026-11-26", "Pick a reminder sound.", "New Settings window."),
            R("1.7.2", "2026-11-26", "Stop button now says I GOT UP."),
            R("1.7.1", "2026-11-26", "Reminder buttons say KEEP SITTING and I GOT UP."),
            R("1.7.0", "2026-11-26", "History grouped by day, with a calendar."),
            R("1.6.1", "2026-11-26", "Bigger timer.", "History has its own button."),
            R("1.6.0", "2026-11-26", "No more puzzles. Stop works right away."),
            R("1.5.0", "2026-11-26", "10 new calm puzzles."),
            R("1.4.1", "2026-11-26", "Resume continues your last session."),
            R("1.4.0", "2026-11-26", "Stopping the timer closes the reminder."),
            R("1.3.9", "2026-11-26", "Small fixes."),
            R("1.3.8", "2026-11-25", "Reminder shows on the screen you are using."),
            R("1.3.7", "2026-11-25", "Session list in a rounded box."),
            R("1.3.6", "2026-11-25", "Reminder layout fix."),
            R("1.3.5", "2026-11-25", "Reminder layout fix."),
            R("1.3.4", "2026-11-25", "Reminder layout fix."),
            R("1.3.3", "2026-11-25", "Reminder layout fix."),
            R("1.3.2", "2026-11-25", "Puzzle layout fix."),
            R("1.3.1", "2026-11-25", "Puzzles open right above the reminder."),
            R("1.3.0", "2026-11-25", "Funny reminder messages."),
            R("1.2.2", "2026-11-25", "Buttons fit their text."),
            R("1.2.1", "2026-11-25", "More words for typing puzzles."),
            R("1.2.0", "2026-11-25", "Five new puzzles."),
            R("1.1.0", "2026-11-25", "New look."),
            R("1.0.3", "2026-11-25", "Typing puzzle replaces math puzzles."),
            R("1.0.2", "2026-11-25", "Reminder preview asks for the puzzle too."),
            R("1.0.1", "2026-11-25", "Small fix."),
            R("1.0.0", "2026-11-25", "First version: a sitting timer with reminders."),
        };
    }
}
