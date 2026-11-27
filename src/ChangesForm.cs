using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class ChangesForm : Form
    {
        public ChangesForm()
        {
            SuspendLayout();
            Ui.SetupWindow(this, "What's new", new Size(440, 640));
            ShowInTaskbar = false;
            var root = Ui.Column(new Padding(22, 4, 22, 20));
            Ui.AddRow(root, new ChangeLog { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0) });
            Controls.Add(new ScrollView(root));
            ResumeLayout(false);
            PerformLayout();
        }
    }

    sealed class ChangeLog : Control
    {
        const TextFormatFlags Wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsTranslateTransform;
        const TextFormatFlags Line = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsTranslateTransform;
        readonly Font title = Theme.SemiBold(10f);
        readonly Font small = Theme.Regular(9f);
        readonly List<(Release Release, Rectangle Box, int[] NoteTops)> layout = new List<(Release, Rectangle, int[])>();
        int layoutWidth = -1, layoutHeight;

        public ChangeLog()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            Font = Theme.Regular(9.5f);
        }

        int Pad => Font.Height;
        int Gap => Font.Height * 3 / 4;
        int Bullet => Font.Height;

        void Measure(int width)
        {
            if (width == layoutWidth) return;
            layoutWidth = width;
            layout.Clear();
            int y = 0, textWidth = Math.Max(40, width - Pad * 2 - Bullet);
            foreach (var release in Changes.All)
            {
                int top = y, head = Math.Max(title.Height, small.Height) + Pad * 3 / 2;
                int inner = top + head + Pad / 2;
                var tops = new int[release.Notes.Length];
                for (int i = 0; i < tops.Length; i++)
                {
                    tops[i] = inner;
                    inner += TextRenderer.MeasureText(release.Notes[i], Font, new Size(textWidth, int.MaxValue), Wrap).Height + Font.Height / 3;
                }
                y = inner + Pad * 2 / 3;
                layout.Add((release, new Rectangle(0, top, width, y - top), tops));
                y += Gap;
            }
            layoutHeight = Math.Max(1, y - Gap);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = proposedSize.Width > 1 && proposedSize.Width < 20000 ? proposedSize.Width : Math.Max(Width, 200);
            Measure(width);
            return new Size(width, layoutHeight);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width != layoutWidth) Parent?.PerformLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var clip = Rectangle.Intersect(e.ClipRectangle, ClientRectangle);
            if (clip.Width <= 0 || clip.Height <= 0) return;
            Measure(Width);
            using (var buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, clip))
            {
                var g = buffer.Graphics;
                g.Clear(Parent?.BackColor ?? Theme.Background);
                var culture = CultureInfo.CurrentCulture;
                int textWidth = Math.Max(40, Width - Pad * 2 - Bullet);
                using (var fill = new SolidBrush(Theme.Surface))
                using (var edge = new Pen(Theme.Track))
                using (var line = new Pen(Theme.Line))
                    foreach (var (release, box, tops) in layout)
                    {
                        if (box.Bottom < clip.Top) continue;
                        if (box.Top > clip.Bottom) break;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var card = Draw.Rounded(new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1.5f, box.Height - 1.5f), Font.Height * 0.6f))
                        {
                            g.FillPath(fill, card);
                            g.DrawPath(edge, card);
                        }
                        g.SmoothingMode = SmoothingMode.None;
                        int headH = Math.Max(title.Height, small.Height) + Pad * 3 / 2;
                        var head = new Rectangle(box.X + Pad, box.Y, box.Width - Pad * 2, headH);
                        TextRenderer.DrawText(g, release.Version, title, head, Theme.Ink, Line | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                        if (release.Version == AppVersion.Text)
                        {
                            int w = TextRenderer.MeasureText(g, release.Version, title, Size.Empty, TextFormatFlags.NoPadding).Width;
                            TextRenderer.DrawText(g, "Your version", small, new Rectangle(head.X + w + Pad / 2, head.Y, head.Width, head.Height), Theme.AccentText, Line | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                        }
                        TextRenderer.DrawText(g, release.Date.ToString("d MMMM yyyy", culture), small, head, Theme.Muted, Line | TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
                        g.DrawLine(line, box.X + Pad, box.Y + headH, box.Right - Pad, box.Y + headH);
                        for (int i = 0; i < tops.Length; i++)
                        {
                            float dot = Font.Height * 0.14f, cy = tops[i] + Font.Height / 2f;
                            using (var brush = new SolidBrush(Theme.Muted))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.FillEllipse(brush, box.X + Pad + Bullet * 0.25f - dot, cy - dot, dot * 2, dot * 2);
                                g.SmoothingMode = SmoothingMode.None;
                            }
                            TextRenderer.DrawText(g, release.Notes[i], Font, new Rectangle(box.X + Pad + Bullet, tops[i], textWidth, box.Bottom - tops[i]), Theme.Ink, Wrap);
                        }
                    }
                buffer.Render(e.Graphics);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                title.Dispose();
                small.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
