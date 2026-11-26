using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class MainForm : Form
    {
        readonly TrayApp app;
        readonly SessionRing ring;
        readonly Label status;
        readonly FlatButton startStop;
        readonly FlatButton resume;
        readonly TableLayoutPanel buttonRow;
        readonly UnderlineBox reminderMinutes;
        readonly FlatButton historyButton;
        HistoryForm history;
        readonly ToggleSwitch startWithWindows;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        readonly Label saveError;
        bool syncing;

        public MainForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Regular(10f);
            ForeColor = Theme.Ink;
            BackColor = Theme.Background;
            Text = "Stillpoint";
            ClientSize = new Size(400, 600);
            MinimumSize = new Size(380, 560);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(26, 12, 26, 16), BackColor = Theme.Background };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.SuspendLayout();

            ring = new SessionRing { Size = new Size(300, 300), Anchor = AnchorStyles.None, Margin = new Padding(0, 6, 0, 6) };
            status = new Label { AutoSize = true, Anchor = AnchorStyles.None, ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 14) };
            startStop = new FlatButton(ButtonKind.Primary) { Text = "Start", Uppercase = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10.5f) };
            resume = new FlatButton(ButtonKind.Ghost) { Text = "Resume", Uppercase = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0), Font = Theme.SemiBold(10.5f), Visible = false };
            buttonRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(0) };
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            buttonRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            buttonRow.Controls.Add(startStop, 0, 0);
            buttonRow.Controls.Add(resume, 1, 0);

            reminderMinutes = new UnderlineBox(10f, 44, HorizontalAlignment.Center) { Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 8, 0) };
            var preview = Link("Preview");
            var reminderLeft = Row(
                new Label { Text = "Remind me every", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0) },
                reminderMinutes,
                new Label { Text = "minutes", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0) });
            var reminderRow = TwoSides(reminderLeft, preview);
            reminderRow.Margin = new Padding(0, 18, 0, 14);

            historyButton = new FlatButton(ButtonKind.Ghost) { Text = "History", Height = 40, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10f) };

            updateStatus = Small(Theme.Muted);
            saveError = Small(Theme.AccentText);

            startWithWindows = new ToggleSwitch { Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
            var autoLabel = new Label { Text = "Start with Windows", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0), Font = Theme.Regular(9f), Cursor = Cursors.Hand };
            var version = new Label { Text = "v" + AppVersion.Text + "  ·", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0), Font = Theme.Regular(9f), ForeColor = Theme.Muted };
            updateLink = Link("Check for updates");
            updateLink.Font = Theme.Regular(9f);
            var footer = TwoSides(Row(startWithWindows, autoLabel), Row(version, updateLink));
            footer.Margin = new Padding(0, 12, 0, 0);

            AddRow(root, ring, fill: true);
            AddRow(root, status);
            AddRow(root, buttonRow);
            AddRow(root, reminderRow);
            AddRow(root, historyButton);
            AddRow(root, updateStatus);
            AddRow(root, saveError);
            AddRow(root, footer);

            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            startStop.Click += (s, e) => app.ToggleRunning();
            resume.Click += (s, e) => app.Resume();
            historyButton.Click += (s, e) => ShowHistory();
            reminderMinutes.Box.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
            reminderMinutes.Box.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitReminder(); } };
            reminderMinutes.Box.Leave += (s, e) => CommitReminder();
            preview.LinkClicked += (s, e) => app.ShowReminder(app.Settings.ReminderMinutes * 60, preview: true);
            startWithWindows.CheckedChanged += (s, e) => { if (!syncing) { app.SetAutoStart(startWithWindows.Checked); SyncControls(); } };
            autoLabel.Click += (s, e) => startWithWindows.Checked = !startWithWindows.Checked;
            updateLink.LinkClicked += (s, e) =>
            {
                if (app.UpdateAvailable != null) app.InstallUpdate();
                else app.CheckForUpdates(manual: true);
            };
        }

        public void SyncControls()
        {
            syncing = true;
            reminderMinutes.Box.Text = app.Settings.ReminderMinutes.ToString(CultureInfo.InvariantCulture);
            startWithWindows.Checked = app.AutoStartEnabled;
            syncing = false;
        }

        public void UpdateView()
        {
            var tracker = app.Tracker;
            string note;
            float progress = 0;
            switch (tracker.State)
            {
                case TrackState.Counting:
                    double interval = tracker.ReminderIntervalSeconds;
                    double into = interval > 0 ? tracker.SessionSeconds % interval : 0;
                    progress = interval > 0 ? (float)(into / interval) : 0;
                    int minutes = (int)Math.Ceiling((interval - into) / 60);
                    note = minutes <= 1 ? "Reminder in a moment" : $"{minutes} min to next reminder";
                    break;
                case TrackState.Paused:
                    note = "Paused";
                    break;
                default:
                    note = "Not counting";
                    break;
            }
            ring.Set(progress, "THIS SESSION", Fmt.Clock(tracker.SessionSeconds), note);
            SetText(status, app.DescribeState());
            startStop.Text = tracker.Running ? "I got up" : "Start";
            bool canResume = tracker.CanResume;
            if (resume.Visible != canResume)
            {
                resume.Visible = canResume;
                buttonRow.ColumnStyles[1] = canResume ? new ColumnStyle(SizeType.Percent, 50F) : new ColumnStyle(SizeType.Absolute, 0F);
            }

            SetLine(updateStatus, app.UpdateStatus);
            SetLine(saveError, app.SaveError == null ? null : "Could not save: " + app.SaveError);
            string link = app.UpdateAvailable != null ? $"Update to {app.UpdateAvailable}" : "Check for updates";
            if (updateLink.Text != link) updateLink.Text = link;
            updateLink.Enabled = !app.UpdateBusy;

            if (history != null && !history.IsDisposed) history.SetSessions(app.Tracker.Sessions, app.Tracker.Current);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !app.Exiting)
            {
                e.Cancel = true;
                Hide();
                app.OnWindowHidden();
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            app.Exit(fromForm: true);
        }

        void CommitReminder()
        {
            if (syncing) return;
            if (int.TryParse(reminderMinutes.Box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
            {
                minutes = Math.Max(Settings.MinReminderMinutes, Math.Min(Settings.MaxReminderMinutes, minutes));
                if (minutes != app.Settings.ReminderMinutes) app.SetReminderMinutes(minutes);
            }
            SyncControls();
        }

        void ShowHistory()
        {
            if (history == null || history.IsDisposed)
            {
                history = new HistoryForm { Icon = Icon, Owner = this };
                history.SetSessions(app.Tracker.Sessions, app.Tracker.Current);
                var area = Screen.FromControl(this).WorkingArea;
                int x = Right + 12 + history.Width <= area.Right ? Right + 12 : Math.Max(area.Left, Left - 12 - history.Width);
                history.Location = new Point(x, Math.Max(area.Top, Math.Min(Top, area.Bottom - history.Height)));
                history.Show();
                return;
            }
            if (history.WindowState == FormWindowState.Minimized) history.WindowState = FormWindowState.Normal;
            history.Activate();
        }

        static void SetText(Label label, string text)
        {
            if (label.Text != text) label.Text = text;
        }

        static void SetLine(Label label, string text)
        {
            label.Visible = text != null;
            if (text != null) SetText(label, text);
        }

        static Label Small(Color color) => new Label
        {
            AutoSize = true,
            Visible = false,
            ForeColor = color,
            Font = Theme.Regular(9f),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 8, 0, 0),
        };

        static LinkLabel Link(string text) => new LinkLabel
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0),
            LinkColor = Theme.AccentText,
            ActiveLinkColor = Theme.Accent,
            VisitedLinkColor = Theme.AccentText,
            LinkBehavior = LinkBehavior.HoverUnderline,
        };

        static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Left,
            };
            row.Controls.AddRange(controls);
            return row;
        }

        static TableLayoutPanel TwoSides(Control left, Control right)
        {
            var table = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            left.Anchor = AnchorStyles.Left;
            right.Anchor = AnchorStyles.Right;
            table.Controls.Add(left, 0, 0);
            table.Controls.Add(right, 1, 0);
            return table;
        }

        static void AddRow(TableLayoutPanel table, Control control, bool fill = false)
        {
            table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100F) : new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, table.RowCount++);
        }
    }
}
