using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;

namespace Stillpoint
{
    static class Sounds
    {
        public const string Off = "none";
        public const string Default = "tibetan-bowl";
        const string Prefix = "Stillpoint.Sounds.";
        const string Extension = ".wav";

        static readonly string[] Preferred =
        {
            "tibetan-bowl", "meditative-bell-long", "soft-chime", "gentle-bell-ping", "pleasant-alert", "bright-notification", "ui-popup",
        };

        static SoundPlayer player;

        public static IReadOnlyList<string> All { get; } = Load();

        static List<string> Load()
        {
            var found = typeof(Sounds).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal) && n.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Substring(Prefix.Length, n.Length - Prefix.Length - Extension.Length))
                .ToList();
            return found.OrderBy(n => Array.IndexOf(Preferred, n) is int i && i >= 0 ? i : Preferred.Length).ThenBy(n => n, StringComparer.Ordinal).ToList();
        }

        public static string Normalize(string id) =>
            id == Off || All.Contains(id) ? id : All.Contains(Default) ? Default : All.FirstOrDefault() ?? Off;

        public static string Label(string id)
        {
            if (id == Off) return "No sound";
            if (id == "ui-popup") return "UI pop-up";
            if (id == "meditative-bell-long") return "Long meditative bell";
            string words = id.Replace('-', ' ');
            return CultureInfo.InvariantCulture.TextInfo.ToUpper(words[0]) + words.Substring(1);
        }

        public static void Play(string id)
        {
            if (id == Off) return;
            try
            {
                using (var stream = typeof(Sounds).Assembly.GetManifestResourceStream(Prefix + id + Extension))
                {
                    if (stream == null)
                    {
                        SystemSounds.Asterisk.Play();
                        return;
                    }
                    var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    buffer.Position = 0;
                    player?.Stop();
                    player?.Dispose();
                    player = new SoundPlayer(buffer);
                    player.Play();
                }
            }
            catch (Exception)
            {
                SystemSounds.Asterisk.Play();
            }
        }
    }
}
