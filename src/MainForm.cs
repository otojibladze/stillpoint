using System;
using System.Collections.Generic;
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
        const int BaseMain = 400, BaseRail = 58, BaseHeight = 640;
        static readonly string[] PageOrder = { "timer", "garden", "journal", "history", "settings", "changes" };
        readonly NavRail rail;
        readonly Panel main;
        readonly PageStack stack;
        readonly Dictionary<string, Control> pageViews = new Dictionary<string, Control>();
        readonly FlatButton startStop;
        readonly FlatButton resume;
        readonly TableLayoutPanel buttonRow;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        readonly Label saveError;
        HistoryForm history;
        GardenForm garden;
        SettingsForm settings;
        ChangesForm changes;
        JournalForm journal;
        bool syncing;

        public MainForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            Ui.SetupWindow(this, "Pebble", new Size(BaseRail + BaseMain, BaseHeight));
            MinimumSize = new Size(360, 480);
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterScreen;

            var root = Ui.Column(new Padding(26, 12, 26, 18));
            root.SuspendLayout();

            stoneToggle = new ToggleSwitch { Margin = new Padding(8, 0, 0, 0), Anchor = AnchorStyles.None };
            var stoneLabel = new Label { Text = app.Settings.StoneOn ? "Garden mode" : "Simple mode", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9f), Margin = new Padding(0), Anchor = AnchorStyles.None, Cursor = Cursors.Hand };
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
            Ui.AddRow(root, updateLink);
            Ui.AddRow(root, updateStatus);
            Ui.AddRow(root, saveError);

            main = new Panel { BackColor = Theme.Background };
            main.Controls.Add(root);
            stack = new PageStack { Dock = DockStyle.Fill, BackColor = Theme.Background };
            stack.Show("timer", main, 0);
            rail = new NavRail { Dock = DockStyle.Left, Width = BaseRail };
            rail.Add("timer", "Timer", Draw.Stopwatch);
            rail.Add("garden", "Garden", Draw.Sprout);
            rail.Add("history", "History", Draw.Clock);
            rail.Add("settings", "Settings", (g, b, c) => Draw.Gear(g, Rectangle.Inflate(b, b.Width / 2, b.Height / 2), c), bottom: true);
            rail.Active = "timer";

            Controls.Add(stack);
            Controls.Add(rail);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            startStop.Click += (s, e) => app.ToggleRunning();
            resume.Click += (s, e) => app.Resume();
            rail.Picked += key =>
            {
                if (key == "timer") ShowTimer();
                else if (key == "garden") ShowGarden(null);
                else if (key == "history") ShowHistory(null);
                else ShowSettings();
            };
            updateLink.LinkClicked += (s, e) => app.InstallUpdate();
            stoneToggle.CheckedChanged += (s, e) => { stoneLabel.Text = stoneToggle.Checked ? "Garden mode" : "Simple mode"; if (!syncing) app.SetStoneOn(stoneToggle.Checked); };
            stoneLabel.Click += (s, e) => stoneToggle.Checked = !stoneToggle.Checked;
            ring.Visited += app.AddVisitor;
            ring.Touch = app.Touches;
            ring.TouchChanged += app.SaveTouches;
            ring.DreamShared += app.MarkDreamTold;
            ring.HintShown += app.MarkHint;
            ApplyStoneMode();
        }

        public void ApplyStoneMode()
        {
            bool on = app.Settings.StoneOn;
            syncing = true;
            stoneToggle.Checked = on;
            syncing = false;
            ring.Plain = !on;
            rail.SetHidden("garden", !on);
            if (history != null && !history.IsDisposed) history.GardenShown = on;
            if (!on && stack.Current == "garden") ShowTimer();
        }

        public void OnReminder() => ring.Reminder();

        public void Announce(string text, bool special = false) => ring.Announce(text, special);

        public void ShowAway(AwayReport report) => ring.ShowAway(report);

        public void Recall(string text) => ring.Recall(text);

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyWindowSize(app.Settings.WindowSize);
        }

        public void ApplyWindowSize(WindowSize size)
        {
            float k = Settings.Scale(size) * DeviceDpi / 96f;
            rail.Width = (int)Math.Round(BaseRail * k);
            var target = new Size(rail.Width + (int)Math.Round(BaseMain * k), (int)Math.Round(BaseHeight * k));
            if (ClientSize != target) ClientSize = target;
            var area = Screen.FromControl(this).WorkingArea;
            Left = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
            Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        }

        void OpenPage<T>(string key, string title, ref T page, Func<T> make, string railKey = null) where T : Form
        {
            if (page == null || page.IsDisposed)
            {
                page = make();
                page.TopLevel = false;
                page.FormBorderStyle = FormBorderStyle.None;
                page.MinimumSize = Size.Empty;
                page.Dock = DockStyle.Fill;
                var head = new Label { Text = title, AutoSize = false, Dock = DockStyle.Top, Height = (int)Math.Round(Font.Height * 2.6f), Font = Theme.Light(15f), ForeColor = Theme.Ink, BackColor = Theme.Background, Padding = new Padding(20, 10, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
                var view = new Panel { BackColor = Theme.Background };
                view.Controls.Add(page);
                view.Controls.Add(head);
                page.Show();
                pageViews[key] = view;
            }
            stack.Show(key, pageViews[key], Array.IndexOf(PageOrder, key));
            rail.Active = railKey ?? key;
        }

        void ShowTimer()
        {
            stack.Show("timer", main, 0);
            rail.Active = "timer";
            ring.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape && stack.Current != "timer")
            {
                ShowTimer();
                e.Handled = true;
            }
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
            var keep = Garden.Progress(tracker.Sessions, tracker.Current, today);
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
                Keepsakes = keep.Earned,
                TotalBreaks = keep.Breaks,
                Tree = Garden.TreeGrowth(Garden.DaysTogether(tracker.Sessions, today)),
                Weeds = Garden.Weeds(tracker.Sessions, tracker.Current, today),
                Streak = Garden.OnTimeStreak(tracker.Sessions, tracker.Current, today, interval),
                UsualStart = Garden.UsualStart(tracker.Sessions, today),
                StoriesSeen = Stories.Count(app.Album),
                Roam = Roam.Today(tracker.Sessions, tracker.Current, DateTime.Now, app.RoamFrom, app.RoamGaps),
                Stars = app.Stars,
                Dream = app.DreamTonight,
                DreamTold = app.DreamTold,
                BreakEnds = app.BreakEnds,
                Hints = app.Settings.Hints,
                Name = app.Settings.StoneName,
                Chat = app.Settings.Chattiness,
                BreaksYesterday = Garden.BreaksOn(tracker.Sessions, tracker.Current, today.AddDays(-1)),
                Sun = app.SunToday,
                Southern = app.Settings.HasTown && app.Settings.TownLat < 0,
                Weather = app.Weather,
                DugToday = app.Album.On(today).Contains(Visitors.Squirrel),
                Caches = Wildlife.Squirrel(app.Album, today, app.Settings.HasTown && app.Settings.TownLat < 0),
                Lat = app.Settings.TownLat,
                Lon = app.Settings.TownLon,
                Kp = app.Kp,
                Temper = app.Temper,
                Friends = app.FriendsNow,
                SeenToday = app.Album.On(today),
                Fledged = app.Album.DaysWith(Visitors.Fledged).Any(d => d.Year == today.Year),
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
            if (journal != null && !journal.IsDisposed && stack.Current == "journal") journal.RefreshEntries();
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
            OpenPage("history", "History", ref history, () =>
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
            OpenPage("garden", "Garden", ref garden, () =>
            {
                var next = new GardenForm();
                next.SetData(app.Tracker.Sessions, app.Tracker.Current, app.Album, app.Tracker.ReminderIntervalSeconds);
                next.CalendarRequested += d => ShowHistory(d);
                next.JournalFor = app.JournalEntry;
                next.JournalRequested += ShowJournal;
                return next;
            });
            if (day != null) garden.ShowDay(day.Value);
        }

        void ShowJournal() => OpenPage("journal", "Pebble's journal", ref journal, () => new JournalForm(app), "garden");

        void ShowSettings() => OpenPage("settings", "Settings", ref settings, () => new SettingsForm(app));

        public void ShowChanges() => OpenPage("changes", "What's new", ref changes, () => new ChangesForm(), "settings");
    }
}
