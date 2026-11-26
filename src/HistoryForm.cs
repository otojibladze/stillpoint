using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class HistoryForm : Form
    {
        readonly FlatButton[] tabs;
        readonly Control[] panels;
        readonly Label summary;
        readonly SessionList all;
        readonly MonthView month;
        readonly GardenView dayGarden;
        readonly Label dayTitle;
        readonly Label dayTotal;
        readonly SessionList day;
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
        double interval = 3600;
        DateTime gardenDate = DateTime.Today;

        public HistoryForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Regular(10f);
            ForeColor = Theme.Ink;
            BackColor = Theme.Background;
            Text = "Pebble history";
            ClientSize = new Size(440, 700);
            MinimumSize = new Size(400, 620);
            StartPosition = FormStartPosition.Manual;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(22, 14, 22, 20), BackColor = Theme.Background };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var title = new Label { Text = "History", AutoSize = true, Font = Theme.SemiBold(14f), Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            tabs = new[] { Tab("List"), Tab("Calendar"), Tab("Garden") };
            var tabRow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Margin = new Padding(i == 0 ? 0 : 6, 0, 0, 0);
                tabRow.Controls.Add(tabs[i]);
            }
            var head = Ui.TwoSides(title, tabRow);
            head.Margin = new Padding(0, 0, 0, 4);

            summary = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 0, 0, 12) };

            all = new SessionList { Dock = DockStyle.Fill, Margin = new Padding(0) };

            month = new MonthView { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
            dayGarden = new GardenView { Size = new Size(44, 44), Radius = 9, Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand };
            dayTitle = new Label { AutoSize = true, Font = Theme.SemiBold(10f), Anchor = AnchorStyles.Left, Margin = new Padding(0, 12, 0, 0) };
            dayTotal = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            var dayHead = Ui.TwoSides(Ui.Row(dayGarden, dayTitle), dayTotal);
            dayHead.Margin = new Padding(0, 0, 0, 8);
            day = new SessionList { Dock = DockStyle.Fill, Margin = new Padding(0), EmptyText = "Nothing counted on this day." };
            var calendarPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0), Visible = false };
            calendarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            calendarPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            calendarPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            calendarPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            calendarPanel.Controls.Add(month, 0, 0);
            calendarPanel.Controls.Add(dayHead, 0, 1);
            calendarPanel.Controls.Add(day, 0, 2);

            weekLabel = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            var previousWeek = Chevron("‹");
            nextWeek = Chevron("›");
            nextWeek.Margin = new Padding(6, 0, 0, 0);
            var weekHead = Ui.TwoSides(weekLabel, Ui.Row(previousWeek, nextWeek));
            weekHead.Margin = new Padding(0, 0, 0, 10);
            hero = new GardenView { Dock = DockStyle.Fill, Height = 176, Margin = new Padding(0, 0, 0, 10) };
            heroTitle = new Label { AutoSize = true, Font = Theme.SemiBold(10.5f), Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            heroStats = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            var heroHead = Ui.TwoSides(heroTitle, heroStats);
            heroHead.Margin = new Padding(0, 0, 0, 4);
            heroVisitors = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 0, 0, 8) };
            week = new WeekStrip { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
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
            var gardenPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Margin = new Padding(0), Visible = false };
            gardenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            foreach (var c in new Control[] { weekHead, hero, heroHead, heroVisitors, week, Caption("Keepsakes"), keepsakes })
            {
                gardenPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                gardenPanel.Controls.Add(c, 0, gardenPanel.RowCount++);
            }

            var body = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            panels = new Control[] { all, calendarPanel, gardenPanel };
            foreach (var p in panels) body.Controls.Add(p);

            root.Controls.Add(head, 0, 0);
            root.Controls.Add(summary, 0, 1);
            root.Controls.Add(body, 0, 2);
            Controls.Add(root);
            ResumeLayout(false);
            PerformLayout();

            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].Click += (s, e) => ShowTab(index);
            }
            month.SelectedChanged += (s, e) => RefreshDay(toTop: true);
            dayGarden.Click += (s, e) => { gardenDate = month.Selected; RefreshGarden(); ShowTab(2); };
            previousWeek.Click += (s, e) => { gardenDate = gardenDate.AddDays(-7); RefreshGarden(); };
            nextWeek.Click += (s, e) => { gardenDate = Min(DateTime.Today, gardenDate.AddDays(7)); RefreshGarden(); };
            week.DayClicked += d => { gardenDate = d; RefreshGarden(); };
            ShowTab(0);
        }

        public void SetSessions(IReadOnlyList<Session> list, Session now, Album visits, double reminderInterval)
        {
            sessions = list;
            current = now;
            album = visits;
            interval = reminderInterval;
            var totals = DayTotals();
            double today = Total(totals, DateTime.Today, 1);
            double weekTotal = Total(totals, DateTime.Today, 7);
            double everything = totals.Values.Sum();
            Ui.SetText(summary, $"Today {Fmt.Hm(today)}   ·   Last 7 days {Fmt.Hm(weekTotal)}   ·   All time {Fmt.Hm(everything)}");
            all.SetRows(GroupedRows(totals));
            month.SetTotals(totals);
            RefreshDay(toTop: false);
            RefreshGarden();
        }

        void ShowTab(int index)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Kind = i == index ? ButtonKind.Primary : ButtonKind.Ghost;
                tabs[i].Invalidate();
                panels[i].Visible = i == index;
            }
            if (index == 1) month.Focus();
        }

        GardenDay GardenOn(DateTime date) => Garden.Day(sessions, current, date, album, interval);

        void RefreshDay(bool toTop)
        {
            var culture = CultureInfo.CurrentCulture;
            var date = month.Selected;
            var list = sessions.Where(s => s.Start.Date == date).OrderBy(s => s.Start).ToList();
            double total = list.Sum(s => s.Seconds);
            Ui.SetText(dayTitle, date.ToString("dddd d MMMM yyyy", culture));
            Ui.SetText(dayTotal, list.Count == 0 ? "" : $"{Fmt.Hm(total)} in {Count(list.Count)}");
            day.SetRows(list.Select(s => new ListRow { Left = Range(s), Right = Length(s.Seconds) }).ToList(), toTop);
            dayGarden.Set(date <= DateTime.Today ? GardenOn(date) : null, 0);
        }

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

        Dictionary<DateTime, double> DayTotals()
        {
            var totals = new Dictionary<DateTime, double>();
            foreach (var s in sessions)
            {
                totals.TryGetValue(s.Start.Date, out double sum);
                totals[s.Start.Date] = sum + s.Seconds;
            }
            return totals;
        }

        static double Total(Dictionary<DateTime, double> totals, DateTime last, int days)
        {
            double sum = 0;
            for (int i = 0; i < days; i++)
                if (totals.TryGetValue(last.AddDays(-i), out double value)) sum += value;
            return sum;
        }

        List<ListRow> GroupedRows(Dictionary<DateTime, double> totals)
        {
            var rows = new List<ListRow>();
            foreach (var group in sessions.GroupBy(s => s.Start.Date).OrderByDescending(g => g.Key))
            {
                var items = group.OrderByDescending(s => s.Start).ToList();
                rows.Add(new ListRow { Header = true, Left = DayName(group.Key), Middle = Count(items.Count), Right = Fmt.Hm(totals[group.Key]) });
                rows.AddRange(items.Select(s => new ListRow { Left = Range(s), Right = Length(s.Seconds) }));
            }
            return rows;
        }

        static string DayName(DateTime date)
        {
            if (date == DateTime.Today) return "Today";
            if (date == DateTime.Today.AddDays(-1)) return "Yesterday";
            var culture = CultureInfo.CurrentCulture;
            return date.ToString(date.Year == DateTime.Today.Year ? "dddd d MMMM" : "dddd d MMMM yyyy", culture);
        }

        string Range(Session s)
        {
            var culture = CultureInfo.CurrentCulture;
            string start = s.Start.ToString("t", culture);
            string end = s == current ? "now"
                : s.End.Date == s.Start.Date ? s.End.ToString("t", culture)
                : s.End.ToString("ddd ", culture) + s.End.ToString("t", culture);
            return start + "  -  " + end;
        }

        static string Length(double seconds) => seconds < 60 ? (long)seconds + "s" : Fmt.Hm(seconds);

        static string Count(int n) => n == 1 ? "1 session" : n + " sessions";

        static FlatButton Tab(string text) => new FlatButton(ButtonKind.Ghost) { Text = text, AutoSize = true, Height = 30, Margin = new Padding(0), Font = Theme.SemiBold(9.5f) };

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
