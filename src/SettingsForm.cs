using System;
using System.Collections.Generic;
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
        readonly LinkLabel nameLink;
        readonly LinkLabel chatLink;
        readonly ToggleSwitch stoneSounds;
        readonly ToggleSwitch desktopBuddy;
        readonly ToggleSwitch startWithWindows;
        readonly LinkLabel updateLink;
        readonly Label updateStatus;
        bool syncing;

        public SettingsForm(TrayApp app)
        {
            this.app = app;
            SuspendLayout();
            Ui.SetupWindow(this, "Stillpoint settings", new Size(400, 640));
            FormBorderStyle = FormBorderStyle.FixedSingle;

            var root = Ui.Column(new Padding(22, 8, 22, 18));
            root.SuspendLayout();

            reminderMinutes = new Stepper { Minimum = Settings.MinReminderMinutes, Maximum = Settings.MaxReminderMinutes, Margin = new Padding(0) };
            soundLink = MenuLink();
            soundLink.Margin = new Padding(0, 0, 10, 0);
            var play = new PlayButton { Anchor = AnchorStyles.Right, Margin = new Padding(0), AccessibleName = "Play sound" };
            var preview = Ui.Link("Preview reminder");

            var reminder = new Card();
            reminder.AddRow(Ui.Text("Every"), reminderMinutes);
            reminder.AddRow(Ui.Text("Sound"), Ui.Row(soundLink, play));
            reminder.AddRow(Muted("See how it looks"), preview);

            nameLink = MenuLink();
            chatLink = MenuLink();
            stoneSounds = new ToggleSwitch { Margin = new Padding(0) };
            desktopBuddy = new ToggleSwitch { Margin = new Padding(0) };
            var soundsLabel = TwoLines("Sounds", "Chime, purr, crickets at night", stoneSounds);
            var buddyLabel = TwoLines("Desktop buddy", "A tiny stone on the taskbar", desktopBuddy);
            var stone = new Card();
            stone.AddRow(Ui.Text("Name"), nameLink);
            stone.AddRow(Ui.Text("Chattiness"), chatLink);
            stone.AddRow(soundsLabel, stoneSounds);
            stone.AddRow(buddyLabel, desktopBuddy);

            startWithWindows = new ToggleSwitch { Margin = new Padding(0) };
            var autoLabel = Ui.Text("Start with Windows");
            autoLabel.Cursor = Cursors.Hand;
            autoLabel.Click += (s, e) => startWithWindows.Checked = !startWithWindows.Checked;
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
            Ui.AddRow(root, Caption("Stone"));
            Ui.AddRow(root, stone);
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
            soundLink.LinkClicked += (s, e) => ShowMenu(soundLink, new[] { Sounds.Off }.Concat(Sounds.All).Select(id => (Sounds.Label(id), id == Sounds.Normalize(app.Settings.Sound), (Action)(() => app.SetSound(id)))), separatorAfterFirst: true);
            play.Click += (s, e) => Sounds.Play(Sounds.Normalize(app.Settings.Sound));
            preview.LinkClicked += (s, e) => app.ShowReminder(app.Settings.ReminderMinutes * 60, preview: true);
            nameLink.LinkClicked += (s, e) => app.AskName(this, rename: true);
            chatLink.LinkClicked += (s, e) => ShowMenu(chatLink, ((Chattiness[])Enum.GetValues(typeof(Chattiness))).Select(c => (ChatLabel(c), c == app.Settings.Chattiness, (Action)(() => app.SetChattiness(c)))), separatorAfterFirst: false);
            stoneSounds.CheckedChanged += (s, e) => { if (!syncing) app.SetStoneSounds(stoneSounds.Checked); };
            desktopBuddy.CheckedChanged += (s, e) => { if (!syncing) app.SetDesktopBuddy(desktopBuddy.Checked); };
            startWithWindows.CheckedChanged += (s, e) => { if (!syncing) { app.SetAutoStart(startWithWindows.Checked); SyncControls(); } };
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
            stoneSounds.Checked = app.Settings.StoneSounds;
            desktopBuddy.Checked = app.Settings.DesktopBuddy;
            soundLink.Text = Sounds.Label(Sounds.Normalize(app.Settings.Sound)) + "  ›";
            nameLink.Text = (string.IsNullOrEmpty(app.Settings.StoneName) ? "Give it a name" : app.Settings.StoneName) + "  ›";
            chatLink.Text = ChatLabel(app.Settings.Chattiness) + "  ›";
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

        static string ChatLabel(Chattiness c) => c == Chattiness.Quiet ? "Quiet" : c == Chattiness.Chatty ? "Chatty" : "Normal";

        void ShowMenu(LinkLabel anchor, IEnumerable<(string Label, bool Checked, Action Pick)> items, bool separatorAfterFirst)
        {
            var menu = new ContextMenuStrip { Font = Theme.Regular(9.5f), ShowImageMargin = false, ShowCheckMargin = true };
            foreach (var item in items)
            {
                var entry = new ToolStripMenuItem(item.Label) { Checked = item.Checked };
                var pick = item.Pick;
                entry.Click += (s, e) => { pick(); SyncControls(); };
                menu.Items.Add(entry);
                if (separatorAfterFirst && menu.Items.Count == 1) menu.Items.Add(new ToolStripSeparator());
            }
            menu.Closed += (s, e) => BeginInvoke((Action)menu.Dispose);
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        static LinkLabel MenuLink()
        {
            var link = Ui.Link("");
            link.Anchor = AnchorStyles.Right;
            return link;
        }

        static Control TwoLines(string title, string note, ToggleSwitch toggle)
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Margin = new Padding(0), Anchor = AnchorStyles.Left, Cursor = Cursors.Hand };
            var top = Ui.Text(title);
            var bottom = new Label { Text = note, AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(8.5f), Margin = new Padding(0, 1, 0, 0) };
            panel.Controls.Add(top, 0, 0);
            panel.Controls.Add(bottom, 0, 1);
            EventHandler flip = (s, e) => toggle.Checked = !toggle.Checked;
            panel.Click += flip;
            top.Click += flip;
            bottom.Click += flip;
            top.Cursor = bottom.Cursor = Cursors.Hand;
            return panel;
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
