using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class NameForm : Form
    {
        readonly TextBox box;

        public NameForm(string current, bool rename)
        {
            SuspendLayout();
            Ui.SetupWindow(this, "Stillpoint", new Size(380, 260));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var root = Ui.Column(new Padding(24, 10, 24, 22));
            root.SuspendLayout();

            var stone = new MiniSpirit { Size = new Size(76, 76), Mood = MiniMood.Happy, Margin = new Padding(0, 0, 16, 0), BackColor = Theme.Background, Snow = Garden.SeasonOf(DateTime.Today) == Season.Winter };
            var title = new Label { Text = rename ? "Rename your stone" : "Meet your stone", AutoSize = true, Font = Theme.Light(18f), Margin = new Padding(0, 4, 0, 4) };
            var body = new Label { Text = "It lives in the sand garden and keeps you company while you sit.", AutoSize = true, MaximumSize = new Size(230, 0), ForeColor = Theme.Muted, Margin = new Padding(0) };
            var words = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Margin = new Padding(0) };
            words.Controls.Add(title, 0, 0);
            words.Controls.Add(body, 0, 1);
            var head = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 16) };
            head.Controls.Add(stone, 0, 0);
            head.Controls.Add(words, 1, 0);

            var ask = new Label { Text = "What should we call it?", AutoSize = true, Font = Theme.Regular(9.5f), Margin = new Padding(0, 0, 0, 6) };
            box = new TextBox { BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Ink, Font = Theme.Regular(11.5f), MaxLength = Settings.MaxNameLength, Text = string.IsNullOrEmpty(current) ? "Pebble" : current, Dock = DockStyle.Fill };
            var field = new InputFrame(box) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 16) };

            var skip = new FlatButton(ButtonKind.Ghost) { Text = rename ? "Cancel" : "Skip", Uppercase = true, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Font = Theme.SemiBold(10f) };
            var save = new FlatButton(ButtonKind.Primary) { Text = rename ? "Save" : "Name it", Uppercase = true, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10f) };
            var buttons = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0) };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            buttons.Controls.Add(skip, 0, 0);
            buttons.Controls.Add(save, 1, 0);

            Ui.AddRow(root, head);
            Ui.AddRow(root, ask);
            Ui.AddRow(root, field);
            Ui.AddRow(root, buttons);
            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
            ClientSize = new Size(ClientSize.Width, root.GetPreferredSize(new Size(ClientSize.Width, 0)).Height);

            skip.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            save.Click += (s, e) => Accept();
            AcceptButton = save;
            CancelButton = skip;
            box.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Accept(); } };
            Shown += (s, e) => { box.Focus(); box.SelectAll(); };
        }

        public string StoneName => Settings.CleanName(box.Text);

        void Accept()
        {
            if (StoneName.Length == 0)
            {
                box.Text = "Pebble";
                box.SelectAll();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }

    sealed class InputFrame : Panel
    {
        public InputFrame(TextBox box)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Padding = new Padding(12, 10, 12, 8);
            Height = 42;
            Controls.Add(box);
            box.GotFocus += (s, e) => Invalidate();
            box.LostFocus += (s, e) => Invalidate();
            Cursor = Cursors.IBeam;
            Click += (s, e) => box.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool focused = Controls.Count > 0 && Controls[0].Focused;
            using (var path = Draw.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 6))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(focused ? Theme.Ink : Theme.Border, focused ? 1.4f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }
    }
}
