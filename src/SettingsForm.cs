using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class SettingsForm : Form
    {
        readonly TrayApp app;
        readonly Stepper reminderMinutes;
        readonly LinkLabel soundLink;
        readonly ToggleSwitch startWithWindows;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        bool syncing;

        public SettingsForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            Ui.SetupWindow(this, "Stillpoint settings", new Size(400, 480));
            FormBorderStyle = FormBorderStyle.FixedSingle;

            var root = Ui.Column(new Padding(22, 8, 22, 18));
            root.SuspendLayout();

            reminderMinutes = new Stepper { Minimum = Settings.MinReminderMinutes, Maximum = Settings.MaxReminderMinutes, Margin = new Padding(0) };
            soundLink = Ui.Link("");
            soundLink.Margin = new Padding(0, 0, 10, 0);
            var play = new PlayButton { Anchor = AnchorStyles.Right, Margin = new Padding(0), AccessibleName = "Play sound" };
            soundLink.Anchor = AnchorStyles.Right;
            var preview = Ui.Link("Preview reminder");

            var reminder = new Card();
            reminder.AddRow(Ui.Text("Every"), reminderMinutes);
            reminder.AddRow(Ui.Text("Sound"), Ui.Row(soundLink, play));
            reminder.AddRow(Muted("See how it looks"), preview);

            startWithWindows = new ToggleSwitch { Margin = new Padding(0) };
            var autoLabel = Ui.Text("Start with Windows");
            autoLabel.Cursor = Cursors.Hand;
            var dataLink = Ui.Link("Open folder");
            var general = new Card();
            general.AddRow(autoLabel, startWithWindows);
            general.AddRow(Ui.Text("Your sessions"), dataLink);

            var version = new Label { Text = "Stillpoint " + AppVersion.Text + "  ·", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9f), Margin = new Padding(0) };
            updateLink = Ui.Link("");
            updateLink.Font = Theme.Regular(9f);
            updateLink.Margin = new Padding(4, 0, 0, 0);
            var footer = Ui.Row(version, updateLink);
            footer.Anchor = AnchorStyles.None;
            footer.Margin = new Padding(0, 16, 0, 0);
            updateStatus = Ui.Small(Theme.Muted);
            updateStatus.TextAlign = ContentAlignment.MiddleCenter;
            updateStatus.Anchor = AnchorStyles.None;

            Ui.AddRow(root, new Label { Text = "Settings", AutoSize = true, Font = Theme.Light(21f), Margin = new Padding(0, 0, 0, 12) });
            Ui.AddRow(root, Caption("Reminder"));
            Ui.AddRow(root, reminder);
            Ui.AddRow(root, Caption("General"));
            Ui.AddRow(root, general);
            Ui.AddRow(root, footer);
            Ui.AddRow(root, updateStatus);

            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            reminderMinutes.ValueChanged += (s, e) => { if (!syncing) app.SetReminderMinutes(reminderMinutes.Value); };
            soundLink.LinkClicked += (s, e) => ShowSoundMenu();
            play.Click += (s, e) => Sounds.Play(Sounds.Normalize(app.Settings.Sound));
            preview.LinkClicked += (s, e) => app.ShowReminder(app.Settings.ReminderMinutes * 60, preview: true);
            startWithWindows.CheckedChanged += (s, e) => { if (!syncing) { app.SetAutoStart(startWithWindows.Checked); SyncControls(); } };
            autoLabel.Click += (s, e) => startWithWindows.Checked = !startWithWindows.Checked;
            dataLink.LinkClicked += (s, e) => app.OpenDataFolder();
            updateLink.LinkClicked += (s, e) =>
            {
                if (app.UpdateAvailable != null) app.InstallUpdate();
                else app.CheckForUpdates(manual: true);
            };

            SyncControls();
            UpdateView();
            Shown += (s, e) => ActiveControl = null;
        }

        public void SyncControls()
        {
            syncing = true;
            reminderMinutes.Value = app.Settings.ReminderMinutes;
            startWithWindows.Checked = app.AutoStartEnabled;
            soundLink.Text = Sounds.Label(Sounds.Normalize(app.Settings.Sound)) + "  ›";
            syncing = false;
        }

        public void UpdateView()
        {
            string link = app.UpdateAvailable != null ? $"Update to {app.UpdateAvailable}" : "Check for updates";
            Ui.SetText(updateLink, link);
            updateLink.Enabled = !app.UpdateBusy;
            bool shown = updateStatus.Visible;
            Ui.SetLine(updateStatus, app.UpdateStatus);
            if (updateStatus.Visible != shown || !IsHandleCreated) FitHeight();
        }

        void FitHeight()
        {
            int height = Controls[0].GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
            if (ClientSize.Height != height) ClientSize = new Size(ClientSize.Width, height);
        }

        void ShowSoundMenu()
        {
            string current = Sounds.Normalize(app.Settings.Sound);
            var menu = new ContextMenuStrip { Font = Theme.Regular(9.5f), ShowImageMargin = false, ShowCheckMargin = true };
            foreach (var id in new[] { Sounds.Off }.Concat(Sounds.All))
            {
                string pick = id;
                var item = new ToolStripMenuItem(Sounds.Label(id)) { Checked = id == current };
                item.Click += (s, e) => { app.SetSound(pick); SyncControls(); };
                menu.Items.Add(item);
                if (id == Sounds.Off) menu.Items.Add(new ToolStripSeparator());
            }
            menu.Closed += (s, e) => BeginInvoke((Action)menu.Dispose);
            menu.Show(soundLink, new Point(0, soundLink.Height));
        }

        static Label Muted(string text)
        {
            var label = Ui.Text(text);
            label.ForeColor = Theme.Muted;
            return label;
        }

        static Label Caption(string text) => new Label
        {
            Text = text.ToUpperInvariant(),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.SemiBold(8f),
            Margin = new Padding(4, 14, 0, 7),
        };

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
