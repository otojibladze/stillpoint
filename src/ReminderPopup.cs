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

        public ReminderPopup(string title, string badge, string body, Func<bool> confirmClose, Func<bool> stop)
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
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(20, 18, 20, 16),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var ring = new MiniRing { Text = badge, Size = new Size(44, 44), Margin = new Padding(0, 0, 16, 0), BackColor = Theme.Surface };
            var titleLabel = new Label { Text = title, AutoSize = true, Font = Theme.SemiBold(13.5f), Margin = new Padding(0, 2, 0, 0) };
            var bodyLabel = new Label { Text = body, AutoSize = true, MaximumSize = new Size(270, 0), ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 4, 0, 0) };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Margin = new Padding(0),
            };
            var ok = new FlatButton(ButtonKind.Primary) { Text = "OK", MinimumSize = new Size(64, 36), Height = 36, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(8, 0, 0, 0), Font = Theme.SemiBold(9.5f) };
            ok.Click += (s, e) => { if (confirmClose == null || confirmClose()) Close(); };
            buttons.Controls.Add(ok);
            if (stop != null)
            {
                var stopButton = new FlatButton(ButtonKind.Ghost) { Text = "Stop counting", MinimumSize = new Size(88, 36), Height = 36, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0), Font = Theme.SemiBold(9.5f) };
                stopButton.Click += (s, e) => { if (stop()) Close(); };
                buttons.Controls.Add(stopButton);
            }

            table.Controls.Add(ring, 0, 0);
            table.SetRowSpan(ring, 2);
            table.Controls.Add(titleLabel, 1, 0);
            table.Controls.Add(bodyLabel, 1, 1);
            table.Controls.Add(buttons, 1, 2);
            Controls.Add(table);
            ResumeLayout(false);
            PerformLayout();
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
            var area = Screen.PrimaryScreen.WorkingArea;
            int margin = (int)Math.Round(12 * AutoScaleFactor.Width);
            Location = new Point(area.Right - Width - margin, area.Bottom - Height - margin);
            Show();
        }
    }
}
