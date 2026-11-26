using System;
using System.Drawing;
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
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        readonly Label saveError;
        HistoryForm history;
        SettingsForm settings;

        public MainForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            Ui.SetupWindow(this, "Stillpoint", new Size(400, 560));
            MinimumSize = new Size(380, 520);
            StartPosition = FormStartPosition.CenterScreen;

            var root = Ui.Column(new Padding(26, 12, 26, 18));
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

            var historyButton = new FlatButton(ButtonKind.Ghost) { Text = "History", Height = 40, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10f) };
            var settingsButton = new FlatButton(ButtonKind.Ghost) { Size = new Size(40, 40), Margin = new Padding(10, 0, 0, 0), Glyph = Draw.Gear, AccessibleName = "Settings" };
            new ToolTip().SetToolTip(settingsButton, "Settings");
            var bottomRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 40, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomRow.Controls.Add(historyButton, 0, 0);
            bottomRow.Controls.Add(settingsButton, 1, 0);

            updateLink = Ui.Link("");
            updateLink.Anchor = AnchorStyles.Left;
            updateLink.Margin = new Padding(0, 10, 0, 0);
            updateLink.Visible = false;
            updateStatus = Ui.Small(Theme.Muted);
            saveError = Ui.Small(Theme.AccentText);

            Ui.AddRow(root, ring, fill: true);
            Ui.AddRow(root, status);
            Ui.AddRow(root, buttonRow);
            Ui.AddRow(root, bottomRow);
            Ui.AddRow(root, updateLink);
            Ui.AddRow(root, updateStatus);
            Ui.AddRow(root, saveError);

            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            startStop.Click += (s, e) => app.ToggleRunning();
            resume.Click += (s, e) => app.Resume();
            historyButton.Click += (s, e) => ShowHistory();
            settingsButton.Click += (s, e) => ShowSettings();
            updateLink.LinkClicked += (s, e) => app.InstallUpdate();
        }

        public void SyncControls()
        {
            if (settings != null && !settings.IsDisposed) settings.SyncControls();
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
            Ui.SetText(status, app.DescribeState());
            startStop.Text = tracker.Running ? "I got up" : "Start";
            bool canResume = tracker.CanResume;
            if (resume.Visible != canResume)
            {
                resume.Visible = canResume;
                buttonRow.ColumnStyles[1] = canResume ? new ColumnStyle(SizeType.Percent, 50F) : new ColumnStyle(SizeType.Absolute, 0F);
            }

            bool hasUpdate = app.UpdateAvailable != null;
            updateLink.Visible = hasUpdate;
            if (hasUpdate) Ui.SetText(updateLink, $"Update to {app.UpdateAvailable}");
            updateLink.Enabled = !app.UpdateBusy;
            Ui.SetLine(updateStatus, app.UpdateStatus);
            Ui.SetLine(saveError, app.SaveError == null ? null : "Could not save: " + app.SaveError);

            if (history != null && !history.IsDisposed) history.SetSessions(app.Tracker.Sessions, app.Tracker.Current);
            if (settings != null && !settings.IsDisposed) settings.UpdateView();
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

        void ShowHistory() => Ui.ShowBeside(this, ref history, () =>
        {
            var next = new HistoryForm();
            next.SetSessions(app.Tracker.Sessions, app.Tracker.Current);
            return next;
        });

        void ShowSettings() => Ui.ShowBeside(this, ref settings, () => new SettingsForm(app));
    }
}
