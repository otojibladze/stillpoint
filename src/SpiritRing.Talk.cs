using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        readonly Dictionary<string, int> saidAt = new Dictionary<string, int>();
        int saidCount;

        int SaidAgo(string key) => saidAt.TryGetValue(key, out var at) ? saidCount - at : int.MaxValue;

        string Remember((string Key, string Text) line)
        {
            saidAt[line.Key] = ++saidCount;
            return line.Text;
        }

        int FlowerNeeds()
        {
            if (Touch == null) return -1;
            var left = Touch.Plants.Select(p => Touches.BloomBreaks - (int)PlantBreaks(p)).Where(n => n > 0).ToList();
            return left.Count == 0 ? -1 : left.Min();
        }

        TalkContext TalkNow()
        {
            var now = DateTime.Now;
            var w = state.Weather != null && state.Weather.Fresh(now) ? state.Weather : null;
            return new TalkContext
            {
                Tone = Tone,
                Running = Running,
                Rain = w != null && w.Rain > 0.02f,
                Snow = w != null && w.Snowfall > 0.01f,
                Fog = w != null && w.Fog,
                Windy = w != null && w.Windiness > 0.5f,
                SessionSeconds = state.SessionSeconds,
                Interval = state.Interval,
                RoamLeft = state.Roam.Left,
                BreaksToday = state.BreaksToday,
                BreaksYesterday = state.BreaksYesterday,
                Streak = state.Streak,
                TotalBreaks = state.TotalBreaks,
                Weeds = state.Weeds,
                Stars = state.Stars.Count,
                TreeDays = state.TreeDays,
                FlowerNeeds = FlowerNeeds(),
                Keepsakes = state.Keepsakes,
                Now = now,
                Season = season,
                Name = state.Name,
                SeenToday = state.SeenToday,
                Friends = state.Friends.Values.ToList(),
            };
        }

        string Thought() => Remember(Talk.Next(TalkNow(), SaidAgo, rng));
    }
}
