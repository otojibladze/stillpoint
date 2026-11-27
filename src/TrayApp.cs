using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Stillpoint
{
    sealed class TrayApp : ApplicationContext
    {
        readonly MainForm form;
        readonly NotifyIcon tray;
        readonly Dictionary<StoneMood, Icon> faces = new Dictionary<StoneMood, Icon>();
        StoneMood? trayMood;
        BuddyWindow buddy;
        ReminderPopup awayPopup;
        bool awayAsked;
        DateTime leftAt;
        DateTime? backAt;
        double secondsAtLeave;
        bool askingName;
        const double WalkAwaySeconds = 600;
        readonly ToolStripMenuItem startStopItem;
        readonly ToolStripMenuItem resumeItem;
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 1000 };
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly RegisteredWaitHandle showWait;
        ReminderPopup popup;
        readonly Random messageRandom = new Random();
        double lastClock;
        int ticksSinceSave;
        bool shutDown;
        bool exited;
        string trayText;
        readonly string extraArgs;
        int ticksUntilUpdateCheck = FirstUpdateCheckSeconds;
        bool updateBusy;
        Version updateNoticeShownFor;
        const int FirstUpdateCheckSeconds = 60;
        const int UpdateCheckEverySeconds = 3 * 3600;
        Version readyVersion;
        WeatherNow weather;
        bool weatherBusy;
        int ticksUntilWeather = 5, ticksSinceSeenSave, ticksUntilSpace = 15, friendsVersion = -1;
        float kp = float.NaN;
        DateTime kpFetched = DateTime.MinValue, friendsDate;
        bool kpBusy;
        Dictionary<string, Friend> friends = new Dictionary<string, Friend>();
        const int WeatherEverySeconds = 30 * 60;
        DateTime lastSeen;
        DateTime sunDate;
        SunTimes sun;
        DateTime lastWindowUse = DateTime.MinValue, wokeAt = DateTime.MinValue;
        readonly bool justUpdated;
        bool updateNoteDue;
        int secondsAlive;

        public TrayApp(Store store, bool showWindow, WaitHandle showEvent, bool continueSession = false, string extraArgs = "", bool updated = false)
        {
            justUpdated = updateNoteDue = updated;
            lastSeen = store.LoadSettings().LastSeen;
            Store = store;
            this.extraArgs = extraArgs;
            Settings = store.LoadSettings();
            Tracker = new UsageTracker(new WindowsSignals(), store.LoadSessions())
            {
                ReminderIntervalSeconds = Settings.ReminderMinutes * 60,
            };
            Tracker.SetRunning(Settings.Running, DateTime.Now, continueSession);
            Album = store.LoadAlbum();
            Touches = store.LoadTouches();
            if (Touches.Prune(DateTime.Now)) SaveTouches();
            if (Enum.TryParse(Settings.Temper, out Temper saved)) Temper = saved;

            ApplyTheme();
            foreach (StoneMood mood in Enum.GetValues(typeof(StoneMood)))
                faces[mood] = StoneFaces.Icon(mood, SystemInformation.SmallIconSize.Width);
            form = new MainForm(this) { Icon = StoneFaces.AppIcon() };
            _ = form.Handle;

            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("Open Pebble", null, (s, e) => ShowWindow());
            open.Font = new Font(open.Font, FontStyle.Bold);
            startStopItem = new ToolStripMenuItem("Start counting", null, (s, e) => ToggleRunning());
            resumeItem = new ToolStripMenuItem("Resume counting", null, (s, e) => Resume());
            var folder = new ToolStripMenuItem("Open data folder", null, (s, e) => OpenDataFolder());
            menu.Items.AddRange(new ToolStripItem[] { open, startStopItem, resumeItem, folder, new ToolStripSeparator(), new ToolStripMenuItem("Exit", null, (s, e) => ExitFromMenu()) });
            tray = new NotifyIcon { Icon = faces[StoneMood.Asleep], Text = "Pebble", ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowWindow(); };

            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.SessionEnding += OnSessionEnding;
            SystemEvents.SessionEnded += OnSessionEnded;
            showWait = ThreadPool.RegisterWaitForSingleObject(showEvent, (state, timedOut) => OnShowSignal(), null, Timeout.Infinite, false);


            if (!Settings.FirstRunDone)
            {
                Settings.FirstRunDone = true;
                TrySetAutoStart(true);
                SaveSettings();
            }
            else
            {
                try
                {
                    AutoStart.MigrateLegacy();
                    AutoStart.RefreshPath();
                }
                catch (Exception ex)
                {
                    store.Log(ex);
                }
            }

            faceTimer.Tick += (s, e) => { faceTimer.Stop(); trayMood = null; Refresh(); };
            timer.Tick += (s, e) => OnTick();
            timer.Start();
            SyncBuddy();
            OnTick();
            if (showWindow) ShowWindow();
            if (updated) tray.ShowBalloonTip(5000, "Pebble updated to " + AppVersion.Text, Changes.Of(AppVersion.Text)?.Notes[0] ?? "Your sessions and garden are safe.", ToolTipIcon.Info);
        }

        public Store Store { get; }
        public Settings Settings { get; }
        public UsageTracker Tracker { get; }
        public Album Album { get; }
        public Touches Touches { get; }
        bool Southern => Settings.HasTown && Settings.TownLat < 0;
        string cacheKey = "";
        List<StarDay> stars = new List<StarDay>();
        List<DateTime> breakEnds = new List<DateTime>();
        (DateTime Night, string Key, Dream Dream) dreamCache;

        public void SaveTouches()
        {
            try { Store.SaveTouches(Touches); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { SaveError = ex.Message; }
        }

        void RefreshCaches()
        {
            string key = Tracker.Sessions.Count + "|" + (Tracker.Current == null) + "|" + DateTime.Today.ToString("yyyyMMdd");
            if (key == cacheKey) return;
            cacheKey = key;
            stars = Constellation.Stars(Tracker.Sessions, Tracker.Current);
            breakEnds = Tracker.Sessions.Where(s => s != Tracker.Current).Select(s => s.End).OrderBy(e => e).ToList();
        }

        public List<StarDay> Stars { get { RefreshCaches(); return stars; } }

        public List<DateTime> BreakEnds { get { RefreshCaches(); return breakEnds; } }

        public DayFacts FactsFor(DateTime day) => Journal.Facts(Tracker.Sessions, Tracker.Current, Album, day, Tracker.ReminderIntervalSeconds, Southern);

        public string JournalEntry(DateTime day)
        {
            var facts = FactsFor(day);
            return Journal.Entry(facts, day.Date < DateTime.Today ? Dreams.Of(facts) : null);
        }

        public Dream DreamTonight
        {
            get
            {
                var night = SkyEvents.NightOf(DateTime.Now);
                string key = Tracker.Sessions.Count + "|" + Album.Version;
                if (dreamCache.Dream == null || dreamCache.Night != night || dreamCache.Key != key) dreamCache = (night, key, Dreams.Of(FactsFor(night)));
                return dreamCache.Dream;
            }
        }

        public bool DreamTold => Settings.DreamTold == SkyEvents.NightOf(DateTime.Now).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        public void MarkDreamTold(DateTime night)
        {
            Settings.DreamTold = night.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            SaveSettings();
        }

        public void MarkHint(string id)
        {
            var hints = (Settings.Hints ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (hints.Contains(id)) return;
            hints.Add(id);
            Settings.Hints = string.Join(",", hints);
            SaveSettings();
        }

        public bool Away => Tracker.IsCounting && Tracker.IdleSeconds >= WalkAwaySeconds;

        public GardenDay GardenOn(DateTime day) => Garden.Day(Tracker.Sessions, Tracker.Current, day, Album, Tracker.ReminderIntervalSeconds);

        public void AddVisitor(string visitor)
        {
            if (!Album.Add(DateTime.Today, visitor)) return;
            try
            {
                Store.SaveAlbum(Album);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                SaveError = ex.Message;
            }
        }


        public void AskName(IWin32Window owner, bool rename)
        {
            if (askingName) return;
            askingName = true;
            try
            {
                using (var dialog = new NameForm(Settings.StoneName, rename))
                {
                    var result = dialog.ShowDialog(owner);
                    if (result == DialogResult.OK) Settings.StoneName = dialog.StoneName;
                    Settings.StoneNameAsked = true;
                    SaveSettings();
                }
            }
            finally
            {
                askingName = false;
            }
            form.SyncControls();
            Refresh();
        }

        public WeatherNow Weather => Settings.RealWeather && Settings.HasTown ? weather : null;

        public Temper Temper { get; private set; } = Temper.Easygoing;

        public float Kp => (DateTime.Now - kpFetched).TotalHours < 4 ? kp : float.NaN;

        public Dictionary<string, Friend> FriendsNow
        {
            get
            {
                if (friendsVersion != Album.Version || friendsDate != DateTime.Today)
                {
                    friends = Friends.All(Album, DateTime.Today).ToDictionary(f => f.Species);
                    friendsVersion = Album.Version;
                    friendsDate = DateTime.Today;
                }
                return friends;
            }
        }

        async void RefreshSpace()
        {
            ticksUntilSpace = 3600;
            if (kpBusy || !Settings.StoneOn || !Settings.RealWeather || !Settings.HasTown) return;
            kpBusy = true;
            try
            {
                float value = await RealWeather.FetchKp();
                if (!float.IsNaN(value)) { kp = value; kpFetched = DateTime.Now; }
            }
            catch (Exception ex)
            {
                Store.Log("Kp: " + ex.Message);
            }
            finally
            {
                kpBusy = false;
            }
        }

        void UpdateTemper()
        {
            float score = Character.Score(Tracker.Sessions, Tracker.Current, DateTime.Today, Tracker.ReminderIntervalSeconds);
            bool first = Settings.Temper.Length == 0;
            var next = first ? Character.Of(score) : Character.Settle(score, Temper);
            if (!first && next == Temper) return;
            Temper = next;
            Settings.Temper = next.ToString();
            SaveSettings();
            form.SyncControls();
            if (!first && Settings.StoneOn) form.Announce(Character.Shift(next));
        }

        void MaybeRecall()
        {
            if (!Settings.StoneOn || !form.Visible) return;
            string today = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (Settings.MemoryDay == today) return;
            Settings.MemoryDay = today;
            SaveSettings();
            var memory = Memories.Today(Tracker.Sessions, Tracker.Current, Album, DateTime.Today, Tracker.ReminderIntervalSeconds, Temper, s => FriendsNow.TryGetValue(s, out var f) ? f : null);
            if (memory != null) form.Recall(memory.Text);
        }

        public SunTimes SunToday
        {
            get
            {
                if (!Settings.HasTown) return default(SunTimes);
                if (sunDate != DateTime.Today) { sunDate = DateTime.Today; sun = Sky.Sun(sunDate, Settings.TownLat, Settings.TownLon); }
                return sun;
            }
        }

        public void AskTown(IWin32Window owner)
        {
            using (var dialog = new TownForm(Settings.TownName))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK) return;
                var town = dialog.Town;
                Settings.TownName = town?.Name ?? "";
                Settings.TownLat = town?.Lat ?? double.NaN;
                Settings.TownLon = town?.Lon ?? double.NaN;
                if (town == null) Settings.RealWeather = false;
                sunDate = DateTime.MinValue;
                weather = null;
                SaveSettings();
            }
            form.SyncControls();
            RefreshWeather();
            RefreshSpace();
            Refresh();
        }

        public void SetRealWeather(IWin32Window owner, bool on)
        {
            if (on && !Settings.HasTown) AskTown(owner);
            Settings.RealWeather = on && Settings.HasTown;
            SaveSettings();
            form.SyncControls();
            RefreshWeather();
            RefreshSpace();
            Refresh();
        }

        async void RefreshWeather()
        {
            ticksUntilWeather = WeatherEverySeconds;
            if (weatherBusy || !Settings.StoneOn || !Settings.RealWeather || !Settings.HasTown) return;
            weatherBusy = true;
            try
            {
                weather = await RealWeather.Fetch(Settings.TownLat, Settings.TownLon);
                CheckFirstSnow();
            }
            catch (Exception ex)
            {
                Store.Log("Weather: " + ex.Message);
            }
            finally
            {
                weatherBusy = false;
            }
        }

        void CheckFirstSnow()
        {
            if (weather == null || (weather.Snowfall <= 0.01f && weather.SnowDepth <= 0.005f)) return;
            var today = DateTime.Today;
            string key = today.Month >= 7 ? today.Year + "-" + (today.Year + 1) : (today.Year - 1) + "-" + today.Year;
            if (Settings.FirstSnow == key) return;
            Settings.FirstSnow = key;
            SaveSettings();
            form.Announce("Look! The first snow of the winter.", special: true);
        }

        void TrackSeen()
        {
            if (!form.Visible || form.WindowState == FormWindowState.Minimized) return;
            var now = DateTime.Now;
            if (Settings.StoneOn && lastSeen != DateTime.MinValue && (now - lastSeen).TotalMinutes >= 20)
            {
                var report = Garden.WhileAway(lastSeen, now, Garden.SeasonOf(now), Weather, new Random());
                foreach (var visit in report.Visits) Album.Add(visit.At, visit.Visitor);
                if (Album.Dirty)
                    try { Store.SaveAlbum(Album); }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { SaveError = ex.Message; }
                form.ShowAway(report);
            }
            lastSeen = now;
            if (++ticksSinceSeenSave >= 60)
            {
                ticksSinceSeenSave = 0;
                Settings.LastSeen = now;
                SaveSettings();
            }
        }

        public void ApplyTheme()
        {
            var now = DateTime.Now;
            bool dark = Settings.DarkMode == DarkMode.On
                || (Settings.DarkMode == DarkMode.Night && Sky.Darkness(now, SunToday) > 0.5f)
                || (Settings.DarkMode == DarkMode.Windows && Theme.WindowsDark());
            Theme.Apply(dark, Settings.SeasonalColors ? Garden.SeasonOf(now) : Season.Autumn);
        }

        public void SetDarkMode(DarkMode mode)
        {
            Settings.DarkMode = mode;
            SaveSettings();
            ApplyTheme();
        }

        public void SetSeasonalColors(bool on)
        {
            Settings.SeasonalColors = on;
            SaveSettings();
            ApplyTheme();
        }

        public void SetStoneOn(bool on)
        {
            Settings.StoneOn = on;
            SaveSettings();
            SyncBuddy();
            form.ApplyStoneMode();
            form.SyncControls();
            if (on)
            {
                lastSeen = DateTime.Now;
                RefreshWeather();
            }
            Refresh();
        }

        public void SetWindowSize(WindowSize size)
        {
            Settings.WindowSize = size;
            SaveSettings();
            form.ApplyWindowSize(size);
        }

        public void SetChattiness(Chattiness chattiness)
        {
            Settings.Chattiness = chattiness;
            SaveSettings();
            Refresh();
        }


        public void SetDesktopBuddy(bool on)
        {
            Settings.DesktopBuddy = on;
            SaveSettings();
            SyncBuddy();
            form.SyncControls();
            Refresh();
        }

        void SyncBuddy()
        {
            if (Settings.DesktopBuddy && Settings.StoneOn && buddy == null && !shutDown)
            {
                buddy = new BuddyWindow();
                buddy.OpenRequested += (s, e) => ShowWindow();
                buddy.HideRequested += (s, e) => SetDesktopBuddy(false);
                buddy.ShowOnTaskbar();
            }
            else if ((!Settings.DesktopBuddy || !Settings.StoneOn) && buddy != null)
            {
                buddy.Close();
                buddy.Dispose();
                buddy = null;
            }
        }

        void AfterShown()
        {
            if (shutDown || !form.Visible) return;
            if (updateNoteDue)
            {
                updateNoteDue = false;
                form.Announce("I learned something new. Pebble " + AppVersion.Text + ".");
            }
            if (!Settings.StoneOn) return;
            if (!Settings.StoneNameAsked)
            {
                AskName(form, rename: false);
                return;
            }
            MaybeRecall();
            var now = DateTime.Now;
            string today = now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (now.Hour < 20 || Settings.EveningCardDate == today) return;
            var day = GardenOn(now.Date);
            if (day.Empty) return;
            Settings.EveningCardDate = today;
            SaveSettings();
            var card = new EveningCard(day, Settings.StoneName, Tracker.ReminderIntervalSeconds) { StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
            card.Location = new Point(form.Left + (form.Width - card.Width) / 2, form.Top + Math.Max(0, (form.Height - card.Height) / 2));
            card.Show(form);
        }

        void ShowAwayPopup(double idle)
        {
            awayAsked = true;
            leftAt = DateTime.Now.AddSeconds(-idle);
            secondsAtLeave = Math.Max(0, Tracker.SessionSeconds - idle);
            backAt = null;
            popup?.Close();
            awayPopup?.Close();
            string since = leftAt.ToString("t", System.Globalization.CultureInfo.CurrentCulture);
            var next = new ReminderPopup("Did you get up?", $"Nothing moved since {since}. If you were away, that time won't count.", Settings.StoneOn ? MiniMood.Look : (MiniMood?)null,
                "I got up", () => AnswerAway(true), "I was here", null);
            next.FormClosed += (s, e) => { if (awayPopup == s) awayPopup = null; };
            awayPopup = next;
            next.ShowNearTray();
        }

        void AnswerAway(bool gotUp)
        {
            if (!gotUp || !Tracker.Running) return;
            Tracker.Tick(DateTime.Now, TakeDelta());
            Tracker.SplitAway(leftAt, backAt ?? DateTime.Now, secondsAtLeave, DateTime.Now);
            SaveUsage();
            Refresh();
        }
        public string SaveError { get; private set; }
        public bool Exiting => shutDown;
        public Version UpdateAvailable { get; private set; }
        public string UpdateStatus { get; private set; }
        public bool UpdateBusy => updateBusy;

        public async void CheckForUpdates(bool manual)
        {
            if (updateBusy) return;
            updateBusy = true;
            ticksUntilUpdateCheck = UpdateCheckEverySeconds;
            if (manual) UpdateStatus = "Checking...";
            Refresh();
            try
            {
                var latest = await Updater.LatestAsync();
                UpdateAvailable = latest != null && latest > Updater.Current ? latest : null;
                if (UpdateAvailable == null)
                {
                    if (manual) UpdateStatus = "You're up to date.";
                }
                else
                {
                    UpdateStatus = null;
                    if (Settings.AutoUpdate && !manual)
                    {
                        if (!Equals(readyVersion, UpdateAvailable)) await DownloadUpdate(UpdateAvailable);
                    }
                    else if (!manual && !Equals(updateNoticeShownFor, UpdateAvailable) && !shutDown)
                    {
                        updateNoticeShownFor = UpdateAvailable;
                        tray.ShowBalloonTip(8000, $"Pebble {UpdateAvailable} is available",
                            "Open Pebble and click Update.", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                if (manual) UpdateStatus = "Couldn't check. Try again later.";
                Store.Log("Update check: " + ex.Message);
            }
            finally
            {
                updateBusy = false;
                Refresh();
            }
        }

        async Task DownloadUpdate(Version version)
        {
            readyVersion = null;
            await Updater.DownloadAsync(Application.ExecutablePath, version);
            readyVersion = version;
        }

        public async void InstallUpdate()
        {
            if (updateBusy || UpdateAvailable == null) return;
            updateBusy = true;
            UpdateStatus = "Downloading...";
            Refresh();
            try
            {
                if (!Equals(readyVersion, UpdateAvailable)) await DownloadUpdate(UpdateAvailable);
                ApplyUpdate();
            }
            catch (Exception ex)
            {
                UpdateStatus = "Update failed. Try again later.";
                Store.Log("Update: " + ex.Message);
                updateBusy = false;
                Refresh();
            }
        }

        void ApplyUpdate()
        {
            string exe = Application.ExecutablePath;
            Updater.Apply(exe);
            readyVersion = null;
            string args = (form.Visible ? "" : "--tray ") + "--continue-session --updated --wait-pid "
                + Process.GetCurrentProcess().Id + extraArgs;
            Process.Start(exe, args);
            Exit();
        }

        bool QuietForUpdate()
        {
            if (popup != null || awayPopup != null || askingName || Form.ActiveForm != null) return false;
            if ((DateTime.Now - lastWindowUse).TotalSeconds < 60) return false;
            return Tracker.IdleSeconds >= 300 || (DateTime.Now - wokeAt).TotalSeconds < 60;
        }

        void TryQuietUpdate()
        {
            if (readyVersion == null || updateBusy || shutDown || !Settings.AutoUpdate || !QuietForUpdate()) return;
            updateBusy = true;
            try
            {
                Tracker.Tick(DateTime.Now, TakeDelta());
                SaveUsage();
                SaveSettings();
                ApplyUpdate();
            }
            catch (Exception ex)
            {
                Store.Log("Automatic update failed: " + ex);
                readyVersion = null;
                updateBusy = false;
            }
        }

        public void SetAutoUpdate(bool on)
        {
            Settings.AutoUpdate = on;
            SaveSettings();
            Refresh();
        }

        public bool AutoStartEnabled
        {
            get
            {
                try { return AutoStart.IsEnabled(); }
                catch (Exception) { return false; }
            }
        }

        public string DescribeState()
        {
            switch (Tracker.State)
            {
                case TrackState.Counting: return $"Counting since {Tracker.SessionStarted:t}";
                case TrackState.Stopped: return Tracker.CanResume ? "Stopped - Start a new session or Resume" : "Stopped - press Start when you sit down";
                default:
                    return (Tracker.Paused & PauseReason.Locked) != 0 ? "Paused while the computer is locked" : "Paused while the computer is asleep";
            }
        }

        public void ShowChanges() => form.ShowChanges();

        public void ShowWindow()
        {
            if (shutDown) return;
            form.SyncControls();
            form.UpdateView();
            form.Show();
            if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
            form.Activate();
            form.BeginInvoke((Action)AfterShown);
        }

        public void OnWindowHidden()
        {
            if (Settings.TrayTipShown) return;
            Settings.TrayTipShown = true;
            SaveSettings();
            tray.ShowBalloonTip(6000, "Pebble is still running",
                "It stays in the tray next to the clock. Click the clock icon to open it, right-click it to exit.",
                ToolTipIcon.Info);
        }

        public void ToggleRunning() => SetRunning(!Tracker.Running);

        void ExitFromMenu()
        {
            if (Tracker.Running) SetRunning(false);
            Exit();
        }
        public void Resume()
        {
            if (!Tracker.CanResume) return;
            var last = Tracker.Sessions.OrderBy(s => s.Start).LastOrDefault();
            var now = DateTime.Now;
            if (last != null && last.End.Date == now.Date && now > last.End)
                Settings.RoamGaps = Roam.AddGap(Settings.RoamGaps, new RoamGap { At = last.End, Sat = last.Seconds, Gap = (now - last.End).TotalSeconds });
            SetRunning(true, continueLast: true);
        }

        public List<RoamGap> RoamGaps => Roam.ParseGaps(Settings.RoamGaps);

        void SetRunning(bool running, bool continueLast = false)
        {
            Apply(() =>
            {
                Tracker.SetRunning(running, DateTime.Now, continueLast);
                Settings.Running = running;
            });
            if (!running)
            {
                popup?.Close();
                awayPopup?.Close();
            }
        }

        public void SetReminderMinutes(int minutes) => Apply(() =>
        {
            Settings.ReminderMinutes = minutes;
            Tracker.ReminderIntervalSeconds = minutes * 60;
        });

        public void OpenDataFolder() => Process.Start("explorer.exe", "\"" + Store.Dir + "\"");

        public void SetSound(string id)
        {
            Settings.Sound = Sounds.Normalize(id);
            SaveSettings();
            Sounds.Play(Settings.Sound);
            Refresh();
        }

        public void SetAutoStart(bool on)
        {
            TrySetAutoStart(on);
            Refresh();
        }

        public void ShowReminder(double sessionMark, bool preview)
        {
            if (shutDown) return;
            popup?.Close();
            string body = Messages.For(sessionMark, messageRandom);
            if (Settings.StoneOn && !string.IsNullOrEmpty(Settings.StoneName)) body += "\n" + Settings.StoneName + " is getting sleepy.";
            var next = new ReminderPopup(Fmt.Words(sessionMark) + " at the computer", body, Settings.StoneOn ? MiniMood.Yawn : (MiniMood?)null, "Keep sitting", null,
                "I got up", preview ? null : (Action)(() => SetRunning(false)));
            next.FormClosed += (s, e) => { if (popup == s) popup = null; };
            popup = next;
            next.ShowNearTray();
            if (!preview) form.OnReminder();
            Sounds.Play(Sounds.Normalize(Settings.Sound));
        }

        public void Exit(bool fromForm = false)
        {
            if (exited) return;
            exited = true;
            Shutdown();
            if (!fromForm && !form.IsDisposed) form.Close();
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            Shutdown();
            base.ExitThreadCore();
        }

        void OnTick()
        {
            Tracker.Tick(DateTime.Now, TakeDelta());
            if (Tracker.TryTakeReminder(out double mark)) ShowReminder(mark, false);
            double idle = Tracker.IdleSeconds;
            if (awayPopup != null && backAt == null && idle < 3) backAt = DateTime.Now.AddSeconds(-idle);
            if (idle < 3) awayAsked = false;
            if (!awayAsked && Tracker.IsCounting && idle >= WalkAwaySeconds) ShowAwayPopup(idle);
            popup?.FollowCursor();
            awayPopup?.FollowCursor();
            if (++ticksSinceSave >= 60) SaveUsage();
            if (--ticksUntilUpdateCheck <= 0) CheckForUpdates(false);
            if (Form.ActiveForm != null) lastWindowUse = DateTime.Now;
            if (--ticksUntilWeather <= 0) RefreshWeather();
            if (--ticksUntilSpace <= 0) RefreshSpace();
            if (secondsAlive % 60 == 5)
            {
                UpdateTemper();
                MaybeRecall();
            }
            TrackSeen();
            if (secondsAlive % 10 == 0) ApplyTheme();
            if (++secondsAlive == 120 && justUpdated) Updater.CleanUp(Application.ExecutablePath);
            TryQuietUpdate();
            Refresh();
            AnimateFace();
        }

        readonly System.Windows.Forms.Timer faceTimer = new System.Windows.Forms.Timer();
        readonly Random faceRandom = new Random();

        void AnimateFace()
        {
            if (shutDown || faceTimer.Enabled || !Settings.StoneOn) return;
            double interval = Tracker.ReminderIntervalSeconds;
            bool near = Tracker.IsCounting && interval > 0 && interval - Tracker.SessionSeconds % interval < 120;
            StoneMood? frame = null;
            int ms = 170;
            if (near && secondsAlive % 15 == 0) { frame = StoneMood.Yawn; ms = 1600; }
            else if (trayMood == StoneMood.Awake && faceRandom.Next(5) == 0) frame = StoneMood.Blink;
            if (frame == null) return;
            tray.Icon = faces[frame.Value];
            trayMood = frame;
            faceTimer.Interval = ms;
            faceTimer.Start();
        }

        void Apply(Action change)
        {
            Tracker.Tick(DateTime.Now, TakeDelta());
            change();
            Tracker.Tick(DateTime.Now, TakeDelta());
            SaveUsage();
            SaveSettings();
            Refresh();
        }

        double TakeDelta()
        {
            double now = clock.Elapsed.TotalSeconds;
            double delta = now - lastClock;
            lastClock = now;
            return delta;
        }

        void Refresh()
        {
            if (shutDown) return;
            bool counting = Tracker.IsCounting;
            var mood = !counting ? StoneMood.Asleep : Tracker.SessionSeconds >= Tracker.ReminderIntervalSeconds ? StoneMood.Sleepy : StoneMood.Awake;
            if (trayMood != mood)
            {
                tray.Icon = faces[mood];
                trayMood = mood;
            }
            string who = !Settings.StoneOn || string.IsNullOrEmpty(Settings.StoneName) ? "" : Settings.StoneName + " · ";
            string text = Tracker.State == TrackState.Counting
                ? $"Pebble: {who}{Fmt.Hm(Tracker.SessionSeconds)} this session"
                : $"Pebble: {who}{(Tracker.State == TrackState.Stopped ? "not counting" : "paused")}";
            if (text.Length > 63) text = text.Substring(0, 63);
            if (buddy != null)
            {
                double interval = Tracker.ReminderIntervalSeconds;
                float drowsy = Tracker.Running && interval > 0 ? (float)(Tracker.SessionSeconds % interval / interval) : 0;
                int hour = DateTime.Now.Hour;
                string tip = (string.IsNullOrEmpty(Settings.StoneName) ? "Pebble" : Settings.StoneName) + "\n"
                    + (counting ? "Sitting " + Fmt.Hm(Tracker.SessionSeconds) : "Not counting") + ". Click to open.";
                buddy.SetState(Tracker.Running, Tracker.State == TrackState.Paused, drowsy, hour >= 22 || hour < 6, tip);
            }
            if (text != trayText)
            {
                tray.Text = text;
                trayText = text;
            }
            startStopItem.Text = Tracker.Running ? "Stop counting" : "Start counting";
            resumeItem.Visible = Tracker.CanResume;
            if (form.Visible) form.UpdateView();
        }

        void SaveUsage()
        {
            ticksSinceSave = 0;
            if (!Tracker.Dirty && SaveError == null) return;
            try
            {
                Store.SaveSessions(Tracker.Sessions);
                Tracker.Dirty = false;
                SaveError = null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                SaveError = ex.Message;
            }
        }

        void SaveSettings()
        {
            try
            {
                Store.SaveSettings(Settings);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                SaveError = ex.Message;
            }
        }

        void TrySetAutoStart(bool on)
        {
            try
            {
                AutoStart.Set(on);
            }
            catch (Exception ex)
            {
                SaveError = "Could not change Start with Windows: " + ex.Message;
            }
        }

        void OnShowSignal()
        {
            try
            {
                if (!shutDown) form.BeginInvoke(new Action(ShowWindow));
            }
            catch (InvalidOperationException)
            {
            }
        }

        void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Tracker.Pause(PauseReason.Locked, DateTime.Now, TakeDelta());
                SaveUsage();
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                wokeAt = DateTime.Now;
                Tracker.Resume(PauseReason.Locked);
                Tracker.Tick(DateTime.Now, TakeDelta());
            }
            Refresh();
        }

        void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Tracker.Pause(PauseReason.Asleep, DateTime.Now, TakeDelta());
                SaveUsage();
            }
            else if (e.Mode == PowerModes.Resume)
            {
                wokeAt = DateTime.Now;
                Tracker.Resume(PauseReason.Asleep);
                Tracker.Tick(DateTime.Now, TakeDelta());
            }
            Refresh();
        }

        void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            Tracker.Tick(DateTime.Now, TakeDelta());
            SaveUsage();
            SaveSettings();
        }

        void OnSessionEnded(object sender, SessionEndedEventArgs e)
        {
            Settings.RoamFrom = DateTime.Now;
            SaveSettings();
        }

        public DateTime RoamFrom => Settings.RoamFrom > bootTime ? Settings.RoamFrom : bootTime;

        readonly DateTime bootTime = Native.BootTime();

        void Shutdown()
        {
            if (shutDown) return;
            timer.Stop();
            showWait.Unregister(null);
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionEnding -= OnSessionEnding;
            SystemEvents.SessionEnded -= OnSessionEnded;
            Tracker.Tick(DateTime.Now, TakeDelta());
            SaveUsage();
            if (form.Visible && form.WindowState != FormWindowState.Minimized) lastSeen = DateTime.Now;
            if (lastSeen != DateTime.MinValue) Settings.LastSeen = lastSeen;
            SaveSettings();
            shutDown = true;
            popup?.Close();
            awayPopup?.Close();
            buddy?.Close();
            buddy = null;
            tray.Visible = false;
            tray.Dispose();
        }
    }
}
