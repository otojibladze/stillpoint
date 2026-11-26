using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class GardenForm : Form
    {
        readonly Label weekLabel;
        readonly FlatButton nextWeek;
        readonly GardenView hero;
        readonly Label heroTitle;
        readonly Label heroStats;
        readonly Label heroVisitors;
        readonly WeekStrip week;
        readonly Dictionary<Keepsake, (KeepsakeIcon Icon, Label Since, ThinBar Bar)> keepsakeRows = new Dictionary<Keepsake, (KeepsakeIcon, Label, ThinBar)>();
        IReadOnlyList<Session> sessions = new List<Session>();
        Session current;
        Album album;
        double interval = 1800;
        DateTime gardenDate = DateTime.Today;

        public GardenForm()
        {
            SuspendLayout();
            Ui.SetupWindow(this, "Pebble garden", new Size(440, 700));
            MinimumSize = new Size(400, 560);
            StartPosition = FormStartPosition.Manual;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(22, 14, 22, 20), BackColor = Theme.Background, AutoScroll = true };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var title = new Label { Text = "Garden", AutoSize = true, Font = Theme.SemiBold(14f), Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 0, 8) };
            weekLabel = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            var previousWeek = Chevron("‹");
            nextWeek = Chevron("›");
            nextWeek.Margin = new Padding(6, 0, 0, 0);
            var weekHead = Ui.TwoSides(weekLabel, Ui.Row(previousWeek, nextWeek));
            weekHead.Margin = new Padding(0, 0, 0, 10);
            hero = new GardenView { Dock = DockStyle.Fill, Height = 190, Margin = new Padding(0, 0, 0, 10) };
            heroTitle = new Label { AutoSize = true, Font = Theme.SemiBold(10.5f), Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            heroStats = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            var heroHead = Ui.TwoSides(heroTitle, heroStats);
            heroHead.Margin = new Padding(0, 0, 0, 4);
            heroVisitors = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 0, 0, 8) };
            week = new WeekStrip { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 2) };
            var hint = new Label { Text = "Double-click a day to open it in the calendar.", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(8.5f), Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 0, 6) };
            var keepsakes = new Card();
            foreach (var (keepsake, name, need) in new[] { (Keepsake.Moss, "Moss", "7 days in a row"), (Keepsake.Lantern, "Stone lantern", "50 breaks"), (Keepsake.Pond, "Pond", "100 breaks"), (Keepsake.Baby, "Baby pebble", "30 days") })
            {
                var icon = new KeepsakeIcon(keepsake) { Margin = new Padding(0, 0, 10, 0) };
                var label = new Label { Text = name, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
                var note = new Label { Text = need, AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(8.5f), Margin = new Padding(6, 7, 0, 0) };
                var since = new Label { AutoSize = true, ForeColor = SpiritArt.Hex(0x4F6B3D), Font = Theme.SemiBold(9f), Anchor = AnchorStyles.Right, Margin = new Padding(0), Visible = false };
                var bar = new ThinBar { Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, 2, 0) };
                keepsakes.AddRow(Ui.Row(icon, label, note), Ui.Row(since, bar));
                keepsakeRows[keepsake] = (icon, since, bar);
            }
            foreach (var c in new Control[] { title, weekHead, hero, heroHead, heroVisitors, week, hint, Caption("Keepsakes"), keepsakes })
            {
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.Controls.Add(c, 0, root.RowCount++);
            }
            Controls.Add(root);
            ResumeLayout(false);
            PerformLayout();

            previousWeek.Click += (s, e) => { gardenDate = gardenDate.AddDays(-7); RefreshGarden(); };
            nextWeek.Click += (s, e) => { gardenDate = Min(DateTime.Today, gardenDate.AddDays(7)); RefreshGarden(); };
            week.DayClicked += d => { gardenDate = d; RefreshGarden(); };
            week.DayDoubleClicked += d => CalendarRequested?.Invoke(d);
        }

        public event Action<DateTime> CalendarRequested;

        public void SetData(IReadOnlyList<Session> list, Session now, Album visits, double reminderInterval)
        {
            sessions = list;
            current = now;
            album = visits;
            interval = reminderInterval;
            RefreshGarden();
        }

        public void ShowDay(DateTime date)
        {
            gardenDate = Min(DateTime.Today, date.Date);
            RefreshGarden();
        }

        GardenDay GardenOn(DateTime date) => Garden.Day(sessions, current, date, album, interval);

        void RefreshGarden()
        {
            var culture = CultureInfo.CurrentCulture;
            var start = StartOfWeek(gardenDate);
            var days = Enumerable.Range(0, 7).Select(i => GardenOn(start.AddDays(i))).ToList();
            var selected = days.First(d => d.Date == gardenDate.Date);
            bool thisWeek = start == StartOfWeek(DateTime.Today);
            string range = start.Month == start.AddDays(6).Month
                ? $"{start.Day} - {start.AddDays(6).ToString("d MMMM", culture)}"
                : $"{start.ToString("d MMM", culture)} - {start.AddDays(6).ToString("d MMM", culture)}";
            Ui.SetText(weekLabel, (thisWeek ? "This week, " : "") + range);
            nextWeek.Enabled = !thisWeek;
            hero.Set(selected, 0);
            Ui.SetText(heroTitle, gardenDate.Date == DateTime.Today ? "Today" : gardenDate.ToString("dddd d MMMM", culture));
            Ui.SetText(heroStats, selected.Empty ? "Nothing counted" : $"{(selected.Breaks == 1 ? "1 break" : selected.Breaks + " breaks")}, {Fmt.Hm(selected.SitSeconds)}");
            var seen = Visitors.Sorted(selected.Visitors).Select(Visitors.Label).ToList();
            Ui.SetText(heroVisitors, seen.Count == 0 ? "No visitors" : "Visitors: " + string.Join(", ", seen));
            week.SetDays(days, gardenDate);

            var progress = Garden.Progress(sessions, current, DateTime.Today);
            SetKeepsake(Keepsake.Moss, progress.MossSince, progress.Streak / (float)Garden.MossStreak);
            SetKeepsake(Keepsake.Lantern, progress.LanternSince, progress.Breaks / (float)Garden.LanternBreaks);
            SetKeepsake(Keepsake.Pond, progress.PondSince, progress.Breaks / (float)Garden.PondBreaks);
            SetKeepsake(Keepsake.Baby, progress.BabySince, progress.Days / (float)Garden.BabyDays);
        }

        void SetKeepsake(Keepsake keepsake, DateTime? since, float progress)
        {
            var row = keepsakeRows[keepsake];
            bool earned = since != null;
            if (row.Icon.Earned != earned) { row.Icon.Earned = earned; row.Icon.Invalidate(); }
            row.Since.Visible = earned;
            row.Bar.Visible = !earned;
            if (earned) Ui.SetText(row.Since, "Since " + since.Value.ToString("d MMM", CultureInfo.CurrentCulture));
            else row.Bar.Value = progress;
        }

        static DateTime StartOfWeek(DateTime date)
        {
            var first = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int diff = (7 + (date.DayOfWeek - first)) % 7;
            return date.Date.AddDays(-diff);
        }

        static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        static FlatButton Chevron(string text) => new FlatButton(ButtonKind.Ghost) { Text = text, Size = new Size(28, 28), MinimumSize = new Size(28, 28), Margin = new Padding(0), Font = Theme.SemiBold(11f) };

        static Label Caption(string text) => new Label
        {
            Text = text.ToUpperInvariant(),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.SemiBold(8f),
            Margin = new Padding(4, 6, 0, 7),
        };

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
