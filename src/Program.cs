using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using Stillpoint;

[assembly: AssemblyTitle("Stillpoint")]
[assembly: AssemblyProduct("Stillpoint")]
[assembly: AssemblyDescription("Reminds you how long you have been sitting at the computer")]
[assembly: AssemblyVersion(AppVersion.Text + ".0")]
[assembly: AssemblyFileVersion(AppVersion.Text + ".0")]
[assembly: TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]

namespace Stillpoint
{
    static class Program
    {
        const string RunningName = @"Local\Stillpoint.Running";
        const string ShowName = @"Local\Stillpoint.Show";
        const string LegacyDataFolder = "UsageTimer";
        static readonly string[] LegacyFiles = { "settings.ini", "sessions.csv", "sessions.csv.bak" };

        [STAThread]
        static void Main(string[] args)
        {
            bool trayOnly = HasFlag(args, "--tray");
            bool continueSession = HasFlag(args, "--continue-session");
            string customDir = Value(args, "--data");
            if (customDir != null) customDir = Path.GetFullPath(customDir);
            string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = customDir ?? Path.Combine(localData, "Stillpoint");
            string extraArgs = customDir == null ? "" : $" --data \"{customDir}\"";

            if (int.TryParse(Value(args, "--wait-pid"), out int waitPid)) WaitForExit(waitPid);
            if (customDir == null) MigrateLegacyData(Path.Combine(localData, LegacyDataFolder), dir);

            using (var running = new Mutex(true, RunningName, out bool firstInstance))
            using (var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowName))
            {
                if (!firstInstance)
                {
                    if (!trayOnly)
                    {
                        Native.AllowAnyForeground();
                        showEvent.Set();
                    }
                    return;
                }

                var store = new Store(dir);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => store.Log(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => store.Log(e.ExceptionObject);
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                Updater.CleanUp(Application.ExecutablePath);
                AutoStart.Command = customDir == null ? $"\"{Application.ExecutablePath}\" --tray" : null;
                Native.RegisterRestart("--tray" + extraArgs);
                Application.Run(new TrayApp(store, !trayOnly, showEvent, continueSession, extraArgs));
            }
        }

        static bool HasFlag(string[] args, string flag) => args.Contains(flag, StringComparer.OrdinalIgnoreCase);

        static string Value(string[] args, string name)
        {
            int i = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static void WaitForExit(int pid)
        {
            try
            {
                using (var process = Process.GetProcessById(pid)) process.WaitForExit(15000);
            }
            catch (ArgumentException)
            {
            }
        }

        static void MigrateLegacyData(string legacyDir, string dir)
        {
            try
            {
                if (Directory.Exists(dir) || !Directory.Exists(legacyDir)) return;
                Directory.CreateDirectory(dir);
                foreach (var name in LegacyFiles)
                {
                    string source = Path.Combine(legacyDir, name);
                    if (File.Exists(source)) File.Copy(source, Path.Combine(dir, name));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
