using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class TownForm : Form
    {
        readonly TextBox box;
        readonly Label status;
        readonly FlatButton find;

        public TownForm(string current)
        {
            SuspendLayout();
            Ui.SetupWindow(this, "Pebble", new Size(380, 300));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var root = Ui.Column(new Padding(24, 10, 24, 22));
            root.SuspendLayout();
            var title = new Label { Text = "Your town", AutoSize = true, Font = Theme.Light(18f), Margin = new Padding(0, 4, 0, 6) };
            var body = new Label
            {
                Text = "The garden follows its real sunrise and sunset, and its weather if you turn that on. The name is looked up on Open-Meteo. Leave it empty to remove it.",
                AutoSize = true,
                MaximumSize = new Size(330, 0),
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 0, 0, 14),
            };
            box = new TextBox { BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Ink, Font = Theme.Regular(11.5f), MaxLength = 60, Text = current ?? "", Dock = DockStyle.Fill };
            var field = new InputFrame(box) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            status = new Label { AutoSize = true, ForeColor = Theme.AccentText, Font = Theme.Regular(9f), Margin = new Padding(0, 0, 0, 10), MaximumSize = new Size(330, 0) };

            var cancel = new FlatButton(ButtonKind.Ghost) { Text = "Cancel", Uppercase = true, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Font = Theme.SemiBold(10f) };
            find = new FlatButton(ButtonKind.Primary) { Text = "Save", Uppercase = true, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10f) };
            var buttons = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0) };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            buttons.Controls.Add(cancel, 0, 0);
            buttons.Controls.Add(find, 1, 0);

            Ui.AddRow(root, title);
            Ui.AddRow(root, body);
            Ui.AddRow(root, field);
            Ui.AddRow(root, status);
            Ui.AddRow(root, buttons);
            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
            ClientSize = new Size(ClientSize.Width, root.GetPreferredSize(new Size(ClientSize.Width, 0)).Height + 20);

            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            find.Click += (s, e) => Look();
            CancelButton = cancel;
            box.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Look(); } };
            Shown += (s, e) => { box.Focus(); box.SelectAll(); };
        }

        public Town Town { get; private set; }

        async void Look()
        {
            string name = box.Text.Trim();
            if (name.Length == 0)
            {
                Town = null;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            find.Enabled = false;
            status.Text = "Looking it up...";
            try
            {
                var town = await RealWeather.FindTown(name);
                if (town == null)
                {
                    status.Text = "Couldn't find that town. Try its English name.";
                    return;
                }
                Town = town;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception)
            {
                status.Text = "Couldn't reach the internet. Try again later.";
            }
            finally
            {
                if (!IsDisposed) find.Enabled = true;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
