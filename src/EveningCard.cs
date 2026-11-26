using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class EveningCard : Form
    {
        public EveningCard(GardenDay day, string name, double interval)
        {
            SuspendLayout();
            Ui.SetupWindow(this, "Pebble", new Size(400, 600));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var garden = new GardenView
            {
                Dock = DockStyle.Top,
                Height = 230,
                Radius = 0,
                Margin = new Padding(0),
                Eyebrow = day.Date.ToString("dddd d MMMM", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture),
                Title = string.IsNullOrEmpty(name) ? "Your stone's day" : name + "'s day",
            };
            garden.Set(day, 0.8f);

            var body = Ui.Column(new Padding(24, 18, 24, 22));
            body.Dock = DockStyle.Fill;
            body.SuspendLayout();
            var line = new Label { Text = Garden.Rhythm(day, interval), AutoSize = true, MaximumSize = new Size(350, 0), Font = Theme.Regular(12f), Margin = new Padding(0, 0, 0, 14) };
            var card = new Card();
            card.AddRow(Ui.Text("Sitting"), Value(Fmt.Hm(day.SitSeconds)));
            card.AddRow(Ui.Text("Got up"), Value(day.Breaks == 1 ? "1 time" : day.Breaks + " times"));
            card.AddRow(Ui.Text("Longest stretch"), Value(Fmt.Hm(day.LongestSeconds)));
            var seen = Visitors.Sorted(day.Visitors).Select(Visitors.Label).ToList();
            card.AddRow(Ui.Text("Visitors"), Value(seen.Count == 0 ? "None today" : string.Join(", ", seen)));
            var goodnight = new FlatButton(ButtonKind.Primary) { Text = "Goodnight", Uppercase = true, Height = 42, Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0), Font = Theme.SemiBold(10f) };
            goodnight.Click += (s, e) => Close();
            Ui.AddRow(body, line);
            Ui.AddRow(body, card);
            Ui.AddRow(body, goodnight);

            Controls.Add(body);
            Controls.Add(garden);
            body.ResumeLayout(false);
            body.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
            ClientSize = new Size(ClientSize.Width, garden.Height + body.GetPreferredSize(new Size(ClientSize.Width, 0)).Height);
            AcceptButton = goodnight;
            CancelButton = goodnight;
        }

        static Label Value(string text)
        {
            var label = Ui.Text(text);
            label.Anchor = AnchorStyles.Right;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.MaximumSize = new Size(210, 0);
            return label;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.StyleTitleBar(this, Theme.Background, Theme.Muted);
        }
    }
}
