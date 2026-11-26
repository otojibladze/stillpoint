using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class SettingsForm : Form
    {
        readonly TrayApp app;
        readonly UnderlineBox reminderMinutes;
        readonly LinkLabel soundLink;
        readonly ToggleSwitch startWithWindows;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        bool syncing;

        public SettingsForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            Ui.SetupWindow(this, "Stillpoint settings", new Size(400, 430));
            FormBorderStyle = FormBorderStyle.FixedSingle;

            var root = Ui.Column(new Padding(26, 14, 26, 20));
            root.SuspendLayout();

            reminderMinutes = new UnderlineBox(10f, 44, HorizontalAlignment.Center) { Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 8, 0) };
            var preview = Ui.Link("Preview");
            var reminderRow = Ui.TwoSides(Ui.Row(Ui.Text("Remind me every"), reminderMinutes, Ui.Text("minutes")), preview);

            soundLink = Ui.Link("");
            soundLink.Margin = new Padding(8, 0, 0, 0);
            var playSound = Ui.Link("Play");
            var soundRow = Ui.TwoSides(Ui.Row(Ui.Text("Sound"), soundLink), playSound);
            soundRow.Margin = new Padding(0, 12, 0, 0);

            startWithWindows = new ToggleSwitch { Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 10, 0) };
            var autoLabel = Ui.Text("Start with Windows");
            autoLabel.Cursor = Cursors.Hand;
            var dataLink = Ui.Link("Open data folder");
            var generalRow = Ui.TwoSides(Ui.Row(startWithWindows, autoLabel), dataLink);

            updateLink = Ui.Link("Check for updates");
            var updateRow = Ui.TwoSides(Ui.Text("Version " + AppVersion.Text), updateLink);
            updateStatus = Ui.Small(Theme.Muted);

            Ui.AddRow(root, new Label { Text = "Settings", AutoSize = true, Font = Theme.SemiBold(14f), Margin = new Padding(0, 0, 0, 4) });
            Ui.AddRow(root, Section("Reminder"));
            Ui.AddRow(root, reminderRow);
            Ui.AddRow(root, soundRow);
            Ui.AddRow(root, Section("General"));
            Ui.AddRow(root, generalRow);
            Ui.AddRow(root, Section("Updates"));
            Ui.AddRow(root, updateRow);
            Ui.AddRow(root, updateStatus);

            Controls.Add(root);
            root.ResumeLayout(false);
            root.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

            reminderMinutes.Box.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
            reminderMinutes.Box.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitReminder(); } };
            reminderMinutes.Box.Leave += (s, e) => CommitReminder();
            preview.LinkClicked += (s, e) => app.ShowReminder(app.Settings.ReminderMinutes * 60, preview: true);
            soundLink.LinkClicked += (s, e) => ShowSoundMenu();
            playSound.LinkClicked += (s, e) => Sounds.Play(Sounds.Normalize(app.Settings.Sound));
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

        void FitHeight()
        {
            var root = Controls[0];
            int height = root.GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
            if (ClientSize.Height != height) ClientSize = new Size(ClientSize.Width, height);
        }

        public void SyncControls()
        {
            syncing = true;
            if (!reminderMinutes.Box.Focused) reminderMinutes.Box.Text = app.Settings.ReminderMinutes.ToString(CultureInfo.InvariantCulture);
            startWithWindows.Checked = app.AutoStartEnabled;
            soundLink.Text = Sounds.Label(Sounds.Normalize(app.Settings.Sound)) + " " + (char)0x25BE;
            syncing = false;
        }

        public void UpdateView()
        {
            bool shown = updateStatus.Visible;
            Ui.SetLine(updateStatus, app.UpdateStatus);
            if (updateStatus.Visible != shown || !IsHandleCreated) FitHeight();
            string link = app.UpdateAvailable != null ? $"Update to {app.UpdateAvailable}" : "Check for updates";
            if (updateLink.Text != link) updateLink.Text = link;
            updateLink.Enabled = !app.UpdateBusy;
        }

        void CommitReminder()
        {
            if (syncing) return;
            if (int.TryParse(reminderMinutes.Box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
            {
                minutes = Math.Max(Settings.MinReminderMinutes, Math.Min(Settings.MaxReminderMinutes, minutes));
                if (minutes != app.Settings.ReminderMinutes) app.SetReminderMinutes(minutes);
            }
            reminderMinutes.Box.Text = app.Settings.ReminderMinutes.ToString(CultureInfo.InvariantCulture);
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

        static Label Section(string text) => new Label
        {
            Text = text.ToUpperInvariant(),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.SemiBold(8.5f),
            Margin = new Padding(0, 22, 0, 10),
        };

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
