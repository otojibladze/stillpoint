using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Stillpoint
{
    static class Updater
    {
        public const string Source = "https://raw.githubusercontent.com/otojibladze/stillpoint/main/latest/";
        const long MinExeBytes = 20 * 1024;

        public static Version Current => new Version(AppVersion.Text);

        public static async Task<Version> LatestAsync()
        {
            using (var web = Client())
            {
                string text = await web.DownloadStringTaskAsync(Source + "version.txt?t=" + DateTime.UtcNow.Ticks);
                return Version.TryParse(text.Trim(), out var version) ? version : null;
            }
        }

        public static async Task DownloadAsync(string exePath, Version expected)
        {
            string partial = exePath + ".download";
            string incoming = exePath + ".new";
            try
            {
                using (var web = Client())
                    await web.DownloadFileTaskAsync(Source + "Stillpoint.exe?t=" + DateTime.UtcNow.Ticks, partial);
                Validate(partial, expected);
                if (File.Exists(incoming)) File.Delete(incoming);
                File.Move(partial, incoming);
            }
            finally
            {
                if (File.Exists(partial)) File.Delete(partial);
            }
        }

        public static void Apply(string exePath)
        {
            string incoming = exePath + ".new", previous = exePath + ".old";
            if (!File.Exists(incoming)) throw new FileNotFoundException("The update was not downloaded.");
            if (File.Exists(previous)) File.Delete(previous);
            File.Move(exePath, previous);
            try
            {
                File.Move(incoming, exePath);
            }
            catch
            {
                File.Move(previous, exePath);
                throw;
            }
        }

        public static bool TryApplyPending(string exePath)
        {
            string incoming = exePath + ".new";
            if (!File.Exists(incoming)) return false;
            try
            {
                Validate(incoming, new Version(Current.Major, Current.Minor, Current.Build + 1));
                Apply(exePath);
                return true;
            }
            catch (Exception)
            {
                Quietly(() => File.Delete(incoming));
                return false;
            }
        }

        public static bool CanRollBack(string exePath) => File.Exists(exePath + ".old");

        public static void RollBack(string exePath)
        {
            string bad = exePath + ".bad";
            if (File.Exists(bad)) File.Delete(bad);
            File.Move(exePath, bad);
            File.Move(exePath + ".old", exePath);
        }

        public static void CleanUp(string exePath)
        {
            foreach (var leftover in new[] { ".old", ".bad", ".download" })
                Quietly(() => { if (File.Exists(exePath + leftover)) File.Delete(exePath + leftover); });
        }

        static void Quietly(Action action)
        {
            try
            {
                action();
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        static void Validate(string path, Version expected)
        {
            var info = new FileInfo(path);
            if (info.Length < MinExeBytes) throw new InvalidDataException("The download is incomplete.");
            using (var stream = info.OpenRead())
            {
                if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z') throw new InvalidDataException("The download is not a program file.");
            }
            string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
            if (!Version.TryParse(fileVersion, out var actual) || actual < expected)
                throw new InvalidDataException("The new version is not ready yet. Try again in a few minutes.");
        }

        static WebClient Client()
        {
            var web = new WebClient();
            web.Headers[HttpRequestHeader.UserAgent] = "Pebble/" + AppVersion.Text;
            web.Headers[HttpRequestHeader.CacheControl] = "no-cache";
            return web;
        }
    }
}
