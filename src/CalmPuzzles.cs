using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class CanvasPuzzle : Puzzle
    {
        readonly string prompt;
        readonly Func<PuzzleCanvas> make;

        public CanvasPuzzle(string prompt, Func<PuzzleCanvas> make)
        {
            this.prompt = prompt;
            this.make = make;
        }

        public PuzzleCanvas Canvas { get; private set; }
        public override string Prompt => prompt;
        public override Control Build() => Canvas = make();
        public override bool Solved() => Canvas != null && Canvas.Done;

        public static CanvasPuzzle Breath(Random r)
        {
            bool biggest = r.Next(2) == 0;
            return new CanvasPuzzle(biggest ? "Click the circle when it is at its biggest" : "Click the circle when it is at its smallest", () => new BreathCanvas(biggest));
        }

        public static CanvasPuzzle Enso() => new CanvasPuzzle("Draw a circle in one stroke", () => new EnsoCanvas());

        public static CanvasPuzzle Cairn(Random r) => new CanvasPuzzle("Drag the stone onto the top of the pile", () => new CairnCanvas(r));

        public static CanvasPuzzle Lotus(Random r) => new CanvasPuzzle("Wait for the lotus to open, then click it", () => new LotusCanvas(r));

        public static CanvasPuzzle Tea(Random r) => new CanvasPuzzle("Hold to pour. Let go between the two lines.", () => new TeaCanvas(r));
    }

    static class PuzzleLayout
    {
        public static TableLayoutPanel Column(params Control[] rows)
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.None, Margin = new Padding(0) };
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Anchor = AnchorStyles.None;
                panel.Controls.Add(rows[i], 0, i);
            }
            return panel;
        }

        public static Label Note(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Regular(9f),
            Margin = new Padding(0, 6, 0, 2),
        };

        public static bool OnlySelected(TileGrid grid, int index) =>
            grid != null && grid.Tiles.Select((t, i) => t.Selected == (i == index)).All(ok => ok);
    }

    sealed class ShadowPuzzle : Puzzle
    {
        readonly Thing target;
        readonly Thing[] options;

        public ShadowPuzzle(Random r)
        {
            var things = Enum.GetValues(typeof(Thing)).Cast<Thing>().OrderBy(_ => r.Next()).ToArray();
            target = things[0];
            options = things.Take(3).OrderBy(_ => r.Next()).ToArray();
            AnswerIndex = Array.IndexOf(options, target);
        }

        public int AnswerIndex { get; }
        public TileGrid Grid { get; private set; }
        public override string Prompt => $"Which shadow belongs to the {Art.ThingNames[(int)target]}?";

        public override Control Build()
        {
            var shown = new Tile(false) { Size = new Size(104, 104), Margin = new Padding(0, 0, 0, 10), Art = Art.Of(target, false), PictureScale = 0.8f };
            Grid = new TileGrid(PickMode.One, 1f, 3, 3);
            for (int i = 0; i < options.Length; i++)
            {
                Grid.Tiles[i].Art = Art.Of(options[i], true);
                Grid.Tiles[i].PictureScale = 0.74f;
            }
            return PuzzleLayout.Column(shown, Grid);
        }

        public override bool Solved() => PuzzleLayout.OnlySelected(Grid, AnswerIndex);
    }

    sealed class DifferencePuzzle : Puzzle
    {
        readonly Picture[] original;
        readonly Picture[] changed;

        public DifferencePuzzle(Random r)
        {
            original = Enumerable.Range(0, Puzzles.TileCount).Select(_ => Puzzles.RandomPicture(r)).ToArray();
            changed = original.Select(p => new Picture { Shape = p.Shape, Color = p.Color }).ToArray();
            AnswerIndex = r.Next(Puzzles.TileCount);
            var p0 = changed[AnswerIndex];
            if (r.Next(2) == 0) p0.Color = (p0.Color + 1 + r.Next(Puzzles.Colors.Length - 1)) % Puzzles.Colors.Length;
            else p0.Shape = (Shape)(((int)p0.Shape + 1 + r.Next(Puzzles.ShapeNames.Length - 1)) % Puzzles.ShapeNames.Length);
        }

        public int AnswerIndex { get; }
        public TileGrid Grid { get; private set; }
        public override string Prompt => "One square on the right is different. Click it.";

        public override Control Build()
        {
            var left = new TileGrid(PickMode.None, 0.46f);
            Grid = new TileGrid(PickMode.One, 0.46f);
            for (int i = 0; i < Puzzles.TileCount; i++)
            {
                left.Tiles[i].Picture = original[i];
                Grid.Tiles[i].Picture = changed[i];
            }
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 2, Anchor = AnchorStyles.None, Margin = new Padding(0) };
            var leftNote = PuzzleLayout.Note("Original");
            var rightNote = PuzzleLayout.Note("Click the change");
            leftNote.Anchor = rightNote.Anchor = AnchorStyles.None;
            left.Margin = new Padding(0, 0, 14, 0);
            panel.Controls.Add(left, 0, 0);
            panel.Controls.Add(Grid, 1, 0);
            panel.Controls.Add(leftNote, 0, 1);
            panel.Controls.Add(rightNote, 1, 1);
            return panel;
        }

        public override bool Solved() => PuzzleLayout.OnlySelected(Grid, AnswerIndex);
    }

    sealed class OrderPuzzle : Puzzle
    {
        static readonly (string Prompt, Action<Graphics, RectangleF, int> Draw, Color? Background)[] Sets =
        {
            ("Click the moons from thinnest to fullest", Art.MoonPhase, Art.Night),
            ("Click the suns from lowest to highest", Art.SunHeight, Art.Sky),
            ("Click from seed to flower", Art.PlantStage, null),
            ("Click the cups from empty to full", Art.CupLevel, null),
            ("Click the stones from smallest to biggest", Art.StoneSize, null),
        };

        readonly int set;

        public OrderPuzzle(Random r)
        {
            set = r.Next(Sets.Length);
            Stages = Enumerable.Range(0, 3).OrderBy(_ => r.Next()).ToArray();
            if (Stages.SequenceEqual(new[] { 0, 1, 2 })) Stages = new[] { 2, 0, 1 };
        }

        public int[] Stages { get; }
        public TileGrid Grid { get; private set; }
        public override string Prompt => Sets[set].Prompt;

        public override Control Build()
        {
            var draw = Sets[set].Draw;
            Grid = new TileGrid(PickMode.InOrder, 1f, 3, 3);
            for (int i = 0; i < 3; i++)
            {
                int stage = Stages[i];
                var tile = Grid.Tiles[i];
                tile.Art = (g, b) => draw(g, b, stage);
                tile.Background = Sets[set].Background;
                tile.PictureScale = 0.8f;
                tile.Order = stage + 1;
            }
            return Grid;
        }

        public override bool Solved() => Grid != null && Grid.OrderDone;
    }

    sealed class MemoryPuzzle : Puzzle
    {
        const int ShowMilliseconds = 2500;
        readonly bool pickNew;
        readonly Picture[] shown;
        readonly Picture[] options;

        public MemoryPuzzle(Random r)
        {
            pickNew = r.Next(2) == 0;
            var all = Enumerable.Range(0, Puzzles.ShapeNames.Length * Puzzles.Colors.Length)
                .Select(i => new Picture { Shape = (Shape)(i / Puzzles.Colors.Length), Color = i % Puzzles.Colors.Length })
                .OrderBy(_ => r.Next()).ToList();
            shown = all.Take(3).ToArray();
            var unseen = all.Skip(3).ToList();
            var choices = pickNew
                ? shown.Concat(unseen.Take(1)).ToList()
                : shown.Take(1).Concat(unseen.Take(3)).ToList();
            var answer = pickNew ? unseen[0] : shown[0];
            options = choices.OrderBy(_ => r.Next()).ToArray();
            AnswerIndex = Array.IndexOf(options, answer);
        }

        public int AnswerIndex { get; }
        public TileGrid Grid { get; private set; }
        public bool Asking => Grid != null;
        public override string Prompt => pickNew ? "Remember the shapes, then click the new one" : "Remember the shapes, then click one you saw";

        public override Control Build()
        {
            Grid = null;
            var panel = new TableLayoutPanel { Size = new Size(290, 200), ColumnCount = 1, RowCount = 2, Anchor = AnchorStyles.None, Margin = new Padding(0) };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var step = PuzzleLayout.Note("Remember these");
            step.Anchor = AnchorStyles.None;
            step.Font = Theme.SemiBold(10f);
            step.ForeColor = Theme.Ink;
            var look = new TileGrid(PickMode.None, 0.9f, 3, 3);
            for (int i = 0; i < 3; i++) look.Tiles[i].Picture = shown[i];
            panel.Controls.Add(step, 0, 0);
            panel.Controls.Add(look, 0, 1);
            var timer = new Timer { Interval = ShowMilliseconds };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Ask(panel, step, look);
            };
            panel.Disposed += (s, e) => timer.Dispose();
            timer.Start();
            return panel;
        }

        public void Ask(TableLayoutPanel panel, Label step, Control look)
        {
            if (Grid != null) return;
            panel.SuspendLayout();
            panel.Controls.Remove(look);
            look.Dispose();
            Grid = new TileGrid(PickMode.One, 0.74f, 4, 4);
            for (int i = 0; i < options.Length; i++) Grid.Tiles[i].Picture = options[i];
            float factor = panel.Width / 290f;
            Grid.Scale(new SizeF(factor, factor));
            step.Text = pickNew ? "Which one is new?" : "Which one did you see?";
            panel.Controls.Add(Grid, 0, 1);
            panel.ResumeLayout(true);
        }

        public override bool Solved() => PuzzleLayout.OnlySelected(Grid, AnswerIndex);
    }

    sealed class UprightPuzzle : Puzzle
    {
        readonly Thing thing;
        readonly int start;

        public UprightPuzzle(Random r)
        {
            thing = (Thing)r.Next(Art.ThingNames.Length);
            start = r.Next(1, 4) * 90;
        }

        public Tile Picture { get; private set; }
        public override string Prompt => $"Click the {Art.ThingNames[(int)thing]} to turn it until it stands upright";

        public override Control Build()
        {
            Picture = new Tile(true) { Size = new Size(160, 160), Art = Art.Of(thing, false), PictureScale = 0.8f, Rotation = start };
            Picture.Click += (s, e) =>
            {
                Picture.Rotation = (Picture.Rotation + 90) % 360;
                Picture.Invalidate();
            };
            return PuzzleLayout.Column(Picture, PuzzleLayout.Note("Each click turns it a quarter"));
        }

        public override bool Solved() => Picture != null && Picture.Rotation % 360 == 0;
    }
}
