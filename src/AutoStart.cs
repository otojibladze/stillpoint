using System.IO;
using Microsoft.Win32;

namespace Stillpoint
{
    static class AutoStart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        const string ValueName = "Stillpoint";
        const string LegacyValueName = "UsageTimer";

        public static string Command { get; set; }

        public static bool IsEnabled()
        {
            using (var run = Registry.CurrentUser.OpenSubKey(RunKey))
            {
                if (!(run?.GetValue(ValueName) is string)) return false;
            }
            using (var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey))
            {
                return !(approved?.GetValue(ValueName) is byte[] flags && flags.Length > 0 && (flags[0] & 1) != 0);
            }
        }

        public static void Set(bool enabled)
        {
            if (Command == null) return;
            using (var run = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled) run.SetValue(ValueName, Command);
                else run.DeleteValue(ValueName, false);
            }
            using (var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, true))
            {
                approved?.DeleteValue(ValueName, false);
            }
        }

        public static void MigrateLegacy()
        {
            if (Command == null) return;
            bool hadLegacy;
            using (var run = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                hadLegacy = run?.GetValue(LegacyValueName) != null;
                if (hadLegacy) run.DeleteValue(LegacyValueName, false);
            }
            using (var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, true))
            {
                approved?.DeleteValue(LegacyValueName, false);
            }
            if (hadLegacy) Set(true);
        }

        public static void RefreshPath()
        {
            if (Command == null) return;
            using (var run = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (run?.GetValue(ValueName) is string current && !File.Exists(ExePath(current)))
                    run.SetValue(ValueName, Command);
            }
        }

        static string ExePath(string command)
        {
            string trimmed = command.Trim();
            if (!trimmed.StartsWith("\"")) return trimmed.Split(' ')[0];
            int end = trimmed.IndexOf('"', 1);
            return end > 0 ? trimmed.Substring(1, end - 1) : trimmed.Trim('"');
        }
    }
}
