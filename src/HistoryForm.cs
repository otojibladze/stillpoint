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
        IReadOnlyList<Session> sessions = new List<Session>();
        Session current;
        Album album;
        double interval = 1800;
        bool gardenShown = true;

        public HistoryForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Regular(10f);
            ForeColor = Theme.Ink;
            BackColor = Theme.Background;
            Text = "Pebble history";
            ClientSize = new Size(440, 640);
            MinimumSize = new Size(400, 560);
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
            tabs = new[] { Tab("List"), Tab("Calendar") };
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
            new ToolTip().SetToolTip(dayGarden, "Open this day in the garden");
            dayTitle = new Label { AutoSize = true, Font = Theme.SemiBold(10f), Anchor = AnchorStyles.Left, Margin = new Padding(0, 12, 0, 0) };
            dayTotal = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            var dayHead = Ui.TwoSides(Ui.Row(dayGarden, dayTitle), dayTotal);
            dayHead.Margin = new Padding(0, 0, 0, 8);
            day = new SessionList { Dock = DockStyle.Fill, Margin = new Padding(0), EmptyText = "Nothing counted on this day." };
            var calendarPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0), Visible = false };
            calendarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            calendarPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            calendarPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            calendarPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            calendarPanel.Controls.Add(month, 0, 0);
            calendarPanel.Controls.Add(dayHead, 0, 1);
            calendarPanel.Controls.Add(day, 0, 2);

            var body = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            panels = new Control[] { all, calendarPanel };
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
            dayGarden.Click += (s, e) => GardenRequested?.Invoke(month.Selected);
            ShowTab(0);
        }

        public event Action<DateTime> GardenRequested;

        public bool GardenShown
        {
            get => gardenShown;
            set { gardenShown = value; dayGarden.Visible = value; }
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
        }

        public void ShowDay(DateTime date)
        {
            ShowTab(1);
            month.Selected = date.Date;
            RefreshDay(toTop: true);
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

        void RefreshDay(bool toTop)
        {
            var culture = CultureInfo.CurrentCulture;
            var date = month.Selected;
            var list = sessions.Where(s => s.Start.Date == date).OrderBy(s => s.Start).ToList();
            double total = list.Sum(s => s.Seconds);
            Ui.SetText(dayTitle, date.ToString("dddd d MMMM yyyy", culture));
            Ui.SetText(dayTotal, list.Count == 0 ? "" : $"{Fmt.Hm(total)} in {Count(list.Count)}");
            day.SetRows(list.Select(s => new ListRow { Left = Range(s), Right = Length(s.Seconds) }).ToList(), toTop);
            if (gardenShown) dayGarden.Set(date <= DateTime.Today ? Garden.Day(sessions, current, date, album, interval) : null, 0);
        }

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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
