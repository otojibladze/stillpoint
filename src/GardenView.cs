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
                (int)day.Keepsakes, string.Join(",", Visitors.Sorted(day.Visitors)), day.Empty, (int)(night * 20), size.Width, size.Height, (int)(day.Tree * 400), day.Weeds, day.Caches.Buried, day.Caches.Sprouts);

        public static Bitmap Render(GardenDay day, float night, Size size, float radius, Color around)
        {
            var bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new RectangleF(0, 0, size.Width, size.Height);
                if (size.Width < 160)
                    using (var big = new Bitmap(size.Width * 2, size.Height * 2, PixelFormat.Format32bppPArgb))
                    {
                        using (var bg = Graphics.FromImage(big))
                        {
                            bg.SmoothingMode = SmoothingMode.AntiAlias;
                            GardenArt.RenderDay(bg, new RectangleF(0, 0, big.Width, big.Height), day, night);
                        }
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(big, rect);
                        g.PixelOffsetMode = PixelOffsetMode.Default;
                    }
                else GardenArt.RenderDay(g, rect, day, night);
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
        public bool Expandable { get; set; }
        PictureZoom zoom;

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!Expandable || day == null || (zoom != null && !zoom.IsDisposed)) return;
            zoom = new PictureZoom(day, night, RectangleToScreen(ClientRectangle), Parent?.BackColor ?? Theme.Background);
            zoom.Show(FindForm());
        }

        public string Eyebrow { get; set; }
        public string Title { get; set; }

        Bitmap slideFrom;
        int slideDir;
        readonly System.Diagnostics.Stopwatch slideClock = new System.Diagnostics.Stopwatch();
        System.Windows.Forms.Timer slideTimer;

        public void Set(GardenDay next, float nightLevel)
        {
            if (cache != null && day != null && next != null && next.Date != day.Date && Visible && IsHandleCreated)
            {
                slideFrom?.Dispose();
                slideFrom = (Bitmap)cache.Clone();
                slideDir = next.Date > day.Date ? 1 : -1;
                slideClock.Restart();
                if (slideTimer == null)
                {
                    slideTimer = new System.Windows.Forms.Timer { Interval = 15 };
                    slideTimer.Tick += (s, e) => { Invalidate(); if (slideClock.ElapsedMilliseconds > 360) { slideTimer.Stop(); slideFrom?.Dispose(); slideFrom = null; Invalidate(); } };
                }
                slideTimer.Start();
            }
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
            if (slideFrom != null)
            {
                float k = SpiritArt.Smooth(0, 1, slideClock.ElapsedMilliseconds / 340f);
                int shift = (int)(k * Width) * slideDir;
                g.DrawImage(slideFrom, new Rectangle(-shift, 0, slideFrom.Width, slideFrom.Height));
                g.DrawImage(cache, new Rectangle(slideDir * Width - shift, 0, cache.Width, cache.Height));
            }
            else g.DrawImage(cache, new Rectangle(0, 0, cache.Width, cache.Height));
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
            if (disposing)
            {
                cache?.Dispose();
                slideFrom?.Dispose();
                slideTimer?.Dispose();
                zoom?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    sealed class PictureZoom : Form
    {
        const float Ms = 320;
        readonly Rectangle from, to;
        readonly Bitmap small;
        Bitmap large;
        readonly GardenDay day;
        readonly float night;
        readonly Frames frames;
        readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        bool closing;
        float pos, closeFrom;

        public PictureZoom(GardenDay day, float night, Rectangle from, Color around)
        {
            this.day = day;
            this.night = night;
            this.from = from;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = around;
            DoubleBuffered = true;
            KeyPreview = true;
            Cursor = Cursors.Hand;
            var area = Screen.FromRectangle(from).WorkingArea;
            float k = Math.Max(1, Math.Min(area.Width * 0.75f / from.Width, area.Height * 0.75f / from.Height));
            var size = new Size((int)Math.Round(from.Width * k), (int)Math.Round(from.Height * k));
            to = new Rectangle(area.Left + (area.Width - size.Width) / 2, area.Top + (area.Height - size.Height) / 2, size.Width, size.Height);
            small = GardenPicture.Render(day, night, from.Size, 0, around);
            Bounds = from;
            frames = new Frames(dt => Step());
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ClassStyle |= 0x20000;
                return p;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Theme.RoundCorners(this);
            clock.Restart();
            frames.Start();
        }

        bool Step()
        {
            if (IsDisposed) return false;
            float u = SpiritArt.Clamp(clock.ElapsedMilliseconds / (closing ? Ms * Math.Max(0.4f, closeFrom) : Ms), 0, 1);
            float k = pos = closing ? closeFrom * (1 - SpiritArt.Smooth(0, 1, u)) : SpiritArt.Smooth(0, 1, u);
            Bounds = new Rectangle(
                (int)Math.Round(from.X + (to.X - from.X) * k), (int)Math.Round(from.Y + (to.Y - from.Y) * k),
                (int)Math.Round(from.Width + (to.Width - from.Width) * k), (int)Math.Round(from.Height + (to.Height - from.Height) * k));
            if (u >= 1)
            {
                if (closing) { BeginInvoke((Action)Close); return false; }
                if (large == null) large = GardenPicture.Render(day, night, to.Size, 0, BackColor);
            }
            Invalidate();
            Update();
            return u < 1;
        }

        void Shrink()
        {
            if (closing || IsDisposed) return;
            closing = true;
            large?.Dispose();
            large = null;
            closeFrom = pos;
            clock.Restart();
            frames.Start();
        }

        protected override void OnClick(EventArgs e) { base.OnClick(e); Shrink(); }

        protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Shrink(); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) Shrink();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (large != null && large.Size == ClientSize) { g.DrawImageUnscaled(large, 0, 0); return; }
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(small, new Rectangle(Point.Empty, ClientSize));
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                frames.Dispose();
                small.Dispose();
                large?.Dispose();
            }
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
                        string key = GardenPicture.Signature(day, 0, size) + "|" + back.ToArgb();
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
