using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillpoint
{
    sealed class Story
    {
        public string Id, Label, Text, Memory;
        public bool Night;
        public int FromMonth = 1, ToMonth = 12;
    }

    static class Stories
    {
        public static readonly Story[] All =
        {
            new Story { Id = Visitors.Balloon, Label = "Red balloon", Text = "A red balloon got caught on my sprout. I kept it for a while.", Memory = "a red balloon got stuck on my sprout" },
            new Story { Id = Visitors.Plane, Label = "Paper plane", Text = "A paper plane landed next to me. It said: take a break.", Memory = "a paper plane landed in our garden" },
            new Story { Id = Visitors.Traveler, Label = "Traveling pebble", Text = "A traveling pebble stopped to rest. It was off to see the sea.", Memory = "a traveling pebble rested here on its way to the sea" },
            new Story { Id = Visitors.Duckling, Label = "Lost duckling", Text = "A lost duckling waddled in. Its mom came to get it.", Memory = "a lost duckling found its mom in our garden", FromMonth = 4, ToMonth = 8 },
            new Story { Id = Visitors.Hedgehog, Label = "Hedgehog", Text = "A hedgehog snuffled through the garden looking for snacks.", Memory = "a hedgehog came by looking for snacks", Night = true, FromMonth = 4, ToMonth = 11 },
        };

        public static Story Of(string id) => All.FirstOrDefault(s => s.Id == id);

        public static bool Is(string id) => Of(id) != null;

        public static bool Fits(Story story, DateTime now, bool night) =>
            story.Night == night && now.Month >= story.FromMonth && now.Month <= story.ToMonth;

        public static List<(DateTime Day, Story Story)> Log(Album album, int max) =>
            album == null ? new List<(DateTime, Story)>()
            : All.SelectMany(s => album.DaysWith(s.Id).Select(d => (d, s))).OrderByDescending(x => x.d).ThenBy(x => Array.IndexOf(All, x.s)).Take(max).ToList();

        public static int Count(Album album) => album == null ? 0 : All.Sum(s => album.DaysWith(s.Id).Count());
    }
}
