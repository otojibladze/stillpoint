using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class ChallengeDialog : Form
    {
        readonly Random random = new Random();
        readonly Label prompt;
        readonly Label feedback;
        readonly TableLayoutPanel host;
        Control content;
        int lastKind = -1;

        public ChallengeDialog(string title, string hintText, string confirmText, string cancelText)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(340, 470);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(16, 12, 16, 12) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            prompt = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 11F), Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 0, 3, 2) };
            var hint = new Label { Text = hintText, AutoSize = true, ForeColor = SystemColors.GrayText, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 0, 3, 8) };
            host = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1, Margin = new Padding(0) };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            feedback = new Label { AutoSize = true, ForeColor = Color.Firebrick, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 8, 3, 0) };

            var confirm = new Button { Text = confirmText, AutoSize = true, MinimumSize = new Size(90, 0) };
            var cancel = new Button { Text = cancelText, AutoSize = true, DialogResult = DialogResult.Cancel };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
            buttons.Controls.Add(confirm);
            buttons.Controls.Add(cancel);

            root.Controls.Add(prompt, 0, 0);
            root.Controls.Add(hint, 0, 1);
            root.Controls.Add(host, 0, 2);
            root.Controls.Add(feedback, 0, 3);
            root.Controls.Add(buttons, 0, 4);
            Controls.Add(root);
            AcceptButton = confirm;
            CancelButton = cancel;
            ResumeLayout(false);
            PerformLayout();

            confirm.Click += (s, e) => Check();
            NewPuzzle();
        }

        public Puzzle Puzzle { get; private set; }

        public void Check()
        {
            if (Puzzle.Solved())
            {
                DialogResult = DialogResult.OK;
                return;
            }
            feedback.Text = "Not quite. Try this one.";
            NewPuzzle();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            FocusInput();
        }

        void NewPuzzle()
        {
            Puzzle = Puzzles.Next(random, ref lastKind);
            var next = Puzzle.Build();
            next.Anchor = AnchorStyles.None;
            host.SuspendLayout();
            if (content != null)
            {
                host.Controls.Remove(content);
                content.Dispose();
            }
            content = next;
            host.Controls.Add(content, 0, 0);
            host.ResumeLayout(true);
            prompt.Text = Puzzle.Prompt;
            FocusInput();
        }

        void FocusInput()
        {
            var box = Descendants(content).OfType<TextBox>().FirstOrDefault();
            if (box != null) ActiveControl = box;
        }

        static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
    }
}
