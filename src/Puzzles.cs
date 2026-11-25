using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    enum Shape { Circle, Square, Triangle, Star }

    sealed class Picture
    {
        public Shape Shape;
        public int Color;

        public bool Same(Picture other) => Shape == other.Shape && Color == other.Color;
    }

    abstract class Puzzle
    {
        public abstract string Prompt { get; }
        public abstract Control Build();
        public abstract bool Solved();
    }

    static class Puzzles
    {
        public static readonly string[] ColorNames = { "red", "green", "blue", "orange" };
        public static readonly Color[] Colors =
        {
            Color.FromArgb(0xD6, 0x3A, 0x3A),
            Color.FromArgb(0x2E, 0x9E, 0x4F),
            Color.FromArgb(0x2F, 0x6F, 0xD6),
            Color.FromArgb(0xE8, 0x8A, 0x1A),
        };
        public static readonly string[] ShapeNames = { "circle", "square", "triangle", "star" };
        public const int TileCount = 9;

        static readonly string[] Words = { "COMPUTER", "KEYBOARD", "MONITOR", "WINDOW", "BREAK", "CHAIR", "SCREEN", "TIMER", "STRETCH", "WALKING" };
        const string CodeLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static readonly Func<Random, Puzzle>[] Kinds =
        {
            r => new PickShapesPuzzle(r),
            r => new OddOneOutPuzzle(r),
            r => new InkColorPuzzle(r),
            r => new NumberOrderPuzzle(r),
            r => CountShapes(r),
            r => new AnswerPuzzle("Type CONFIRM in the box", Big("CONFIRM"), "CONFIRM", false),
            r => TypeCode(r),
            r => TypeBackwards(r),
            r => MathSum(r),
            r => NextInSequence(r),
            r => CountLetters(r),
            r => new HoldPuzzle(r.Next(4, 7)),
            r => new SliderPuzzle(r),
        };

        public static Puzzle Next(Random random, ref int lastKind)
        {
            int kind;
            do kind = random.Next(Kinds.Length);
            while (kind == lastKind && Kinds.Length > 1);
            lastKind = kind;
            return Kinds[kind](random);
        }

        public static Picture RandomPicture(Random r) => new Picture { Shape = (Shape)r.Next(ShapeNames.Length), Color = r.Next(Colors.Length) };

        public static string Describe(Picture p) => ColorNames[p.Color] + " " + ShapeNames[(int)p.Shape];

        public static Label Big(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Consolas", 20F, FontStyle.Bold),
            Anchor = AnchorStyles.None,
            Margin = new Padding(3, 3, 3, 12),
        };

        static Puzzle CountShapes(Random r)
        {
            int color = r.Next(Colors.Length);
            var pictures = Enumerable.Range(0, TileCount).Select(_ => RandomPicture(r)).ToList();
            int count = pictures.Count(p => p.Color == color);
            if (count == 0)
            {
                pictures[r.Next(TileCount)].Color = color;
                count = 1;
            }
            Func<Control> grid = () =>
            {
                var g = new TileGrid(PickMode.None, 0.8f);
                for (int i = 0; i < TileCount; i++) g.Tiles[i].Picture = pictures[i];
                return g;
            };
            return new AnswerPuzzle($"How many {ColorNames[color]} shapes are there?", grid, count.ToString(), true);
        }

        static Puzzle TypeCode(Random r)
        {
            string code = new string(Enumerable.Range(0, 6).Select(_ => CodeLetters[r.Next(CodeLetters.Length)]).ToArray());
            return new AnswerPuzzle("Type this code in the box", Big(code), code, true);
        }

        static Puzzle TypeBackwards(Random r)
        {
            string word = Words[r.Next(Words.Length)];
            return new AnswerPuzzle("Type this word backwards", Big(word), new string(word.Reverse().ToArray()), true);
        }

        static Puzzle MathSum(Random r)
        {
            int a, b, answer;
            string op;
            switch (r.Next(3))
            {
                case 0: a = r.Next(12, 90); b = r.Next(12, 90); op = "+"; answer = a + b; break;
                case 1: a = r.Next(40, 100); b = r.Next(11, a - 5); op = "-"; answer = a - b; break;
                default: a = r.Next(3, 10); b = r.Next(6, 16); op = "x"; answer = a * b; break;
            }
            return new AnswerPuzzle("Solve this", Big($"{a} {op} {b} = ?"), answer.ToString(), true);
        }

        static Puzzle NextInSequence(Random r)
        {
            int start = r.Next(1, 20), step = r.Next(2, 10);
            var terms = Enumerable.Range(0, 4).Select(i => (start + i * step).ToString());
            return new AnswerPuzzle("Which number comes next?", Big(string.Join(", ", terms) + ", ?"), (start + 4 * step).ToString(), true);
        }

        static Puzzle CountLetters(Random r)
        {
            const string pool = "AEKMRST";
            char letter = pool[r.Next(pool.Length)];
            string text = new string(Enumerable.Range(0, 12).Select(_ => pool[r.Next(pool.Length)]).ToArray());
            if (text.IndexOf(letter) < 0) text = text.Substring(0, 5) + letter + text.Substring(6);
            int count = text.Count(c => c == letter);
            return new AnswerPuzzle($"How many times does the letter {letter} appear?", Big(text), count.ToString(), true);
        }
    }

    sealed class AnswerPuzzle : Puzzle
    {
        readonly string prompt;
        readonly Func<Control> display;
        readonly bool ignoreCase;
        TextBox box;

        public AnswerPuzzle(string prompt, Control display, string expected, bool ignoreCase)
            : this(prompt, () => display, expected, ignoreCase)
        {
        }

        public AnswerPuzzle(string prompt, Func<Control> display, string expected, bool ignoreCase)
        {
            this.prompt = prompt;
            this.display = display;
            Expected = expected;
            this.ignoreCase = ignoreCase;
        }

        public string Expected { get; }
        public TextBox Box => box;
        public override string Prompt => prompt;

        public override Control Build()
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.None, Margin = new Padding(0) };
            var shown = display();
            shown.Anchor = AnchorStyles.None;
            panel.Controls.Add(shown, 0, 0);
            box = new TextBox { Width = 200, Font = new Font("Segoe UI", 14F), TextAlign = HorizontalAlignment.Center, Anchor = AnchorStyles.None, ShortcutsEnabled = false };
            panel.Controls.Add(box, 0, 1);
            return panel;
        }

        public override bool Solved() =>
            box != null && string.Equals(box.Text.Trim(), Expected, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    sealed class PickShapesPuzzle : Puzzle
    {
        readonly List<Picture> pictures;
        TileGrid grid;

        public PickShapesPuzzle(Random r)
        {
            Target = Puzzles.RandomPicture(r);
            pictures = new List<Picture>();
            int matches = r.Next(2, 5);
            for (int i = 0; i < matches; i++) pictures.Add(new Picture { Shape = Target.Shape, Color = Target.Color });
            while (pictures.Count < Puzzles.TileCount)
            {
                var decoy = r.Next(3) switch
                {
                    0 => new Picture { Shape = Target.Shape, Color = r.Next(Puzzles.Colors.Length) },
                    1 => new Picture { Shape = (Shape)r.Next(Puzzles.ShapeNames.Length), Color = Target.Color },
                    _ => Puzzles.RandomPicture(r),
                };
                if (!decoy.Same(Target)) pictures.Add(decoy);
            }
            pictures = pictures.OrderBy(_ => r.Next()).ToList();
        }

        public Picture Target { get; }
        public TileGrid Grid => grid;
        public override string Prompt => "Select every " + Puzzles.Describe(Target);

        public override Control Build()
        {
            grid = new TileGrid(PickMode.Many, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++) grid.Tiles[i].Picture = pictures[i];
            return grid;
        }

        public override bool Solved() => grid != null && grid.Tiles.All(t => t.Selected == t.Picture.Same(Target));
    }

    sealed class OddOneOutPuzzle : Puzzle
    {
        readonly Picture common;
        readonly Picture odd;
        readonly int oddIndex;
        TileGrid grid;

        public OddOneOutPuzzle(Random r)
        {
            common = Puzzles.RandomPicture(r);
            odd = r.Next(2) == 0
                ? new Picture { Shape = common.Shape, Color = (common.Color + 1 + r.Next(Puzzles.Colors.Length - 1)) % Puzzles.Colors.Length }
                : new Picture { Shape = (Shape)(((int)common.Shape + 1 + r.Next(Puzzles.ShapeNames.Length - 1)) % Puzzles.ShapeNames.Length), Color = common.Color };
            oddIndex = r.Next(Puzzles.TileCount);
        }

        public int OddIndex => oddIndex;
        public TileGrid Grid => grid;
        public override string Prompt => "Click the one that is different";

        public override Control Build()
        {
            grid = new TileGrid(PickMode.One, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++) grid.Tiles[i].Picture = i == oddIndex ? odd : common;
            return grid;
        }

        public override bool Solved() => grid != null && grid.Tiles.Select((t, i) => t.Selected == (i == oddIndex)).All(ok => ok);
    }

    sealed class InkColorPuzzle : Puzzle
    {
        readonly int targetInk;
        readonly int[] inks = new int[Puzzles.TileCount];
        readonly int[] words = new int[Puzzles.TileCount];
        TileGrid grid;

        public InkColorPuzzle(Random r)
        {
            int colors = Puzzles.Colors.Length;
            targetInk = r.Next(colors);
            int answer = r.Next(Puzzles.TileCount);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                inks[i] = i == answer ? targetInk : (targetInk + 1 + r.Next(colors - 1)) % colors;
                do words[i] = r.Next(colors);
                while (words[i] == inks[i]);
            }
            AnswerIndex = answer;
        }

        public int AnswerIndex { get; }
        public TileGrid Grid => grid;
        public override string Prompt => $"Click the word written in {Puzzles.ColorNames[targetInk].ToUpperInvariant()} ink";

        public override Control Build()
        {
            grid = new TileGrid(PickMode.One, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                grid.Tiles[i].Label = Puzzles.ColorNames[words[i]].ToUpperInvariant();
                grid.Tiles[i].LabelColor = Puzzles.Colors[inks[i]];
            }
            return grid;
        }

        public override bool Solved() => grid != null && grid.Tiles.Select((t, i) => t.Selected == (i == AnswerIndex)).All(ok => ok);
    }

    sealed class NumberOrderPuzzle : Puzzle
    {
        readonly int[] numbers;
        TileGrid grid;

        public NumberOrderPuzzle(Random r)
        {
            numbers = Enumerable.Range(1, Puzzles.TileCount).OrderBy(_ => r.Next()).ToArray();
        }

        public int[] Numbers => numbers;
        public TileGrid Grid => grid;
        public override string Prompt => "Click the numbers in order, 1 to 9";

        public override Control Build()
        {
            grid = new TileGrid(PickMode.InOrder, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                grid.Tiles[i].Label = numbers[i].ToString();
                grid.Tiles[i].Order = numbers[i];
            }
            return grid;
        }

        public override bool Solved() => grid != null && grid.OrderDone;
    }

    sealed class HoldPuzzle : Puzzle
    {
        readonly int seconds;
        bool done;

        public HoldPuzzle(int seconds)
        {
            this.seconds = seconds;
        }

        public override string Prompt => $"Press and hold the button for {seconds} seconds";

        public override Control Build()
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.None };
            var button = new Button { Text = "Hold me", Size = new Size(170, 56), Font = new Font("Segoe UI Semibold", 12F), Anchor = AnchorStyles.None };
            var bar = new ProgressBar { Maximum = 1000, Size = new Size(220, 16), Anchor = AnchorStyles.None, Margin = new Padding(3, 12, 3, 3) };
            var timer = new Timer { Interval = 40 };
            var watch = new Stopwatch();
            timer.Tick += (s, e) =>
            {
                double progress = watch.Elapsed.TotalSeconds / seconds;
                bar.Value = (int)Math.Min(1000, progress * 1000);
                if (progress < 1) return;
                done = true;
                timer.Stop();
                button.Text = "Done";
                button.Enabled = false;
            };
            MouseEventHandler down = (s, e) => { if (!done && e.Button == MouseButtons.Left) { watch.Restart(); timer.Start(); } };
            EventHandler release = (s, e) => { if (done) return; timer.Stop(); watch.Reset(); bar.Value = 0; };
            button.MouseDown += down;
            button.MouseUp += (s, e) => release(s, e);
            button.MouseLeave += release;
            button.Disposed += (s, e) => timer.Dispose();
            panel.Controls.Add(button, 0, 0);
            panel.Controls.Add(bar, 0, 1);
            return panel;
        }

        public override bool Solved() => done;
    }

    sealed class SliderPuzzle : Puzzle
    {
        TrackBar slider;

        public SliderPuzzle(Random r)
        {
            Target = r.Next(11, 90);
            if (Target % 10 == 0) Target++;
            Start = Target < 50 ? r.Next(70, 101) : r.Next(0, 31);
        }

        public int Target { get; }
        public int Start { get; }
        public TrackBar Slider => slider;
        public override string Prompt => $"Move the slider to exactly {Target}";

        public override Control Build()
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.None };
            var value = Puzzles.Big(Start.ToString());
            slider = new TrackBar { Minimum = 0, Maximum = 100, Value = Start, TickFrequency = 10, Width = 260, Anchor = AnchorStyles.None };
            slider.ValueChanged += (s, e) => value.Text = slider.Value.ToString();
            panel.Controls.Add(value, 0, 0);
            panel.Controls.Add(slider, 0, 1);
            return panel;
        }

        public override bool Solved() => slider != null && slider.Value == Target;
    }

    enum PickMode { None, Many, One, InOrder }

    sealed class TileGrid : TableLayoutPanel
    {
        int nextOrder = 1;

        public TileGrid(PickMode mode, float scale)
        {
            ColumnCount = 3;
            RowCount = 3;
            AutoSize = true;
            Anchor = AnchorStyles.None;
            Margin = new Padding(0);
            int size = (int)(84 * scale);
            Tiles = new Tile[Puzzles.TileCount];
            for (int i = 0; i < Tiles.Length; i++)
            {
                var tile = new Tile(mode != PickMode.None) { Size = new Size(size, size), Margin = new Padding(4) };
                tile.Click += (s, e) => Pick(mode, tile);
                Tiles[i] = tile;
                Controls.Add(tile, i % 3, i / 3);
            }
        }

        public Tile[] Tiles { get; }
        public bool OrderDone => nextOrder > Puzzles.TileCount;

        void Pick(PickMode mode, Tile tile)
        {
            switch (mode)
            {
                case PickMode.Many:
                    tile.Selected = !tile.Selected;
                    break;
                case PickMode.One:
                    foreach (var t in Tiles) t.Selected = t == tile;
                    break;
                case PickMode.InOrder:
                    if (tile.Selected) return;
                    if (tile.Order == nextOrder)
                    {
                        tile.Selected = true;
                        nextOrder++;
                    }
                    else
                    {
                        foreach (var t in Tiles) t.Selected = false;
                        nextOrder = 1;
                    }
                    break;
            }
        }
    }

    sealed class Tile : Control
    {
        static readonly Color Accent = Color.FromArgb(0x2F, 0x6F, 0xD6);
        bool selected;

        public Tile(bool clickable)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            if (clickable) Cursor = Cursors.Hand;
        }

        public Picture Picture { get; set; }
        public string Label { get; set; }
        public Color LabelColor { get; set; } = Color.Black;
        public int Order { get; set; }

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = ClientRectangle;
            g.Clear(Selected ? Color.FromArgb(0xE3, 0xEE, 0xFB) : Color.FromArgb(0xF4, 0xF5, 0xF7));
            float border = Selected ? Math.Max(3f, bounds.Width * 0.04f) : 1f;
            using (var pen = new Pen(Selected ? Accent : Color.FromArgb(0xC8, 0xCC, 0xD2), border))
                g.DrawRectangle(pen, border / 2, border / 2, bounds.Width - border, bounds.Height - border);

            if (Picture != null) DrawPicture(g, bounds);
            if (Label != null)
            {
                float size = Label.Length <= 2 ? bounds.Height * 0.3f : bounds.Height * 0.15f;
                using (var font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(LabelColor))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(Label, font, brush, bounds, format);
            }
            if (Selected) DrawCheck(g, bounds);
        }

        void DrawPicture(Graphics g, Rectangle bounds)
        {
            float size = bounds.Width * 0.56f;
            var box = new RectangleF((bounds.Width - size) / 2, (bounds.Height - size) / 2, size, size);
            using (var brush = new SolidBrush(Puzzles.Colors[Picture.Color]))
            {
                switch (Picture.Shape)
                {
                    case Shape.Circle: g.FillEllipse(brush, box); break;
                    case Shape.Square: g.FillRectangle(brush, box.X + size * 0.06f, box.Y + size * 0.06f, size * 0.88f, size * 0.88f); break;
                    case Shape.Triangle: g.FillPolygon(brush, Points(box, 3, 0)); break;
                    default: g.FillPolygon(brush, Points(box, 5, 0.42f)); break;
                }
            }
        }

        static void DrawCheck(Graphics g, Rectangle bounds)
        {
            float r = bounds.Width * 0.12f;
            var mark = new RectangleF(bounds.Width - r * 2 - 6, 6, r * 2, r * 2);
            using (var brush = new SolidBrush(Accent)) g.FillEllipse(brush, mark);
            using (var pen = new Pen(Color.White, Math.Max(2f, r * 0.3f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLines(pen, new[]
                {
                    new PointF(mark.X + r * 0.55f, mark.Y + r * 1.05f),
                    new PointF(mark.X + r * 0.9f, mark.Y + r * 1.4f),
                    new PointF(mark.X + r * 1.5f, mark.Y + r * 0.65f),
                });
        }

        static PointF[] Points(RectangleF box, int corners, float innerRatio)
        {
            float cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2 + (corners == 3 ? box.Height * 0.08f : 0), r = box.Width / 2;
            int count = innerRatio > 0 ? corners * 2 : corners;
            var points = new PointF[count];
            for (int i = 0; i < count; i++)
            {
                float radius = innerRatio > 0 && i % 2 == 1 ? r * innerRatio : r;
                double angle = (-90 + 360.0 * i / count) * Math.PI / 180;
                points[i] = new PointF(cx + (float)(radius * Math.Cos(angle)), cy + (float)(radius * Math.Sin(angle)));
            }
            return points;
        }
    }
}
