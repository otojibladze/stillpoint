using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Stillpoint
{
    sealed class Town
    {
        public string Name;
        public double Lat, Lon;
    }

    sealed class WeatherNow
    {
        public DateTime Fetched;
        public float CloudCover, Rain, Snowfall, SnowDepth;
        public int Code;
        public List<(DateTime Hour, float Rain, float Snow)> Hours = new List<(DateTime, float, float)>();

        public bool Fresh(DateTime now) => (now - Fetched).TotalHours < 3;
        public bool Fog => Code == 45 || Code == 48;
        public bool Storm => Code >= 95;

        public float Wetness(DateTime now)
        {
            if (Rain > 0.05f) return 1;
            var last = Hours.Where(h => h.Hour <= now && h.Rain > 0.1f).Select(h => (DateTime?)h.Hour).LastOrDefault();
            if (last == null) return 0;
            return 1 - SpiritArt.Smooth(0.5f, 5, (float)(now - last.Value).TotalHours);
        }

        public (bool Rain, bool Snow) Between(DateTime from, DateTime to)
        {
            var span = Hours.Where(h => h.Hour >= from.AddHours(-1) && h.Hour <= to).ToList();
            return (span.Any(h => h.Rain > 0.1f), span.Any(h => h.Snow > 0.05f));
        }
    }

    static class RealWeather
    {
        const string Geocoding = "https://geocoding-api.open-meteo.com/v1/search?count=1&language=en&format=json&name=";
        const string Forecast = "https://api.open-meteo.com/v1/forecast?current=precipitation,rain,showers,snowfall,cloud_cover,weather_code,snow_depth&hourly=precipitation,snowfall&past_days=2&forecast_days=1&timezone=GMT";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static async Task<Town> FindTown(string name)
        {
            string json;
            using (var web = Client()) json = await web.DownloadStringTaskAsync(Geocoding + Uri.EscapeDataString(name.Trim()));
            var root = Parse(json);
            if (!(root.TryGetValue("results", out var results) && results is object[] list && list.Length > 0 && list[0] is Dictionary<string, object> first)) return null;
            string label = Text(first, "name");
            string country = Text(first, "country");
            if (!string.IsNullOrEmpty(country)) label += ", " + country;
            return new Town { Name = label, Lat = Number(first, "latitude"), Lon = Number(first, "longitude") };
        }

        public static async Task<WeatherNow> Fetch(double lat, double lon)
        {
            string url = Forecast + "&latitude=" + lat.ToString("0.####", Inv) + "&longitude=" + lon.ToString("0.####", Inv);
            string json;
            using (var web = Client()) json = await web.DownloadStringTaskAsync(url);
            var root = Parse(json);
            var weather = new WeatherNow { Fetched = DateTime.Now };
            if (root.TryGetValue("current", out var c) && c is Dictionary<string, object> current)
            {
                weather.CloudCover = (float)Number(current, "cloud_cover");
                weather.Rain = (float)(Number(current, "rain") + Number(current, "showers"));
                if (weather.Rain <= 0) weather.Rain = (float)Math.Max(0, Number(current, "precipitation") - Number(current, "snowfall"));
                weather.Snowfall = (float)Number(current, "snowfall");
                weather.SnowDepth = (float)Number(current, "snow_depth");
                weather.Code = (int)Number(current, "weather_code");
            }
            if (root.TryGetValue("hourly", out var h) && h is Dictionary<string, object> hourly
                && hourly.TryGetValue("time", out var t) && t is object[] times
                && hourly.TryGetValue("precipitation", out var p) && p is object[] rain
                && hourly.TryGetValue("snowfall", out var s) && s is object[] snow)
            {
                for (int i = 0; i < times.Length && i < rain.Length && i < snow.Length; i++)
                    if (DateTime.TryParseExact(times[i] as string, "yyyy-MM-dd'T'HH:mm", Inv, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var hour))
                        weather.Hours.Add((hour.ToLocalTime(), ToFloat(rain[i]), ToFloat(snow[i])));
            }
            return weather;
        }

        static Dictionary<string, object> Parse(string json) =>
            new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object> ?? new Dictionary<string, object>();

        static string Text(Dictionary<string, object> d, string key) => d.TryGetValue(key, out var v) ? v as string ?? "" : "";

        static double Number(Dictionary<string, object> d, string key) =>
            d.TryGetValue(key, out var v) && v != null ? Convert.ToDouble(v, Inv) : 0;

        static float ToFloat(object v) => v == null ? 0 : (float)Convert.ToDouble(v, Inv);

        static WebClient Client()
        {
            var web = new WebClient();
            web.Headers[HttpRequestHeader.UserAgent] = "Pebble/" + AppVersion.Text;
            return web;
        }
    }
}
