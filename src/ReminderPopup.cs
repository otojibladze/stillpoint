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

        readonly Bitmap image = Icons.Draw(96, Icons.On);

        public ReminderPopup(string title, string body, Func<bool> confirmClose, Func<bool> stop)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = "Stillpoint";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(190, 195, 202);
            Padding = new Padding(1);
            ClientSize = new Size(380, 124);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(14, 12, 14, 10),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var icon = new PictureBox { Image = image, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(36, 36), Margin = new Padding(0, 2, 12, 0) };
            var titleLabel = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Margin = new Padding(0) };
            var bodyLabel = new Label { Text = body, AutoSize = true, ForeColor = Color.FromArgb(85, 91, 102), Margin = new Padding(0, 2, 0, 0) };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
            };
            var ok = new Button { Text = "OK", AutoSize = true, MinimumSize = new Size(80, 0) };
            ok.Click += (s, e) => { if (confirmClose == null || confirmClose()) Close(); };
            buttons.Controls.Add(ok);
            if (stop != null)
            {
                var stopButton = new Button { Text = "Stop counting", AutoSize = true };
                stopButton.Click += (s, e) => { if (stop()) Close(); };
                buttons.Controls.Add(stopButton);
            }

            table.Controls.Add(icon, 0, 0);
            table.SetRowSpan(icon, 2);
            table.Controls.Add(titleLabel, 1, 0);
            table.Controls.Add(bodyLabel, 1, 1);
            table.Controls.Add(buttons, 0, 2);
            table.SetColumnSpan(buttons, 2);
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

        public void ShowNearTray()
        {
            var area = Screen.PrimaryScreen.WorkingArea;
            int margin = (int)Math.Round(12 * AutoScaleFactor.Width);
            Location = new Point(area.Right - Width - margin, area.Bottom - Height - margin);
            Show();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            image.Dispose();
        }
    }
}
