using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Stillpoint
{
    public sealed class Plant
    {
        public float X, Y;
        public int Kind;
        public DateTime At;
    }

    public sealed class Touches
    {
        public const int MaxPlants = 5, PlantDays = 36, WiltDays = 30;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        public readonly List<Plant> Plants = new List<Plant>();

        public static int KindFor(DateTime date) => (int)Garden.SeasonOf(date);

        public static int Stage(int breaks) => breaks <= 0 ? 0 : breaks < 3 ? 1 : breaks < 6 ? 2 : breaks < 10 ? 3 : 4;

        public bool Prune(DateTime now)
        {
            int before = Plants.Count;
            Plants.RemoveAll(p => (now - p.At).TotalDays > PlantDays || p.At > now.AddMinutes(5));
            while (Plants.Count > MaxPlants) Plants.RemoveAt(0);
            return Plants.Count != before;
        }

        public static Touches Parse(IEnumerable<string> lines)
        {
            var touches = new Touches();
            foreach (var raw in lines ?? new string[0])
            {
                var parts = raw.Trim().Split('|');
                if (parts.Length < 3 || !DateTime.TryParseExact(parts[1], TimeFormat, Inv, DateTimeStyles.None, out var at)) continue;
                if (parts[0] == "plant" && parts.Length >= 5
                    && float.TryParse(parts[2], NumberStyles.Float, Inv, out var px) && float.TryParse(parts[3], NumberStyles.Float, Inv, out var py)
                    && int.TryParse(parts[4], NumberStyles.Integer, Inv, out var kind) && kind >= 0 && kind <= 3
                    && !float.IsNaN(px) && !float.IsNaN(py) && Math.Abs(px) < 400 && Math.Abs(py) < 400)
                    touches.Plants.Add(new Plant { X = px, Y = py, Kind = kind, At = at });
            }
            while (touches.Plants.Count > MaxPlants) touches.Plants.RemoveAt(0);
            return touches;
        }

        public string Format()
        {
            var text = new StringBuilder();
            foreach (var p in Plants)
                text.Append("plant|").Append(p.At.ToString(TimeFormat, Inv)).Append('|').Append(p.X.ToString("0.#", Inv)).Append('|')
                    .Append(p.Y.ToString("0.#", Inv)).Append('|').Append(p.Kind.ToString(Inv)).Append("\r\n");
            return text.ToString();
        }
    }
}
