using System;
using System.Collections.Generic;

namespace Stillpoint
{
    [Flags]
    enum DayPart { Night = 1, Twilight = 2, Day = 4, Any = 7 }

    enum Damp { Any, Needs, NeedsByDay }

    sealed class Habitat
    {
        public int From = 1, To = 12;
        public DayPart When = DayPart.Any;
        public float MinTemp = -30, MaxTemp = 45, MaxWind = 1;
        public bool Rain, SnowFall, SnowGround = true;
        public Damp Damp;
    }

    struct Env
    {
        public int Month;
        public DayPart Part;
        public float Temp, Wind;
        public bool Raining, SnowFalling, SnowGround, Wet;
    }

    static class Habitats
    {
        const DayPart Dark = DayPart.Night | DayPart.Twilight;
        static readonly Habitat Slimy = new Habitat { From = 3, To = 10, MinTemp = 6, MaxTemp = 28, Rain = true, SnowGround = false, Damp = Damp.NeedsByDay };

        static readonly Dictionary<string, Habitat> All = new Dictionary<string, Habitat>
        {
            [Visitors.Bird] = new Habitat { When = DayPart.Day | DayPart.Twilight, MinTemp = -20, MaxWind = 0.8f },
            [Visitors.Butterfly] = new Habitat { From = 4, To = 9, When = DayPart.Day, MinTemp = 13, SnowGround = false, MaxWind = 0.55f },
            [Visitors.Dragonfly] = new Habitat { From = 6, To = 9, When = DayPart.Day, MinTemp = 18, SnowGround = false, MaxWind = 0.5f },
            [Visitors.Bee] = new Habitat { From = 3, To = 9, When = DayPart.Day, MinTemp = 12, SnowGround = false, MaxWind = 0.6f },
            [Visitors.Ladybug] = new Habitat { From = 4, To = 9, When = DayPart.Day, MinTemp = 12, SnowGround = false, MaxWind = 0.7f },
            [Visitors.Moth] = new Habitat { From = 4, To = 10, When = Dark, MinTemp = 8, SnowGround = false, MaxWind = 0.6f },
            [Visitors.Bat] = new Habitat { From = 4, To = 10, When = Dark, MinTemp = 8, SnowGround = false, MaxWind = 0.6f },
            [Visitors.Owl] = new Habitat { When = Dark, MaxWind = 0.8f },
            [Visitors.Fox] = new Habitat { SnowFall = true },
            [Visitors.Cat] = new Habitat { MinTemp = -5, MaxWind = 0.9f },
            [Visitors.Squirrel] = new Habitat { When = DayPart.Day, MinTemp = -15, MaxWind = 0.8f },
            [Visitors.Rabbit] = new Habitat { When = Dark, MaxWind = 0.8f },
            [Visitors.Mouse] = new Habitat { When = DayPart.Night, MinTemp = -10 },
            [Visitors.Worm] = new Habitat { From = 3, To = 11, MinTemp = 4, MaxTemp = 26, Rain = true, SnowGround = false, Damp = Damp.Needs },
            [Visitors.Snail] = Slimy,
            [Visitors.Snails] = Slimy,
            [Visitors.Frog] = new Habitat { From = 3, To = 10, MinTemp = 8, MaxTemp = 32, Rain = true, SnowGround = false, Damp = Damp.NeedsByDay },
            [Visitors.Turtle] = new Habitat { From = 5, To = 9, When = DayPart.Day, MinTemp = 18, SnowGround = false, MaxWind = 0.8f },
            [Visitors.Hedgehog] = new Habitat { From = 4, To = 10, When = Dark, MinTemp = 6, SnowGround = false },
            [Visitors.Duckling] = new Habitat { From = 4, To = 7, When = DayPart.Day, MinTemp = 10, Rain = true, SnowGround = false, MaxWind = 0.8f },
        };

        static readonly float[] Typical = { 2, 4, 9, 14, 19, 23, 26, 26, 21, 14, 8, 3 };

        public static bool Has(string id) => All.ContainsKey(id);

        public static bool Fits(string id, Env e)
        {
            if (!All.TryGetValue(id, out var h)) return true;
            bool inMonths = h.From <= h.To ? e.Month >= h.From && e.Month <= h.To : e.Month >= h.From || e.Month <= h.To;
            if (!inMonths || (h.When & e.Part) == 0) return false;
            if (e.Temp < h.MinTemp || e.Temp > h.MaxTemp || e.Wind > h.MaxWind) return false;
            if (e.Raining && !h.Rain || e.SnowFalling && !h.SnowFall || e.SnowGround && !h.SnowGround) return false;
            if (h.Damp == Damp.Needs && !e.Wet) return false;
            if (h.Damp == Damp.NeedsByDay && e.Part == DayPart.Day && !e.Wet) return false;
            return true;
        }

        public static DayPart PartAt(DateTime now, SunTimes sun)
        {
            var rise = sun.Valid ? sun.Rise : now.Date.AddHours(6);
            var set = sun.Valid ? sun.Set : now.Date.AddHours(21);
            if (now < rise.AddMinutes(-60) || now > set.AddMinutes(60)) return DayPart.Night;
            if (now <= rise.AddMinutes(60) || now >= set.AddMinutes(-60)) return DayPart.Twilight;
            return DayPart.Day;
        }

        public static float TypicalTemp(DateTime now, DayPart part) =>
            Typical[Garden.SeasonMonth(now) - 1] - (part == DayPart.Night ? 5 : part == DayPart.Twilight ? 2 : 0);

        public static Env Hour(DateTime time, SunTimes sun, WeatherNow weather, Func<DateTime, bool> rainedAt)
        {
            var part = PartAt(time, sun);
            var hour = weather?.At(time);
            bool rain = rainedAt(time);
            return new Env
            {
                Month = Garden.SeasonMonth(time),
                Part = part,
                Temp = hour != null && !float.IsNaN(hour.Value.Temp) ? hour.Value.Temp : TypicalTemp(time, part),
                Raining = rain && Garden.SeasonOf(time) != Season.Winter,
                SnowFalling = hour != null && hour.Value.Snow > 0.05f,
                SnowGround = Garden.SeasonOf(time) == Season.Winter,
                Wet = rain || rainedAt(time.AddHours(-1)) || rainedAt(time.AddHours(-2)),
            };
        }
    }
}
