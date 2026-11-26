using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class MainForm : Form
    {
        readonly TrayApp app;
        readonly SpiritRing ring;
        readonly Label status;
        readonly LinkLabel breathe;
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
            Ui.SetupWindow(this, "Pebble", new Size(400, 590));
            MinimumSize = new Size(300, 440);
            StartPosition = FormStartPosition.CenterScreen;

            var root = Ui.Column(new Padding(26, 12, 26, 18));
            root.SuspendLayout();

            ring = new SpiritRing { Size = new Size(300, 300), Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 6) };
            status = new Label { AutoSize = true, Anchor = AnchorStyles.None, ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 6) };
            breathe = Ui.Link("Breathe with me");
            breathe.Anchor = AnchorStyles.None;
            breathe.Margin = new Padding(0, 0, 0, 14);
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
            Ui.AddRow(root, breathe);
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
            breathe.LinkClicked += (s, e) => ring.ToggleBreathing();
            ring.BreathingChanged += (s, e) => Ui.SetText(breathe, ring.Breathing ? "Stop breathing" : "Breathe with me");
            ring.Visited += app.AddVisitor;
            ring.SoundRequested += app.PlayStoneSound;
        }

        public void OnReminder() => ring.Reminder();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyWindowSize(app.Settings.WindowSize);
        }

        public void ApplyWindowSize(WindowSize size)
        {
            float k = Settings.Scale(size) * DeviceDpi / 96f;
            var target = new Size((int)Math.Round(400 * k), (int)Math.Round(590 * k));
            if (ClientSize == target) return;
            ClientSize = target;
            var area = Screen.FromControl(this).WorkingArea;
            Left = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
            Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        }

        public void SyncControls()
        {
            if (settings != null && !settings.IsDisposed) settings.SyncControls();
        }

        public void UpdateView()
        {
            var tracker = app.Tracker;
            double interval = tracker.ReminderIntervalSeconds;
            double into = tracker.Running && interval > 0 ? tracker.SessionSeconds % interval : 0;
            float progress = interval > 0 ? (float)(into / interval) : 0;
            string state = app.DescribeState();
            string caption;
            switch (tracker.State)
            {
                case TrackState.Counting:
                    int minutes = (int)Math.Ceiling((interval - into) / 60);
                    state += minutes <= 1 ? "  ·  reminder in a moment" : $"  ·  reminder in {minutes} min";
                    caption = "THIS SESSION";
                    break;
                case TrackState.Paused:
                    caption = "PAUSED";
                    break;
                default:
                    caption = tracker.CanResume ? "LAST SESSION" : "READY";
                    break;
            }
            var today = DateTime.Today;
            ring.SetSession(new RingState
            {
                Running = tracker.Running,
                Paused = tracker.State == TrackState.Paused,
                Away = app.Away,
                Progress = progress,
                SessionSeconds = tracker.Running ? tracker.SessionSeconds : 0,
                Interval = interval,
                Time = Fmt.Clock(tracker.SessionSeconds),
                Caption = caption,
                BreaksToday = Garden.BreaksOn(tracker.Sessions, tracker.Current, today),
                LastEnd = tracker.Sessions.Count > 0 ? tracker.Sessions.Max(s => s.End) : default(DateTime),
                Keepsakes = Garden.Progress(tracker.Sessions, tracker.Current, today).Earned,
                Name = app.Settings.StoneName,
                Chat = app.Settings.Chattiness,
            });
            Ui.SetText(status, state);
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

            if (history != null && !history.IsDisposed) history.SetSessions(app.Tracker.Sessions, app.Tracker.Current, app.Album, interval);
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
            next.SetSessions(app.Tracker.Sessions, app.Tracker.Current, app.Album, app.Tracker.ReminderIntervalSeconds);
            return next;
        });

        void ShowSettings() => Ui.ShowBeside(this, ref settings, () => new SettingsForm(app));
    }
}
