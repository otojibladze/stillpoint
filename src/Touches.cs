using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Stillpoint
{
    public sealed class RakeStroke
    {
        public readonly List<PointF> Points = new List<PointF>();
        public DateTime At;
    }

    public sealed class Plant
    {
        public float X, Y;
        public int Kind;
        public DateTime At;
    }

    public sealed class Touches
    {
        public const int MaxStrokes = 30, MaxPoints = 160, MaxPlants = 5, StrokeHours = 72, PlantDays = 36, WiltDays = 30;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        public readonly List<RakeStroke> Strokes = new List<RakeStroke>();
        public readonly List<Plant> Plants = new List<Plant>();

        public static int KindFor(DateTime date) => (int)Garden.SeasonOf(date);

        public static int Stage(int breaks) => breaks <= 0 ? 0 : breaks < 3 ? 1 : breaks < 6 ? 2 : breaks < 10 ? 3 : 4;

        public bool Prune(DateTime now)
        {
            int before = Strokes.Count + Plants.Count;
            Strokes.RemoveAll(s => (now - s.At).TotalHours > StrokeHours || s.At > now.AddMinutes(5) || s.Points.Count < 2);
            Plants.RemoveAll(p => (now - p.At).TotalDays > PlantDays || p.At > now.AddMinutes(5));
            while (Strokes.Count > MaxStrokes) Strokes.RemoveAt(0);
            while (Plants.Count > MaxPlants) Plants.RemoveAt(0);
            return Strokes.Count + Plants.Count != before;
        }

        public static Touches Parse(IEnumerable<string> lines)
        {
            var touches = new Touches();
            foreach (var raw in lines ?? new string[0])
            {
                var parts = raw.Trim().Split('|');
                if (parts.Length < 3 || !DateTime.TryParseExact(parts[1], TimeFormat, Inv, DateTimeStyles.None, out var at)) continue;
                if (parts[0] == "rake")
                {
                    var stroke = new RakeStroke { At = at };
                    foreach (var pair in parts[2].Split(' '))
                    {
                        var xy = pair.Split(',');
                        if (xy.Length == 2 && float.TryParse(xy[0], NumberStyles.Float, Inv, out var x) && float.TryParse(xy[1], NumberStyles.Float, Inv, out var y)
                            && !float.IsNaN(x) && !float.IsNaN(y) && Math.Abs(x) < 400 && Math.Abs(y) < 400)
                            stroke.Points.Add(new PointF(x, y));
                        if (stroke.Points.Count >= MaxPoints) break;
                    }
                    if (stroke.Points.Count >= 2) touches.Strokes.Add(stroke);
                }
                else if (parts[0] == "plant" && parts.Length >= 5
                    && float.TryParse(parts[2], NumberStyles.Float, Inv, out var px) && float.TryParse(parts[3], NumberStyles.Float, Inv, out var py)
                    && int.TryParse(parts[4], NumberStyles.Integer, Inv, out var kind) && kind >= 0 && kind <= 3
                    && !float.IsNaN(px) && !float.IsNaN(py) && Math.Abs(px) < 400 && Math.Abs(py) < 400)
                    touches.Plants.Add(new Plant { X = px, Y = py, Kind = kind, At = at });
            }
            while (touches.Strokes.Count > MaxStrokes) touches.Strokes.RemoveAt(0);
            while (touches.Plants.Count > MaxPlants) touches.Plants.RemoveAt(0);
            return touches;
        }

        public string Format()
        {
            var text = new StringBuilder();
            foreach (var s in Strokes)
                text.Append("rake|").Append(s.At.ToString(TimeFormat, Inv)).Append('|')
                    .Append(string.Join(" ", s.Points.Select(p => p.X.ToString("0.#", Inv) + "," + p.Y.ToString("0.#", Inv)))).Append("\r\n");
            foreach (var p in Plants)
                text.Append("plant|").Append(p.At.ToString(TimeFormat, Inv)).Append('|').Append(p.X.ToString("0.#", Inv)).Append('|')
                    .Append(p.Y.ToString("0.#", Inv)).Append('|').Append(p.Kind.ToString(Inv)).Append("\r\n");
            return text.ToString();
        }
    }
}
