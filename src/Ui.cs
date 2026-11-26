using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stillpoint
{
    static class Ui
    {
        public static void SetText(Label label, string text)
        {
            if (label.Text != text) label.Text = text;
        }

        public static void SetLine(Label label, string text)
        {
            label.Visible = text != null;
            if (text != null) SetText(label, text);
        }

        public static Label Small(Color color) => new Label
        {
            AutoSize = true,
            Visible = false,
            ForeColor = color,
            Font = Theme.Regular(9f),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 8, 0, 0),
        };

        public static Label Text(string text, Padding margin = default(Padding)) =>
            new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = margin };

        public static LinkLabel Link(string text) => new LinkLabel
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0),
            LinkColor = Theme.AccentText,
            ActiveLinkColor = Theme.Accent,
            VisitedLinkColor = Theme.AccentText,
            LinkBehavior = LinkBehavior.HoverUnderline,
        };

        public static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Left,
            };
            row.Controls.AddRange(controls);
            return row;
        }

        public static TableLayoutPanel TwoSides(Control left, Control right)
        {
            var table = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            left.Anchor = AnchorStyles.Left;
            right.Anchor = AnchorStyles.Right;
            table.Controls.Add(left, 0, 0);
            table.Controls.Add(right, 1, 0);
            return table;
        }

        public static void AddRow(TableLayoutPanel table, Control control, bool fill = false)
        {
            table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100F) : new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, table.RowCount++);
        }

        public static TableLayoutPanel Column(Padding padding) =>
            new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = padding, BackColor = Theme.Background, ColumnStyles = { new ColumnStyle(SizeType.Percent, 100F) } };

        public static void SetupWindow(Form form, string title, Size client)
        {
            form.AutoScaleDimensions = new SizeF(96F, 96F);
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Font = Theme.Regular(10f);
            form.ForeColor = Theme.Ink;
            form.BackColor = Theme.Background;
            form.Text = title;
            form.ClientSize = client;
            form.MaximizeBox = false;
        }
    }
}
