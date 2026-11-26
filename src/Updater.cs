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

        public static async Task InstallAsync(string exePath, Version expected)
        {
            string incoming = exePath + ".new";
            string previous = exePath + ".old";
            try
            {
                using (var web = Client())
                    await web.DownloadFileTaskAsync(Source + "Stillpoint.exe?t=" + DateTime.UtcNow.Ticks, incoming);
                Validate(incoming, expected);
                if (File.Exists(previous)) File.Delete(previous);
                File.Move(exePath, previous);
                File.Move(incoming, exePath);
            }
            finally
            {
                if (File.Exists(incoming)) File.Delete(incoming);
            }
        }

        public static void CleanUp(string exePath)
        {
            try
            {
                if (File.Exists(exePath + ".old")) File.Delete(exePath + ".old");
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
