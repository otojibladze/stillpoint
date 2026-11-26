using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Stillpoint
{
    sealed class TrayApp : ApplicationContext
    {
        readonly MainForm form;
        readonly NotifyIcon tray;
        readonly Icon iconOn;
        readonly Icon iconOff;
        readonly ToolStripMenuItem startStopItem;
        readonly ToolStripMenuItem resumeItem;
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 1000 };
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly RegisteredWaitHandle showWait;
        readonly HotKey quitKey;
        ReminderPopup popup;
        readonly Random messageRandom = new Random();
        bool challengeOpen;
        double lastClock;
        int ticksSinceSave;
        bool shutDown;
        bool exited;
        bool? trayCounting;
        string trayText;
        readonly string extraArgs;
        int ticksUntilUpdateCheck = FirstUpdateCheckSeconds;
        bool updateBusy;
        Version updateNoticeShownFor;
        const int FirstUpdateCheckSeconds = 20;
        const int UpdateCheckEverySeconds = 6 * 3600;

        public TrayApp(Store store, bool showWindow, WaitHandle showEvent, bool continueSession = false, string extraArgs = "")
        {
            Store = store;
            this.extraArgs = extraArgs;
            Settings = store.LoadSettings();
            Tracker = new UsageTracker(new WindowsSignals(), store.LoadSessions())
            {
                ReminderIntervalSeconds = Settings.ReminderMinutes * 60,
            };
            Tracker.SetRunning(Settings.Running, DateTime.Now, continueSession);

            iconOn = Icons.Load(Icons.On, SystemInformation.SmallIconSize);
            iconOff = Icons.Load(Icons.Off, SystemInformation.SmallIconSize);
            form = new MainForm(this) { Icon = Icons.Load(Icons.On) };
            _ = form.Handle;

            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("Open Stillpoint", null, (s, e) => ShowWindow());
            open.Font = new Font(open.Font, FontStyle.Bold);
            startStopItem = new ToolStripMenuItem("Start counting", null, (s, e) => ToggleRunning());
            resumeItem = new ToolStripMenuItem("Resume counting", null, (s, e) => Resume());
            var folder = new ToolStripMenuItem("Open data folder", null, (s, e) => Process.Start("explorer.exe", "\"" + store.Dir + "\""));
            menu.Items.AddRange(new ToolStripItem[] { open, startStopItem, resumeItem, folder, new ToolStripSeparator(), new ToolStripMenuItem("Exit", null, (s, e) => ExitFromMenu()) });
            tray = new NotifyIcon { Icon = iconOff, Text = "Stillpoint", ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowWindow(); };

            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.SessionEnding += OnSessionEnding;
            showWait = ThreadPool.RegisterWaitForSingleObject(showEvent, (state, timedOut) => OnShowSignal(), null, Timeout.Infinite, false);
            quitKey = new HotKey(HotKey.Control | HotKey.Alt | HotKey.Shift, Keys.Q, QuitNow);
            if (!quitKey.Registered) store.Log("Quit shortcut Ctrl+Alt+Shift+Q is taken by another app");

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

            timer.Tick += (s, e) => OnTick();
            timer.Start();
            OnTick();
            if (showWindow) ShowWindow();
        }

        public Store Store { get; }
        public Settings Settings { get; }
        public UsageTracker Tracker { get; }
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
                    if (manual) UpdateStatus = "You have the latest version.";
                }
                else
                {
                    UpdateStatus = null;
                    if (!manual && !Equals(updateNoticeShownFor, UpdateAvailable) && !shutDown)
                    {
                        updateNoticeShownFor = UpdateAvailable;
                        tray.ShowBalloonTip(8000, $"Stillpoint {UpdateAvailable} is available",
                            "Open Stillpoint and click Update.", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                if (manual) UpdateStatus = "Could not check for updates: " + ex.Message;
            }
            finally
            {
                updateBusy = false;
                Refresh();
            }
        }

        public async void InstallUpdate()
        {
            if (updateBusy || UpdateAvailable == null) return;
            updateBusy = true;
            UpdateStatus = "Downloading...";
            Refresh();
            try
            {
                string exe = Application.ExecutablePath;
                await Updater.InstallAsync(exe, UpdateAvailable);
                string args = (form.Visible ? "" : "--tray ") + "--continue-session --wait-pid "
                    + Process.GetCurrentProcess().Id + extraArgs;
                Process.Start(exe, args);
                Exit();
            }
            catch (Exception ex)
            {
                UpdateStatus = "Update failed: " + ex.Message;
                updateBusy = false;
                Refresh();
            }
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

        public void ShowWindow()
        {
            if (shutDown) return;
            form.SyncControls();
            form.UpdateView();
            form.Show();
            if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
            form.Activate();
        }

        public void OnWindowHidden()
        {
            if (Settings.TrayTipShown) return;
            Settings.TrayTipShown = true;
            SaveSettings();
            tray.ShowBalloonTip(6000, "Stillpoint is still running",
                "It stays in the tray next to the clock. Click the clock icon to open it, right-click it to exit.",
                ToolTipIcon.Info);
        }

        public void ToggleRunning()
        {
            if (Tracker.Running) RequestStop();
            else SetRunning(true);
        }

        public bool RequestStop() => RequestStop(null);

        bool RequestStop(Form above)
        {
            if (!Tracker.Running) return true;
            if (!Verify("Stop counting?", "To stop, solve this puzzle.", "Stop", "Keep counting", above)) return false;
            SetRunning(false);
            return true;
        }

        bool Verify(string title, string hint, string confirmText, string cancelText, Form above = null)
        {
            if (challengeOpen) return false;
            challengeOpen = true;
            try
            {
                using (var challenge = new ChallengeDialog(title, hint, confirmText, cancelText) { Above = above })
                {
                    var owner = form.Visible ? form : null;
                    return challenge.ShowDialog(owner) == DialogResult.OK;
                }
            }
            finally
            {
                challengeOpen = false;
            }
        }

        void ExitFromMenu()
        {
            if (RequestStop()) Exit();
        }

        void QuitNow()
        {
            if (exited) return;
            foreach (var dialog in Application.OpenForms.OfType<ChallengeDialog>().ToList()) dialog.DialogResult = DialogResult.Cancel;
            form.BeginInvoke((Action)(() =>
            {
                if (exited) return;
                if (Tracker.Running) SetRunning(false);
                Exit();
            }));
        }

        public void Resume()
        {
            if (Tracker.CanResume) SetRunning(true, continueLast: true);
        }

        void SetRunning(bool running, bool continueLast = false)
        {
            Apply(() =>
            {
                Tracker.SetRunning(running, DateTime.Now, continueLast);
                Settings.Running = running;
            });
            if (!running) popup?.Close();
        }

        public void SetReminderMinutes(int minutes) => Apply(() =>
        {
            Settings.ReminderMinutes = minutes;
            Tracker.ReminderIntervalSeconds = minutes * 60;
        });

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
            ReminderPopup next = null;
            Func<bool> confirmClose = () => Verify("Close the reminder?", "To close the reminder, solve this puzzle.", "Close reminder", "Cancel", next);
            next = new ReminderPopup(Fmt.Words(sessionMark) + " at the computer", Fmt.Badge(sessionMark), body, confirmClose, preview ? null : (Func<bool>)(() => RequestStop(next)));
            next.FormClosed += (s, e) => { if (popup == s) popup = null; };
            popup = next;
            next.ShowNearTray();
            SystemSounds.Asterisk.Play();
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
            popup?.FollowCursor();
            if (++ticksSinceSave >= 60) SaveUsage();
            if (--ticksUntilUpdateCheck <= 0) CheckForUpdates(false);
            Refresh();
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
            if (trayCounting != counting)
            {
                tray.Icon = counting ? iconOn : iconOff;
                trayCounting = counting;
            }
            string text = Tracker.State == TrackState.Counting
                ? $"Stillpoint: {Fmt.Hm(Tracker.SessionSeconds)} this session"
                : $"Stillpoint: {(Tracker.State == TrackState.Stopped ? "stopped" : "paused")}";
            if (text.Length > 63) text = text.Substring(0, 63);
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

        void Shutdown()
        {
            if (shutDown) return;
            timer.Stop();
            showWait.Unregister(null);
            quitKey.Dispose();
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionEnding -= OnSessionEnding;
            Tracker.Tick(DateTime.Now, TakeDelta());
            SaveUsage();
            SaveSettings();
            shutDown = true;
            popup?.Close();
            tray.Visible = false;
            tray.Dispose();
        }
    }
}
