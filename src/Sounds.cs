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
            var bytes = Resource(id);
            if (bytes == null)
            {
                SystemSounds.Asterisk.Play();
                return;
            }
            PlayBytes(bytes);
        }

        static byte[] Resource(string id)
        {
            using (var stream = typeof(Sounds).Assembly.GetManifestResourceStream(Prefix + id + Extension))
            {
                if (stream == null) return null;
                var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        static void PlayBytes(byte[] bytes)
        {
            try
            {
                player?.Stop();
                player?.Dispose();
                player = new SoundPlayer(new MemoryStream(bytes));
                player.Play();
            }
            catch (Exception)
            {
            }
        }
    }
}
