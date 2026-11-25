using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Stillpoint
{
    public sealed class Settings
    {
        public const int MinReminderMinutes = 5;
        public const int MaxReminderMinutes = 720;

        public bool Running;
        public int ReminderMinutes = 60;
        public bool FirstRunDone;
        public bool TrayTipShown;
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
            foreach (var line in ReadLines(SettingsPath) ?? new string[0])
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
                    case "firstRunDone": settings.FirstRunDone = on; break;
                    case "trayTipShown": settings.TrayTipShown = on; break;
                }
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
                "firstRunDone=" + Flag(s.FirstRunDone),
                "trayTipShown=" + Flag(s.TrayTipShown),
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
