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
            Color.FromArgb(0xC8, 0x3E, 0x3E),
            Color.FromArgb(0x3F, 0x8C, 0x55),
            Color.FromArgb(0x3D, 0x6F, 0xB8),
            Color.FromArgb(0xD8, 0x8A, 0x2A),
        };
        public static readonly string[] ShapeNames = { "circle", "square", "triangle", "star" };
        public const int TileCount = 9;

        static readonly string[] Words = { "COMPUTER", "KEYBOARD", "MONITOR", "WINDOW", "BREAK", "CHAIR", "SCREEN", "TIMER", "STRETCH", "WALKING" };
        const string CodeLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        static readonly string[] Phrases =
        {
            "I choose to stand up and stretch",
            "My body needs a short walk",
            "I am taking a mindful break",
            "Breathe in, breathe out",
            "Time to rest my eyes",
            "I will move for a few minutes",
        };

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
            r => TypePhrase(r),
            r => CountLetters(r),
            r => new HoldPuzzle(r.Next(4, 7)),
            r => new SliderPuzzle(r),
            r => new MatchingPairPuzzle(r),
            r => new ArrowPuzzle(r),
            r => new BiggestCirclePuzzle(r),
            r => MissingLetters(r),
            r => UpsideDown(r),
        };

        static readonly string[] CalmWords = { "BREATHE", "STRETCH", "BALANCE", "MOUNTAIN", "SUNRISE", "GRATITUDE", "STILLNESS", "PRESENCE", "WALKING", "PATIENCE" };

        static Puzzle MissingLetters(Random r)
        {
            string word = CalmWords[r.Next(CalmWords.Length)];
            var hidden = Enumerable.Range(1, word.Length - 1).OrderBy(_ => r.Next()).Take(2).ToList();
            string shown = new string(word.Select((c, i) => hidden.Contains(i) ? '_' : c).ToArray());
            return new AnswerPuzzle("Fill in the missing letters and type the whole word", Big(shown), word, true);
        }

        static Puzzle UpsideDown(Random r)
        {
            string word = CalmWords[r.Next(CalmWords.Length)];
            return new AnswerPuzzle("This word is upside down. Type it the right way up.", () => new UpsideDownText(word), word, true);
        }

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
            Font = Theme.SemiBold(22f),
            ForeColor = Theme.Ink,
            Anchor = AnchorStyles.None,
            Margin = new Padding(3, 3, 3, 14),
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

        static Puzzle TypePhrase(Random r)
        {
            string phrase = Phrases[r.Next(Phrases.Length)];
            var shown = new Label
            {
                Text = phrase,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                Font = Theme.SemiBold(14f),
                ForeColor = Theme.Ink,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(3, 3, 3, 14),
            };
            return new AnswerPuzzle("Type this sentence", shown, phrase, true);
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
            var field = new UnderlineBox(14f, 220, HorizontalAlignment.Center) { Anchor = AnchorStyles.None, Margin = new Padding(0, 4, 0, 0) };
            box = field.Box;
            panel.Controls.Add(field, 0, 1);
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
        public override string Prompt => "Click 1 to 9 in order";

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
            var ring = new HoldRing { Size = new Size(156, 156), Anchor = AnchorStyles.None };
            var timer = new Timer { Interval = 40 };
            var watch = new Stopwatch();
            timer.Tick += (s, e) =>
            {
                double progress = watch.Elapsed.TotalSeconds / seconds;
                if (progress < 1)
                {
                    ring.Set((float)progress, watch.Elapsed.TotalSeconds.ToString("0.0") + "s");
                    return;
                }
                done = true;
                timer.Stop();
                ring.Set(1f, "DONE");
                ring.Cursor = Cursors.Default;
            };
            ring.MouseDown += (s, e) => { if (!done && e.Button == MouseButtons.Left) { watch.Restart(); timer.Start(); } };
            EventHandler release = (s, e) => { if (done) return; timer.Stop(); watch.Reset(); ring.Set(0f, "HOLD"); };
            ring.MouseUp += (s, e) => release(s, e);
            ring.MouseLeave += release;
            ring.Disposed += (s, e) => timer.Dispose();
            return ring;
        }

        public override bool Solved() => done;
    }

    sealed class SliderPuzzle : Puzzle
    {
        StoneSlider slider;

        public SliderPuzzle(Random r)
        {
            Target = r.Next(11, 90);
            if (Target % 10 == 0) Target++;
            Start = Target < 50 ? r.Next(70, 101) : r.Next(0, 31);
        }

        public int Target { get; }
        public int Start { get; }
        public StoneSlider Slider => slider;
        public override string Prompt => $"Move the slider to exactly {Target}";

        public override Control Build()
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.None };
            var value = Puzzles.Big(Start.ToString());
            slider = new StoneSlider { Minimum = 0, Maximum = 100, Size = new Size(260, 32), Anchor = AnchorStyles.None };
            slider.Value = Start;
            slider.ValueChanged += (s, e) => value.Text = slider.Value.ToString();
            panel.Controls.Add(value, 0, 0);
            panel.Controls.Add(slider, 0, 1);
            return panel;
        }

        public override bool Solved() => slider != null && slider.Value == Target;
    }

    sealed class MatchingPairPuzzle : Puzzle
    {
        readonly List<Picture> pictures;
        TileGrid grid;

        public MatchingPairPuzzle(Random r)
        {
            var all = Enumerable.Range(0, Puzzles.ShapeNames.Length * Puzzles.Colors.Length)
                .Select(i => new Picture { Shape = (Shape)(i / Puzzles.Colors.Length), Color = i % Puzzles.Colors.Length })
                .OrderBy(_ => r.Next()).Take(Puzzles.TileCount - 1).ToList();
            var twin = all[0];
            all.Add(new Picture { Shape = twin.Shape, Color = twin.Color });
            pictures = all.OrderBy(_ => r.Next()).ToList();
            Pair = pictures.Select((p, i) => p.Same(twin) ? i : -1).Where(i => i >= 0).ToArray();
        }

        public int[] Pair { get; }
        public TileGrid Grid => grid;
        public override string Prompt => "Find the two pictures that are exactly the same";

        public override Control Build()
        {
            grid = new TileGrid(PickMode.Many, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++) grid.Tiles[i].Picture = pictures[i];
            return grid;
        }

        public override bool Solved() => grid != null && grid.Tiles.Select((t, i) => t.Selected == Pair.Contains(i)).All(ok => ok);
    }

    sealed class ArrowPuzzle : Puzzle
    {
        static readonly string[] Names = { "UP", "RIGHT", "DOWN", "LEFT" };
        readonly int[] directions = new int[Puzzles.TileCount];
        readonly int[] colors = new int[Puzzles.TileCount];
        TileGrid grid;

        public ArrowPuzzle(Random r)
        {
            Target = r.Next(4);
            int matches = r.Next(2, 5);
            var order = Enumerable.Range(0, Puzzles.TileCount).OrderBy(_ => r.Next()).ToArray();
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                directions[order[i]] = i < matches ? Target : (Target + 1 + r.Next(3)) % 4;
                colors[i] = r.Next(Puzzles.Colors.Length);
            }
        }

        public int Target { get; }
        public int[] Directions => directions;
        public TileGrid Grid => grid;
        public override string Prompt => $"Select every arrow pointing {Names[Target]}";

        public override Control Build()
        {
            grid = new TileGrid(PickMode.Many, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                grid.Tiles[i].Picture = new Picture { Shape = Shape.Triangle, Color = colors[i] };
                grid.Tiles[i].Rotation = directions[i] * 90;
            }
            return grid;
        }

        public override bool Solved() => grid != null && grid.Tiles.Select((t, i) => t.Selected == (directions[i] == Target)).All(ok => ok);
    }

    sealed class BiggestCirclePuzzle : Puzzle
    {
        static readonly float[] Smaller = { 0.3f, 0.38f, 0.44f, 0.5f };
        readonly float[] scales = new float[Puzzles.TileCount];
        readonly int[] colors = new int[Puzzles.TileCount];
        TileGrid grid;

        public BiggestCirclePuzzle(Random r)
        {
            AnswerIndex = r.Next(Puzzles.TileCount);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                scales[i] = i == AnswerIndex ? 0.72f : Smaller[r.Next(Smaller.Length)];
                colors[i] = r.Next(Puzzles.Colors.Length);
            }
        }

        public int AnswerIndex { get; }
        public TileGrid Grid => grid;
        public override string Prompt => "Click the biggest circle";

        public override Control Build()
        {
            grid = new TileGrid(PickMode.One, 1f);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                grid.Tiles[i].Picture = new Picture { Shape = Shape.Circle, Color = colors[i] };
                grid.Tiles[i].PictureScale = scales[i];
            }
            return grid;
        }

        public override bool Solved() => grid != null && grid.Tiles.Select((t, i) => t.Selected == (i == AnswerIndex)).All(ok => ok);
    }

    sealed class UpsideDownText : Control
    {
        public UpsideDownText(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Font = Theme.SemiBold(22f);
            Size = new Size(260, 56);
            Margin = new Padding(3, 3, 3, 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.TranslateTransform(Width, Height);
            g.RotateTransform(180);
            using (var brush = new SolidBrush(Theme.Ink))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(Text, Font, brush, new RectangleF(0, 0, Width, Height), format);
        }
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
        static readonly Color Accent = Theme.Accent;
        bool selected;

        public Tile(bool clickable)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            if (clickable) Cursor = Cursors.Hand;
        }

        public Picture Picture { get; set; }
        public string Label { get; set; }
        public Color LabelColor { get; set; } = Theme.Ink;
        public int Order { get; set; }
        public float PictureScale { get; set; } = 0.56f;
        public float Rotation { get; set; }

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
            g.Clear(Parent?.BackColor ?? Theme.Background);
            float border = Selected ? Math.Max(2.5f, bounds.Width * 0.035f) : 0f;
            var r = new RectangleF(border / 2, border / 2, bounds.Width - border - 1, bounds.Height - border - 1);
            using (var path = Draw.Rounded(r, bounds.Width * 0.08f))
            {
                using (var brush = new SolidBrush(Selected ? Theme.TileSelected : Theme.Tile)) g.FillPath(brush, path);
                if (Selected) using (var pen = new Pen(Accent, border)) g.DrawPath(pen, path);
            }

            if (Picture != null) DrawPicture(g, bounds);
            if (Label != null)
            {
                float points = (Label.Length <= 2 ? bounds.Height * 0.3f : bounds.Height * 0.15f) * 72f / g.DpiY;
                using (var font = Theme.SemiBold(points))
                    TextRenderer.DrawText(g, Label, font, bounds, LabelColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            if (Selected) DrawCheck(g, bounds);
        }

        void DrawPicture(Graphics g, Rectangle bounds)
        {
            float size = bounds.Width * PictureScale;
            var box = new RectangleF((bounds.Width - size) / 2, (bounds.Height - size) / 2, size, size);
            var state = g.Save();
            if (Rotation != 0)
            {
                g.TranslateTransform(bounds.Width / 2f, bounds.Height / 2f);
                g.RotateTransform(Rotation);
                g.TranslateTransform(-bounds.Width / 2f, -bounds.Height / 2f);
            }
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
            g.Restore(state);
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
