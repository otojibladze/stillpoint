using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class ReminderPopup : Form
    {
        const int WsExTopmost = 0x8;
        const int WsExToolWindow = 0x80;
        const int WsExNoActivate = 0x08000000;

        public ReminderPopup(string title, string body, MiniMood mood, string primaryText, Action primary, string secondaryText = null, Action secondary = null)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Regular(10f);
            ForeColor = Theme.Ink;
            Text = "Stillpoint";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Track;
            Padding = new Padding(1);
            ClientSize = new Size(380, 168);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(20, 18, 20, 16),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var ring = new MiniSpirit { Size = new Size(56, 56), Margin = new Padding(0, 0, 14, 0), BackColor = Theme.Surface, Mood = mood, Snow = Garden.SeasonOf(DateTime.Today) == Season.Winter };
            var titleLabel = new Label { Text = title, AutoSize = true, Font = Theme.SemiBold(13.5f), Margin = new Padding(0, 2, 0, 0) };
            var bodyLabel = new Label { Text = body, AutoSize = true, MaximumSize = new Size(320, 0), ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 4, 0, 0) };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Margin = new Padding(24, 0, 0, 0),
            };
            buttons.Controls.Add(Button(ButtonKind.Primary, primaryText, primary, new Padding(8, 0, 0, 0)));
            if (secondaryText != null) buttons.Controls.Add(Button(ButtonKind.Ghost, secondaryText, secondary, new Padding(0)));

            table.Controls.Add(ring, 0, 0);
            table.SetRowSpan(ring, 2);
            table.Controls.Add(titleLabel, 1, 0);
            table.Controls.Add(bodyLabel, 1, 1);
            table.Controls.Add(buttons, 2, 0);
            table.SetRowSpan(buttons, 2);
            Controls.Add(table);
            this.table = table;
            ResumeLayout(false);
            PerformLayout();
            FitSize();
        }

        readonly TableLayoutPanel table;

        FlatButton Button(ButtonKind kind, string text, Action action, Padding margin)
        {
            var button = new FlatButton(kind) { Text = text, Uppercase = true, MinimumSize = new Size(88, 36), Height = 36, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = margin, Font = Theme.SemiBold(9.5f) };
            button.Click += (s, e) => { action?.Invoke(); Close(); };
            return button;
        }

        void FitSize()
        {
            var size = table.GetPreferredSize(Size.Empty);
            ClientSize = new Size(size.Width + Padding.Horizontal, size.Height + Padding.Vertical);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WsExTopmost | WsExToolWindow | WsExNoActivate;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.RoundCorners(this);
        }

        public void ShowNearTray()
        {
            FitSize();
            PlaceOn(Screen.FromPoint(Cursor.Position));
            Show();
        }

        public void FollowCursor()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            if (!screen.Equals(Screen.FromControl(this))) PlaceOn(screen);
        }

        void PlaceOn(Screen screen)
        {
            var area = screen.WorkingArea;
            int margin = (int)Math.Round(12 * AutoScaleFactor.Width);
            Location = new Point(area.Right - Width - margin, area.Bottom - Height - margin);
        }
    }
}
