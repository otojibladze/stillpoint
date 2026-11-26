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
        readonly ToggleSwitch stoneToggle;
        readonly FlatButton gardenButton;
        readonly TableLayoutPanel bottomRow;
        readonly FlatButton startStop;
        readonly FlatButton resume;
        readonly TableLayoutPanel buttonRow;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        readonly Label saveError;
        HistoryForm history;
        GardenForm garden;
        SettingsForm settings;
        bool syncing;

        public MainForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            Ui.SetupWindow(this, "Pebble", new Size(400, 590));
            MinimumSize = new Size(300, 440);
            StartPosition = FormStartPosition.CenterScreen;

            var root = Ui.Column(new Padding(26, 12, 26, 18));
            root.SuspendLayout();

            stoneToggle = new ToggleSwitch { Margin = new Padding(8, 2, 0, 0) };
            var stoneLabel = new Label { Text = "Stone and garden", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9f), Margin = new Padding(0), Cursor = Cursors.Hand };
            var stoneRow = Ui.Row(stoneLabel, stoneToggle);
            stoneRow.Anchor = AnchorStyles.Right;
            stoneRow.Margin = new Padding(0);
            new ToolTip().SetToolTip(stoneToggle, "Turn off to use Pebble as a plain timer");
            ring = new SpiritRing { Size = new Size(300, 300), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 6) };
            status = new Label { AutoSize = true, Anchor = AnchorStyles.None, ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 14) };
            startStop = new FlatButton(ButtonKind.Primary) { Text = "Start", Uppercase = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10.5f) };
            resume = new FlatButton(ButtonKind.Ghost) { Text = "Resume", Uppercase = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0), Font = Theme.SemiBold(10.5f), Visible = false };
            buttonRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(0) };
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            buttonRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            buttonRow.Controls.Add(startStop, 0, 0);
            buttonRow.Controls.Add(resume, 1, 0);

            var historyButton = new FlatButton(ButtonKind.Ghost) { Text = "History", Icon = Draw.Clock, Height = 40, Dock = DockStyle.Fill, Margin = new Padding(0), Font = Theme.SemiBold(10f) };
            gardenButton = new FlatButton(ButtonKind.Ghost) { Text = "Garden", Icon = Draw.Sprout, Height = 40, Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0), Font = Theme.SemiBold(10f) };
            var settingsButton = new FlatButton(ButtonKind.Ghost) { Size = new Size(40, 40), Margin = new Padding(10, 0, 0, 0), Glyph = Draw.Gear, AccessibleName = "Settings" };
            new ToolTip().SetToolTip(settingsButton, "Settings");
            bottomRow = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Height = 40, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomRow.Controls.Add(historyButton, 0, 0);
            bottomRow.Controls.Add(gardenButton, 1, 0);
            bottomRow.Controls.Add(settingsButton, 2, 0);

            updateLink = Ui.Link("");
            updateLink.Anchor = AnchorStyles.Left;
            updateLink.Margin = new Padding(0, 10, 0, 0);
            updateLink.Visible = false;
            updateStatus = Ui.Small(Theme.Muted);
            saveError = Ui.Small(Theme.AccentText);

            Ui.AddRow(root, stoneRow);
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
            historyButton.Click += (s, e) => ShowHistory(null);
            gardenButton.Click += (s, e) => ShowGarden(null);
            settingsButton.Click += (s, e) => ShowSettings();
            updateLink.LinkClicked += (s, e) => app.InstallUpdate();
            stoneToggle.CheckedChanged += (s, e) => { if (!syncing) app.SetStoneOn(stoneToggle.Checked); };
            stoneLabel.Click += (s, e) => stoneToggle.Checked = !stoneToggle.Checked;
            ring.Visited += app.AddVisitor;
            ApplyStoneMode();
        }

        public void ApplyStoneMode()
        {
            bool on = app.Settings.StoneOn;
            syncing = true;
            stoneToggle.Checked = on;
            syncing = false;
            ring.Plain = !on;
            gardenButton.Visible = on;
            bottomRow.ColumnStyles[1] = on ? new ColumnStyle(SizeType.Percent, 50F) : new ColumnStyle(SizeType.Absolute, 0F);
            if (history != null && !history.IsDisposed) history.GardenShown = on;
            if (!on && garden != null && !garden.IsDisposed) garden.Close();
        }

        public void OnReminder() => ring.Reminder();

        public void Announce(string text, bool special = false) => ring.Announce(text, special);

        public void ShowAway(AwayReport report) => ring.ShowAway(report);

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
            string caption, note;
            switch (tracker.State)
            {
                case TrackState.Counting:
                    int minutes = (int)Math.Ceiling((interval - into) / 60);
                    note = minutes <= 1 ? "Reminder in a moment" : $"{minutes} min to next reminder";
                    state += minutes <= 1 ? "  ·  reminder in a moment" : $"  ·  reminder in {minutes} min";
                    caption = "THIS SESSION";
                    break;
                case TrackState.Paused:
                    caption = "PAUSED";
                    note = "Paused";
                    break;
                default:
                    caption = tracker.CanResume ? "LAST SESSION" : "READY";
                    note = "Not counting";
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
                Note = note,
                BreaksToday = Garden.BreaksOn(tracker.Sessions, tracker.Current, today),
                LastEnd = tracker.Sessions.Count > 0 ? tracker.Sessions.Max(s => s.End) : default(DateTime),
                Keepsakes = Garden.Progress(tracker.Sessions, tracker.Current, today).Earned,
                Name = app.Settings.StoneName,
                Chat = app.Settings.Chattiness,
                BreaksYesterday = Garden.BreaksOn(tracker.Sessions, tracker.Current, today.AddDays(-1)),
                Sun = app.SunToday,
                Southern = app.Settings.HasTown && app.Settings.TownLat < 0,
                Weather = app.Weather,
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
            if (garden != null && !garden.IsDisposed) garden.SetData(app.Tracker.Sessions, app.Tracker.Current, app.Album, interval);
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

        void ShowHistory(DateTime? day)
        {
            Ui.ShowBeside(this, ref history, () =>
            {
                var next = new HistoryForm { GardenShown = app.Settings.StoneOn };
                next.SetSessions(app.Tracker.Sessions, app.Tracker.Current, app.Album, app.Tracker.ReminderIntervalSeconds);
                next.GardenRequested += d => ShowGarden(d);
                return next;
            });
            if (day != null) history.ShowDay(day.Value);
        }

        void ShowGarden(DateTime? day)
        {
            if (!app.Settings.StoneOn) return;
            Ui.ShowBeside(this, ref garden, () =>
            {
                var next = new GardenForm();
                next.SetData(app.Tracker.Sessions, app.Tracker.Current, app.Album, app.Tracker.ReminderIntervalSeconds);
                next.CalendarRequested += d => ShowHistory(d);
                return next;
            });
            if (day != null) garden.ShowDay(day.Value);
        }

        void ShowSettings() => Ui.ShowBeside(this, ref settings, () => new SettingsForm(app));
    }
}
