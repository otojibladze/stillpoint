using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class MainForm : Form
    {
        readonly TrayApp app;
        readonly Label sessionValue;
        readonly Label status;
        readonly Label saveError;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        readonly Button startStop;
        readonly NumericUpDown reminderMinutes;
        readonly CheckBox startWithWindows;
        readonly ListView history;
        readonly ColumnHeader startColumn;
        readonly ColumnHeader endColumn;
        readonly ColumnHeader lengthColumn;
        bool syncing;

        public MainForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = "Stillpoint";
            ClientSize = new Size(400, 560);
            MinimumSize = new Size(360, 480);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(16, 12, 16, 12) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.SuspendLayout();

            sessionValue = new Label { AutoSize = true, Font = new Font("Segoe UI Light", 32F), Margin = new Padding(0) };
            status = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), Margin = new Padding(3, 0, 3, 8) };
            startStop = new Button
            {
                Text = "Start",
                AutoSize = true,
                MinimumSize = new Size(130, 38),
                Font = new Font("Segoe UI Semibold", 11F),
                Anchor = AnchorStyles.Left,
            };

            reminderMinutes = new NumericUpDown
            {
                Minimum = Settings.MinReminderMinutes,
                Maximum = Settings.MaxReminderMinutes,
                Increment = 5,
                Value = 60,
                Width = 60,
                Anchor = AnchorStyles.Left,
                TextAlign = HorizontalAlignment.Right,
            };
            var preview = new LinkLabel { Text = "Preview", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 3, 3, 3) };

            startColumn = new ColumnHeader { Text = "Started" };
            endColumn = new ColumnHeader { Text = "Ended" };
            lengthColumn = new ColumnHeader { Text = "Length", TextAlign = HorizontalAlignment.Right };
            history = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 0, 3, 6),
            };
            history.Columns.AddRange(new[] { startColumn, endColumn, lengthColumn });

            startWithWindows = new CheckBox { Text = "Start with Windows", AutoSize = true, Anchor = AnchorStyles.Left };
            var openFolder = new LinkLabel { Text = "Open data folder", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(24, 3, 3, 3) };
            var version = new Label { Text = "Version " + AppVersion.Text, AutoSize = true, ForeColor = SystemColors.GrayText, Anchor = AnchorStyles.Left };
            updateLink = new LinkLabel { Text = "Check for updates", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 3, 3, 3) };
            updateStatus = Hint("");
            updateStatus.Visible = false;
            saveError = new Label { AutoSize = true, ForeColor = Color.Firebrick, Anchor = AnchorStyles.Left | AnchorStyles.Right, Visible = false };

            AddRow(root, Caption("This session", 0));
            AddRow(root, sessionValue);
            AddRow(root, status);
            AddRow(root, startStop);
            AddRow(root, Caption("Reminders", 16));
            AddRow(root, Row(
                new Label { Text = "Remind me every", AutoSize = true, Anchor = AnchorStyles.Left },
                reminderMinutes,
                new Label { Text = "minutes", AutoSize = true, Anchor = AnchorStyles.Left },
                preview));
            AddRow(root, Hint("A small window pops up in the bottom corner of the screen and stays until you click OK."));
            AddRow(root, Caption("Sessions", 12));
            AddRow(root, history, fill: true);
            AddRow(root, Row(startWithWindows, openFolder));
            AddRow(root, Row(version, updateLink));
            AddRow(root, updateStatus);
            AddRow(root, saveError);
            AddRow(root, Hint("Closing this window keeps it running in the tray. Right-click the tray icon to exit."));

            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            startStop.Click += (s, e) => app.ToggleRunning();
            reminderMinutes.ValueChanged += (s, e) => { if (!syncing) app.SetReminderMinutes((int)reminderMinutes.Value); };
            preview.LinkClicked += (s, e) => app.ShowReminder(app.Settings.ReminderMinutes * 60, preview: true);
            startWithWindows.CheckedChanged += (s, e) => { if (!syncing) { app.SetAutoStart(startWithWindows.Checked); SyncControls(); } };
            openFolder.LinkClicked += (s, e) => Process.Start("explorer.exe", "\"" + app.Store.Dir + "\"");
            updateLink.LinkClicked += (s, e) =>
            {
                if (app.UpdateAvailable != null) app.InstallUpdate();
                else app.CheckForUpdates(manual: true);
            };
            history.Resize += (s, e) => FitColumns();
        }

        public void SyncControls()
        {
            syncing = true;
            reminderMinutes.Value = Math.Max(reminderMinutes.Minimum, Math.Min(reminderMinutes.Maximum, app.Settings.ReminderMinutes));
            startWithWindows.Checked = app.AutoStartEnabled;
            syncing = false;
        }

        public void UpdateView()
        {
            var tracker = app.Tracker;
            sessionValue.Text = Fmt.Clock(tracker.SessionSeconds);
            status.Text = app.DescribeState();
            status.ForeColor = tracker.IsCounting ? Icons.On : SystemColors.GrayText;
            startStop.Text = tracker.Running ? "Stop" : "Start";

            string error = app.SaveError == null ? null : "Could not save: " + app.SaveError;
            saveError.Visible = error != null;
            string link = app.UpdateAvailable != null ? $"Update to {app.UpdateAvailable}" : "Check for updates";
            if (updateLink.Text != link) updateLink.Text = link;
            updateLink.Enabled = !app.UpdateBusy;
            updateStatus.Visible = app.UpdateStatus != null;
            if (app.UpdateStatus != null && updateStatus.Text != app.UpdateStatus) updateStatus.Text = app.UpdateStatus;
            if (error != null && saveError.Text != error) saveError.Text = error;

            UpdateHistory();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            FitColumns();
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

        void UpdateHistory()
        {
            var tracker = app.Tracker;
            var sessions = tracker.Sessions.OrderByDescending(s => s.Start).ToList();
            bool rebuild = history.Items.Count != sessions.Count
                || (sessions.Count > 0 && history.Items[0].Tag != sessions[0]);
            if (rebuild)
            {
                history.BeginUpdate();
                history.Items.Clear();
                foreach (var session in sessions)
                    history.Items.Add(new ListViewItem(Cells(session, tracker.Current)) { Tag = session });
                history.EndUpdate();
                return;
            }
            for (int i = 0; i < sessions.Count; i++)
            {
                var cells = Cells(sessions[i], tracker.Current);
                var item = history.Items[i];
                for (int c = 1; c < cells.Length; c++)
                    if (item.SubItems[c].Text != cells[c]) item.SubItems[c].Text = cells[c];
            }
        }

        static string[] Cells(Session session, Session current)
        {
            var culture = CultureInfo.CurrentCulture;
            string started = session.Start.ToString("ddd d MMM, ", culture) + session.Start.ToString("t", culture);
            string ended = session == current ? "now"
                : session.End.Date == session.Start.Date ? session.End.ToString("t", culture)
                : session.End.ToString("ddd ", culture) + session.End.ToString("t", culture);
            return new[] { started, ended, Fmt.Hm(session.Seconds) };
        }

        void FitColumns()
        {
            int lengthWidth = Scaled(80), endWidth = Scaled(90);
            lengthColumn.Width = lengthWidth;
            endColumn.Width = endWidth;
            startColumn.Width = Math.Max(Scaled(110), history.ClientSize.Width - lengthWidth - endWidth - SystemInformation.VerticalScrollBarWidth);
        }

        int Scaled(int logicalPixels) => (int)Math.Round(logicalPixels * AutoScaleFactor.Width);

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

        static Label Hint(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 2, 3, 4),
        };

        static Label Caption(string text, int top) => new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(85, 91, 102),
            Margin = new Padding(3, top, 3, 2),
        };

        static void AddRow(TableLayoutPanel table, Control control, bool fill = false)
        {
            table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100F) : new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, table.RowCount++);
        }
    }
}
