using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class MainForm : Form
    {
        readonly TrayApp app;
        readonly SessionRing ring;
        readonly Label status;
        readonly FlatButton startStop;
        readonly UnderlineBox reminderMinutes;
        readonly SessionList sessions;
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
            ClientSize = new Size(400, 606);
            MinimumSize = new Size(380, 560);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(26, 12, 26, 16), BackColor = Theme.Background };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.SuspendLayout();

            ring = new SessionRing { Size = new Size(196, 196), Anchor = AnchorStyles.None, Margin = new Padding(0, 6, 0, 6) };
            status = new Label { AutoSize = true, Anchor = AnchorStyles.None, ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 14) };
            startStop = new FlatButton(ButtonKind.Primary) { Text = "Start", Uppercase = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10.5f) };

            reminderMinutes = new UnderlineBox(10f, 44, HorizontalAlignment.Center) { Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 8, 0) };
            var preview = Link("Preview");
            var reminderLeft = Row(
                new Label { Text = "Remind me every", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0) },
                reminderMinutes,
                new Label { Text = "minutes", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0) });
            var reminderRow = TwoSides(reminderLeft, preview);
            reminderRow.Margin = new Padding(0, 18, 0, 14);

            sessions = new SessionList { Dock = DockStyle.Fill, Margin = new Padding(0) };

            updateStatus = Small(Theme.Muted);
            saveError = Small(Theme.AccentText);

            startWithWindows = new ToggleSwitch { Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
            var autoLabel = new Label { Text = "Start with Windows", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0), Font = Theme.Regular(9f), Cursor = Cursors.Hand };
            var version = new Label { Text = "v" + AppVersion.Text + "  ·", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0), Font = Theme.Regular(9f), ForeColor = Theme.Muted };
            updateLink = Link("Check for updates");
            updateLink.Font = Theme.Regular(9f);
            var footer = TwoSides(Row(startWithWindows, autoLabel), Row(version, updateLink));
            footer.Margin = new Padding(0, 12, 0, 0);

            AddRow(root, ring);
            AddRow(root, status);
            AddRow(root, startStop);
            AddRow(root, reminderRow);
            AddRow(root, sessions, fill: true);
            AddRow(root, updateStatus);
            AddRow(root, saveError);
            AddRow(root, footer);

            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            startStop.Click += (s, e) => app.ToggleRunning();
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
            startStop.Text = tracker.Running ? "Stop" : "Start";

            SetLine(updateStatus, app.UpdateStatus);
            SetLine(saveError, app.SaveError == null ? null : "Could not save: " + app.SaveError);
            string link = app.UpdateAvailable != null ? $"Update to {app.UpdateAvailable}" : "Check for updates";
            if (updateLink.Text != link) updateLink.Text = link;
            updateLink.Enabled = !app.UpdateBusy;

            sessions.SetRows(tracker.Sessions.OrderByDescending(s => s.Start).Select(s => Cells(s, tracker.Current)).ToList());
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

        static string[] Cells(Session session, Session current)
        {
            var culture = CultureInfo.CurrentCulture;
            string started = session.Start.ToString("ddd d MMM, ", culture) + session.Start.ToString("t", culture);
            string ended = session == current ? "now"
                : session.End.Date == session.Start.Date ? session.End.ToString("t", culture)
                : session.End.ToString("ddd ", culture) + session.End.ToString("t", culture);
            return new[] { started, ended, Fmt.Hm(session.Seconds) };
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
