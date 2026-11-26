using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Stillpoint
{
    public enum Chattiness { Quiet, Normal, Chatty }

    public enum WindowSize { Small, Medium, Large, ExtraLarge }

    public enum DarkMode { Off, On, Night, Windows }

    public sealed class Settings
    {
        public const int MinReminderMinutes = 5;
        public const int MaxReminderMinutes = 720;
        public const int MaxNameLength = 20;

        public bool Running;
        public int ReminderMinutes = 30;
        public bool StoneOn = true;
        public DarkMode DarkMode = DarkMode.Off;
        public bool SeasonalColors = true;

        public static string Label(DarkMode mode) =>
            mode == DarkMode.On ? "On" : mode == DarkMode.Night ? "At night" : mode == DarkMode.Windows ? "Like Windows" : "Off";
        public int Defaults = 2;
        public string Sound = Sounds.Default;
        public bool FirstRunDone;
        public bool TrayTipShown;
        public string StoneName = "";
        public bool StoneNameAsked;
        public Chattiness Chattiness = Chattiness.Normal;
        public bool StoneSounds;
        public bool DesktopBuddy;
        public string EveningCardDate = "";
        public WindowSize WindowSize = WindowSize.Medium;
        public bool AutoUpdate = true;
        public bool RealWeather;
        public string TownName = "";
        public double TownLat = double.NaN, TownLon = double.NaN;
        public DateTime LastSeen = DateTime.MinValue;
        public string FirstSnow = "";

        public bool HasTown => !double.IsNaN(TownLat) && !double.IsNaN(TownLon);

        public static float Scale(WindowSize size) =>
            size == WindowSize.Small ? 0.85f : size == WindowSize.Large ? 1.2f : size == WindowSize.ExtraLarge ? 1.4f : 1f;

        public static string Label(WindowSize size) =>
            size == WindowSize.ExtraLarge ? "Extra large" : size.ToString();

        public static string CleanName(string name)
        {
            string clean = new string((name ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
            return clean.Length > MaxNameLength ? clean.Substring(0, MaxNameLength).Trim() : clean;
        }
    }

    public sealed class Store
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const long MaxLogBytes = 1024 * 1024;

        public Store(string dir)
        {
            Dir = dir;
            Directory.CreateDirectory(dir);
        }

        public string Dir { get; }
        const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        public string SessionsPath => Path.Combine(Dir, "sessions.csv");
        string SettingsPath => Path.Combine(Dir, "settings.ini");
        string AlbumPath => Path.Combine(Dir, "album.csv");

        public Album LoadAlbum() => Album.Parse(ReadLines(AlbumPath) ?? ReadLines(AlbumPath + ".bak"));

        public void SaveAlbum(Album album)
        {
            WriteAtomic(AlbumPath, album.Format());
            album.Dirty = false;
        }
        string LogPath => Path.Combine(Dir, "error.log");

        public List<Session> LoadSessions() =>
            ReadSessions(SessionsPath) ?? ReadSessions(SessionsPath + ".bak") ?? new List<Session>();

        public void SaveSessions(IEnumerable<Session> sessions)
        {
            var text = new StringBuilder("start,end,seconds,length\r\n");
            foreach (var s in sessions.OrderBy(s => s.Start))
            {
                text.Append(s.Start.ToString(TimeFormat, Inv)).Append(',')
                    .Append(s.End.ToString(TimeFormat, Inv)).Append(',')
                    .Append(Math.Round(s.Seconds).ToString(Inv)).Append(',')
                    .Append(Fmt.Clock(Math.Round(s.Seconds))).Append("\r\n");
            }
            WriteAtomic(SessionsPath, text.ToString());
        }

        public Settings LoadSettings()
        {
            var settings = new Settings();
            var saved = ReadLines(SettingsPath);
            if (saved != null) settings.Defaults = 1;
            foreach (var line in saved ?? new string[0])
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                bool on = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                switch (key)
                {
                    case "running": settings.Running = on; break;
                    case "reminderMinutes":
                        if (int.TryParse(value, NumberStyles.Integer, Inv, out int minutes))
                            settings.ReminderMinutes = Math.Max(Settings.MinReminderMinutes, Math.Min(Settings.MaxReminderMinutes, minutes));
                        break;
                    case "sound": settings.Sound = value; break;
                    case "firstRunDone": settings.FirstRunDone = on; break;
                    case "trayTipShown": settings.TrayTipShown = on; break;
                    case "stoneName": settings.StoneName = Settings.CleanName(value); break;
                    case "stoneNameAsked": settings.StoneNameAsked = on; break;
                    case "chattiness":
                        if (Enum.TryParse(value, true, out Chattiness chat) && Enum.IsDefined(typeof(Chattiness), chat)) settings.Chattiness = chat;
                        break;
                    case "stoneSounds": settings.StoneSounds = on; break;
                    case "desktopBuddy": settings.DesktopBuddy = on; break;
                    case "eveningCard": settings.EveningCardDate = value; break;
                    case "autoUpdate": settings.AutoUpdate = on; break;
                    case "stone": settings.StoneOn = on; break;
                    case "seasonalColors": settings.SeasonalColors = on; break;
                    case "darkMode":
                        if (Enum.TryParse(value, true, out DarkMode dark) && Enum.IsDefined(typeof(DarkMode), dark)) settings.DarkMode = dark;
                        break;
                    case "defaults":
                        if (int.TryParse(value, NumberStyles.Integer, Inv, out int defaults)) settings.Defaults = defaults;
                        break;
                    case "realWeather": settings.RealWeather = on; break;
                    case "town": settings.TownName = value; break;
                    case "townLat":
                        if (double.TryParse(value, NumberStyles.Float, Inv, out double lat)) settings.TownLat = lat;
                        break;
                    case "townLon":
                        if (double.TryParse(value, NumberStyles.Float, Inv, out double lon)) settings.TownLon = lon;
                        break;
                    case "lastSeen":
                        if (DateTime.TryParseExact(value, TimeFormat, Inv, DateTimeStyles.None, out var seen)) settings.LastSeen = seen;
                        break;
                    case "firstSnow": settings.FirstSnow = value; break;
                    case "windowSize":
                        if (Enum.TryParse(value, true, out WindowSize size) && Enum.IsDefined(typeof(WindowSize), size)) settings.WindowSize = size;
                        break;
                }
            }
            if (settings.Defaults < 2)
            {
                if (settings.ReminderMinutes == 60) settings.ReminderMinutes = 30;
                settings.Defaults = 2;
            }
            return settings;
        }

        public void SaveSettings(Settings s)
        {
            string Flag(bool b) => b ? "1" : "0";
            var lines = new[]
            {
                "running=" + Flag(s.Running),
                "reminderMinutes=" + s.ReminderMinutes.ToString(Inv),
                "sound=" + s.Sound,
                "firstRunDone=" + Flag(s.FirstRunDone),
                "trayTipShown=" + Flag(s.TrayTipShown),
                "stoneName=" + Settings.CleanName(s.StoneName),
                "stoneNameAsked=" + Flag(s.StoneNameAsked),
                "chattiness=" + s.Chattiness.ToString().ToLowerInvariant(),
                "stoneSounds=" + Flag(s.StoneSounds),
                "desktopBuddy=" + Flag(s.DesktopBuddy),
                "eveningCard=" + s.EveningCardDate,
                "windowSize=" + s.WindowSize.ToString().ToLowerInvariant(),
                "autoUpdate=" + Flag(s.AutoUpdate),
                "stone=" + Flag(s.StoneOn),
                "darkMode=" + s.DarkMode.ToString().ToLowerInvariant(),
                "seasonalColors=" + Flag(s.SeasonalColors),
                "defaults=" + s.Defaults.ToString(Inv),
                "realWeather=" + Flag(s.RealWeather),
                "town=" + (s.TownName ?? "").Replace("\r", "").Replace("\n", ""),
                "townLat=" + (s.HasTown ? s.TownLat.ToString("R", Inv) : ""),
                "townLon=" + (s.HasTown ? s.TownLon.ToString("R", Inv) : ""),
                "lastSeen=" + (s.LastSeen == DateTime.MinValue ? "" : s.LastSeen.ToString(TimeFormat, Inv)),
                "firstSnow=" + s.FirstSnow,
            };
            WriteAtomic(SettingsPath, string.Join("\r\n", lines) + "\r\n");
        }

        public void Log(object error)
        {
            try
            {
                var info = new FileInfo(LogPath);
                if (info.Exists && info.Length > MaxLogBytes) return;
                File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Inv) + " " + error + "\r\n");
            }
            catch (Exception)
            {
            }
        }

        static List<Session> ReadSessions(string path)
        {
            var lines = ReadLines(path);
            if (lines == null) return null;
            var sessions = new List<Session>();
            int bad = 0;
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("start", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = line.Split(',', ';');
                if (parts.Length >= 3
                    && DateTime.TryParseExact(parts[0].Trim(), TimeFormat, Inv, DateTimeStyles.None, out var start)
                    && DateTime.TryParseExact(parts[1].Trim(), TimeFormat, Inv, DateTimeStyles.None, out var end)
                    && double.TryParse(parts[2].Trim(), NumberStyles.Float, Inv, out var seconds)
                    && seconds >= 0)
                {
                    sessions.Add(new Session { Start = start, End = end, Seconds = seconds });
                }
                else
                {
                    bad++;
                }
            }
            return sessions.Count == 0 && bad > 0 ? null : sessions;
        }

        static string[] ReadLines(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(text);
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak", true);
            else File.Move(tmp, path);
        }
    }
}
