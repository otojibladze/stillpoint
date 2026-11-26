using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class HistoryForm : Form
    {
        public HistoryForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Regular(10f);
            ForeColor = Theme.Ink;
            BackColor = Theme.Background;
            Text = "Stillpoint history";
            ClientSize = new Size(420, 520);
            MinimumSize = new Size(360, 320);
            StartPosition = FormStartPosition.Manual;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(22, 14, 22, 20), BackColor = Theme.Background };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var title = new Label { Text = "History", AutoSize = true, Font = Theme.SemiBold(14f), Margin = new Padding(0, 0, 0, 2) };
            var hint = new Label { Text = "Every session you counted, newest first.", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 0, 0, 12) };
            List = new SessionList { Dock = DockStyle.Fill, Margin = new Padding(0) };
            root.Controls.Add(title, 0, 0);
            root.Controls.Add(hint, 0, 1);
            root.Controls.Add(List, 0, 2);
            Controls.Add(root);
            ResumeLayout(false);
            PerformLayout();
        }

        public SessionList List { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
