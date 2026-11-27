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
        readonly Label summary;
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

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(22, 14, 22, 20), BackColor = Theme.Background };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            summary = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Margin = new Padding(0, 0, 0, 10) };
            month = new MonthView { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
            dayGarden = new GardenView { Size = new Size(44, 44), Radius = 9, Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand };
            new ToolTip().SetToolTip(dayGarden, "Open this day in the garden");
            dayTitle = new Label { AutoSize = true, Font = Theme.SemiBold(10f), Anchor = AnchorStyles.Left, Margin = new Padding(0, 12, 0, 0) };
            dayTotal = new Label { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Regular(9.5f), Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            var dayHead = Ui.TwoSides(Ui.Row(dayGarden, dayTitle), dayTotal);
            dayHead.Margin = new Padding(0, 0, 0, 8);
            day = new SessionList { Dock = DockStyle.Top, Margin = new Padding(0), Expand = true, AutoSize = true, EmptyText = "Nothing counted on this day." };

            root.Controls.Add(summary, 0, 0);
            root.Controls.Add(month, 0, 1);
            root.Controls.Add(dayHead, 0, 2);
            root.Controls.Add(day, 0, 3);
            Controls.Add(new ScrollView(root) { FillViewport = true });
            ResumeLayout(false);
            PerformLayout();

            month.SelectedChanged += (s, e) => RefreshDay(toTop: true);
            dayGarden.Click += (s, e) => GardenRequested?.Invoke(month.Selected);
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
            month.RollToday();
            var totals = DayTotals();
            double today = Total(totals, DateTime.Today, 1);
            double weekTotal = Total(totals, DateTime.Today, 7);
            double everything = totals.Values.Sum();
            Ui.SetText(summary, $"Today {Fmt.Hm(today)}   ·   Last 7 days {Fmt.Hm(weekTotal)}   ·   All time {Fmt.Hm(everything)}");
            month.SetTotals(totals);
            RefreshDay(toTop: false);
        }

        public void ShowDay(DateTime date)
        {
            month.Selected = date.Date;
            RefreshDay(toTop: true);
        }

        List<Session> DaySessions(DateTime date) => sessions.Where(s => s.Start.Date == date).OrderBy(s => s.Start).ToList();

        List<ListRow> Rows(IEnumerable<Session> list) => list.Select(s => new ListRow { Left = Range(s), Right = Length(s.Seconds) }).ToList();
        void RefreshDay(bool toTop)
        {
            var culture = CultureInfo.CurrentCulture;
            var date = month.Selected;
            var list = DaySessions(date);
            double total = list.Sum(s => s.Seconds);
            Ui.SetText(dayTitle, date.ToString("dddd d MMMM yyyy", culture));
            Ui.SetText(dayTotal, list.Count == 0 ? "" : $"{Fmt.Hm(total)} in {Count(list.Count)}");
            day.SetRows(Rows(list), toTop);
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


        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
