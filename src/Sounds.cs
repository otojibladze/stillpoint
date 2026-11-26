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

        static DateTime lastPlayed = DateTime.MinValue;
        static readonly Dictionary<StoneSound, byte[]> stoneCache = new Dictionary<StoneSound, byte[]>();

        public static void Play(string id)
        {
            if (id == Off) return;
            var bytes = Resource(id);
            if (bytes == null)
            {
                SystemSounds.Asterisk.Play();
                return;
            }
            lastPlayed = DateTime.Now;
            PlayBytes(bytes);
        }

        public static void PlayStone(StoneSound sound)
        {
            if ((DateTime.Now - lastPlayed).TotalSeconds < 8) return;
            if (!stoneCache.TryGetValue(sound, out var bytes))
            {
                bytes = sound == StoneSound.Purr ? Synth.Purr()
                    : sound == StoneSound.Crickets ? Synth.Crickets()
                    : Synth.Quieter(Resource("gentle-bell-ping") ?? Resource(All.FirstOrDefault() ?? Off), 0.35);
                stoneCache[sound] = bytes;
            }
            if (bytes != null) PlayBytes(bytes);
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

    enum StoneSound { Chime, Purr, Crickets }

    static class Synth
    {
        const int Rate = 22050;

        public static byte[] Purr()
        {
            double length = 1.9;
            var samples = new double[(int)(Rate * length)];
            var rnd = new Random(3);
            double low = 0, low2 = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / Rate;
                low += (rnd.NextDouble() * 2 - 1 - low) * 0.09;
                low2 += (low - low2) * 0.25;
                double pulse = Math.Pow(0.5 + 0.5 * Math.Sin(2 * Math.PI * 25 * t), 2.5);
                double breath = t < 0.85 ? Math.Sin(Math.PI * t / 0.85) : t > 0.95 ? 0.75 * Math.Sin(Math.PI * Math.Min(1, (t - 0.95) / 0.95)) : 0;
                double hum = Math.Sin(2 * Math.PI * 110 * t) * 0.35 + Math.Sin(2 * Math.PI * 165 * t) * 0.15;
                samples[i] = (low2 * 3.2 + hum * 0.6) * pulse * breath;
            }
            return Wav(samples, 0.45);
        }

        public static byte[] Crickets()
        {
            double length = 2.6;
            var samples = new double[(int)(Rate * length)];
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / Rate;
                double inChirp = t % 0.55;
                double env = 0;
                for (int p = 0; p < 3; p++)
                {
                    double start = p * 0.034, u = (inChirp - start) / 0.022;
                    if (u >= 0 && u <= 1) env = Math.Sin(Math.PI * u);
                }
                double fade = Math.Min(1, Math.Min(t / 0.3, (length - t) / 0.4));
                samples[i] = Math.Sin(2 * Math.PI * 4400 * t) * env * fade * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 0.7 * t));
            }
            return Wav(samples, 0.12);
        }

        public static byte[] Quieter(byte[] wav, double volume)
        {
            if (wav == null || wav.Length < 44) return wav;
            var copy = (byte[])wav.Clone();
            int bits = BitConverter.ToInt16(copy, 34), format = BitConverter.ToInt16(copy, 20);
            if (format != 1 || bits != 16) return copy;
            int pos = 12;
            while (pos + 8 <= copy.Length)
            {
                int size = BitConverter.ToInt32(copy, pos + 4);
                bool data = copy[pos] == 'd' && copy[pos + 1] == 'a' && copy[pos + 2] == 't' && copy[pos + 3] == 'a';
                if (data)
                {
                    int end = Math.Min(copy.Length - 1, pos + 8 + size);
                    for (int i = pos + 8; i + 1 < end; i += 2)
                    {
                        short v = (short)(BitConverter.ToInt16(copy, i) * volume);
                        copy[i] = (byte)(v & 0xFF);
                        copy[i + 1] = (byte)((v >> 8) & 0xFF);
                    }
                    break;
                }
                pos += 8 + size + (size & 1);
            }
            return copy;
        }

        static byte[] Wav(double[] samples, double volume)
        {
            double peak = 1e-9;
            foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                w.Write(36 + bytes);
                w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                w.Write(bytes);
                foreach (var s in samples) w.Write((short)Math.Round(s / peak * volume * 32767));
                w.Flush();
                return stream.ToArray();
            }
        }
    }
}
