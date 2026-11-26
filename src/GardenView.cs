using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Stillpoint
{
    static class GardenPicture
    {
        public static string Signature(GardenDay day, float night, Size size) =>
            day == null ? "" : string.Join("|", day.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture), day.Breaks, (int)(day.Clouds * 20), (int)(day.Rain * 20),
                (int)day.Keepsakes, string.Join(",", Visitors.Sorted(day.Visitors)), day.Empty, (int)(night * 20), size.Width, size.Height);

        public static Bitmap Render(GardenDay day, float night, Size size, float radius, Color around)
        {
            var bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new RectangleF(0, 0, size.Width, size.Height);
                GardenArt.RenderDay(g, rect, day, night);
                if (radius > 0)
                    using (var corners = new GraphicsPath { FillMode = FillMode.Alternate })
                    using (var round = Draw.Rounded(new RectangleF(0, 0, size.Width - 0.5f, size.Height - 0.5f), radius))
                    using (var brush = new SolidBrush(around))
                    {
                        corners.AddRectangle(new RectangleF(-1, -1, size.Width + 2, size.Height + 2));
                        corners.AddPath(round, false);
                        g.FillPath(brush, corners);
                    }
            }
            return bitmap;
        }
    }

    sealed class GardenView : Control
    {
        GardenDay day;
        float night;
        Bitmap cache;
        string cacheKey = "";

        public GardenView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public float Radius { get; set; } = 14;
        public string Eyebrow { get; set; }
        public string Title { get; set; }

        public void Set(GardenDay next, float nightLevel)
        {
            day = next;
            night = nightLevel;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var around = Parent?.BackColor ?? Theme.Background;
            g.Clear(around);
            if (day == null || Width < 4 || Height < 4) return;
            float radius = Radius * DeviceDpi / 96f;
            string key = GardenPicture.Signature(day, night, Size) + "|" + radius + "|" + around.ToArgb();
            if (cache == null || key != cacheKey)
            {
                cache?.Dispose();
                cache = GardenPicture.Render(day, night, Size, radius, around);
                cacheKey = key;
            }
            g.DrawImage(cache, new Rectangle(0, 0, cache.Width, cache.Height));
            if (string.IsNullOrEmpty(Title)) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            var shade = new RectangleF(0, Height * 0.45f, Width, Height * 0.55f);
            using (var b = new LinearGradientBrush(RectangleF.Inflate(shade, 0, 1), Color.FromArgb(0, 20, 22, 32), Color.FromArgb(150, 20, 22, 32), LinearGradientMode.Vertical))
                g.FillRectangle(b, shade);
            float pad = 24 * DeviceDpi / 96f;
            using (var titleFont = Theme.Light(21f))
            using (var eyebrowFont = Theme.SemiBold(8.5f))
            using (var titleBrush = new SolidBrush(SpiritArt.Hex(0xF2EEE8)))
            using (var eyebrowBrush = new SolidBrush(SpiritArt.Hex(0xD8D3CC)))
            {
                var titleSize = g.MeasureString(Title, titleFont);
                float titleTop = Height - pad * 0.7f - titleSize.Height;
                g.DrawString(Title, titleFont, titleBrush, pad - 3, titleTop);
                if (!string.IsNullOrEmpty(Eyebrow))
                {
                    var eyebrowSize = g.MeasureString(Eyebrow, eyebrowFont);
                    g.DrawString(Eyebrow, eyebrowFont, eyebrowBrush, pad - 2, titleTop - eyebrowSize.Height + 4);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) cache?.Dispose();
            base.Dispose(disposing);
        }
    }

    sealed class WeekStrip : Control
    {
        readonly Dictionary<DateTime, Bitmap> thumbs = new Dictionary<DateTime, Bitmap>();
        readonly Dictionary<DateTime, string> keys = new Dictionary<DateTime, string>();
        List<GardenDay> days = new List<GardenDay>();
        DateTime selected;

        public WeekStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.SemiBold(9f);
            Height = 84;
            Cursor = Cursors.Hand;
        }

        public event Action<DateTime> DayClicked;

        public void SetDays(List<GardenDay> week, DateTime selectedDay)
        {
            days = week;
            selected = selectedDay.Date;
            Invalidate();
        }

        float Cell => Width / 7f;
        float Thumb => Math.Min(Cell - 8 * DeviceDpi / 96f, 48 * DeviceDpi / 96f);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var back = Parent?.BackColor ?? Theme.Background;
            g.Clear(back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float scale = DeviceDpi / 96f, thumb = Thumb;
            var culture = CultureInfo.CurrentCulture;
            using (var small = Theme.Regular(8.5f))
            using (var ink = new SolidBrush(Theme.Ink))
            using (var muted = new SolidBrush(Theme.Muted))
            using (var centered = new StringFormat { Alignment = StringAlignment.Center })
                for (int i = 0; i < days.Count && i < 7; i++)
                {
                    var day = days[i];
                    float cx = Cell * i + Cell / 2, top = 4 * scale;
                    var rect = new RectangleF(cx - thumb / 2, top, thumb, thumb);
                    bool future = day.Date > DateTime.Today;
                    if (future)
                        using (var path = Draw.Rounded(rect, 9 * scale))
                        using (var fill = new SolidBrush(Theme.Tile)) g.FillPath(fill, path);
                    else
                    {
                        var size = new Size((int)thumb, (int)thumb);
                        string key = GardenPicture.Signature(day, 0, size);
                        if (!thumbs.TryGetValue(day.Date, out var bmp) || keys[day.Date] != key)
                        {
                            bmp?.Dispose();
                            bmp = GardenPicture.Render(day, 0, size, 9 * scale, back);
                            thumbs[day.Date] = bmp;
                            keys[day.Date] = key;
                        }
                        g.DrawImage(bmp, new Rectangle((int)rect.X, (int)rect.Y, bmp.Width, bmp.Height));
                        if (day.Empty)
                            using (var path = Draw.Rounded(rect, 9 * scale))
                            using (var veil = new SolidBrush(Color.FromArgb(120, back))) g.FillPath(veil, path);
                    }
                    if (day.Date == selected)
                        using (var path = Draw.Rounded(RectangleF.Inflate(rect, 2.5f * scale, 2.5f * scale), 11 * scale))
                        using (var pen = new Pen(Theme.Ink, 2 * scale)) g.DrawPath(pen, path);
                    string letter = day.Date.ToString("ddd", culture).Substring(0, 1).ToUpperInvariant();
                    g.DrawString(letter, Font, ink, cx, top + thumb + 6 * scale, centered);
                    string count = future || day.Empty ? "-" : day.Breaks.ToString(culture);
                    g.DrawString(count, small, muted, cx, top + thumb + 21 * scale, centered);
                }
        }

        public event Action<DateTime> DayDoubleClicked;

        DateTime? DayAt(int x)
        {
            int i = (int)(x / Cell);
            return i >= 0 && i < days.Count && days[i].Date <= DateTime.Today ? days[i].Date : (DateTime?)null;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (DayAt(e.X) is DateTime day) DayClicked?.Invoke(day);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (DayAt(e.X) is DateTime day) DayDoubleClicked?.Invoke(day);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (var b in thumbs.Values) b.Dispose();
            base.Dispose(disposing);
        }
    }

    sealed class KeepsakeIcon : Control
    {
        public KeepsakeIcon(Keepsake keepsake)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Keepsake = keepsake;
            Size = new Size(28, 28);
        }

        public Keepsake Keepsake { get; }
        public bool Earned { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? Theme.Surface);
            float size = Math.Min(Width, Height) - 1;
            GardenArt.DrawKeepsakeIcon(e.Graphics, new RectangleF(0.5f, 0.5f, size, size), Keepsake, Earned);
        }
    }

    sealed class ThinBar : Control
    {
        float value;

        public ThinBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(72, 4);
        }

        public float Value
        {
            get => value;
            set { this.value = Math.Max(0, Math.Min(1, value)); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var path = Draw.Rounded(r, r.Height / 2))
            using (var b = new SolidBrush(Theme.Track)) g.FillPath(b, path);
            if (value <= 0) return;
            using (var path = Draw.Rounded(new RectangleF(0, 0, Math.Max(r.Height, r.Width * value), r.Height), r.Height / 2))
            using (var b = new SolidBrush(Theme.Accent)) g.FillPath(b, path);
        }
    }
}
