using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stillpoint
{
    sealed class BuddyWindow : Form
    {
        const int WsExLayered = 0x80000, WsExToolWindow = 0x80, WsExNoActivate = 0x08000000, WsExTopmost = 0x8;
        const float LogicalWidth = 76, LogicalHeight = 70;

        readonly Stopwatch watch = Stopwatch.StartNew();
        readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer { Interval = 31 };
        readonly ToolTip tip = new ToolTip { InitialDelay = 300 };
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        bool known, running, paused, sleeping, hover, hidden;
        float drowsy, hopT = -99;
        string tipText = "";

        public BuddyWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Stillpoint buddy";
            menu.Items.Add("Open Stillpoint", null, (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("Hide desktop buddy", null, (s, e) => HideRequested?.Invoke(this, EventArgs.Empty));
            anim.Tick += (s, e) => { if (Visible) Present(); };
            MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) OpenRequested?.Invoke(this, EventArgs.Empty);
                else if (e.Button == MouseButtons.Right) menu.Show(Cursor.Position);
            };
            MouseEnter += (s, e) => hover = true;
            MouseLeave += (s, e) => hover = false;
        }

        public event EventHandler OpenRequested;
        public event EventHandler HideRequested;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WsExLayered | WsExToolWindow | WsExNoActivate | WsExTopmost;
                return cp;
            }
        }

        public void ShowOnTaskbar()
        {
            Place();
            if (!hidden) Show();
            anim.Start();
            Present();
        }

        void Place()
        {
            float scale = DeviceDpi / 96f;
            int w = (int)Math.Round(LogicalWidth * scale), h = (int)Math.Round(LogicalHeight * scale);
            var area = Screen.PrimaryScreen.WorkingArea;
            Bounds = new Rectangle(area.Right - w - (int)(150 * scale), area.Bottom - h, w, h);
        }

        public void SetState(bool running, bool paused, float drowsy, bool night, string text)
        {
            if (known && this.running && !running) hopT = Now;
            known = true;
            this.running = running;
            this.paused = paused;
            this.drowsy = drowsy;
            sleeping = paused || (!running && night);
            if (text != tipText)
            {
                tipText = text;
                tip.SetToolTip(this, text);
            }
            bool fullScreen = FullScreenAppRunning();
            if (fullScreen != hidden)
            {
                hidden = fullScreen;
                if (hidden) Hide();
                else { Place(); Show(); Present(); }
            }
        }

        float Now => (float)watch.Elapsed.TotalSeconds;

        SpiritPose PoseAt(float t)
        {
            var p = new SpiritPose { Snow = Garden.SeasonOf(DateTime.Today) == Season.Winter ? 1 : 0, Blush = 0.4f };
            float b = SpiritArt.Sin(t * 1.7f) * 0.016f;
            p.Sy += b;
            p.Sx -= b * 0.5f;
            float blink = t % 4.2f;
            if (blink < 0.16f) p.EyeOpen = Math.Abs(blink - 0.08f) / 0.08f;
            if (sleeping)
            {
                p.Eyes = Eyes.Closed; p.Zzz = 1; p.Sprout = 0.35f; p.Blush = 0.2f;
            }
            else if (running)
            {
                float dd = SpiritArt.Smooth(0.2f, 1, drowsy);
                p.EyeOpen *= 1 - 0.62f * dd;
                p.Sprout = 1 - 0.85f * dd;
                if (dd > 0.5f) p.Mouth = Mouth.Flat;
                if (dd > 0.35f && t % 15 < 2.2f)
                {
                    float k = SpiritArt.Sin(t % 15 / 2.2f * SpiritArt.Pi);
                    p.Mouth = Mouth.O; p.Yawn = k; p.EyeOpen *= 1 - k; p.Sy += 0.05f * k;
                }
            }
            else p.Mouth = Mouth.Smile;
            if (hover && !sleeping) { p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Blush = 0.6f; }
            float since = t - hopT;
            if (since >= 0 && since < 1.2f)
            {
                float h = SpiritArt.Sin(since / 1.2f * SpiritArt.Pi);
                p.Dy = -12 * h; p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Sprout = 1;
            }
            return p;
        }

        void Present()
        {
            if (!IsHandleCreated || Width < 2 || Height < 2) return;
            float t = Now;
            using (var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float s = Width / LogicalWidth;
                    g.ScaleTransform(s, s);
                    SpiritArt.DrawSpirit(g, 34, 64, 22, PoseAt(t), t);
                }
                IntPtr screen = GetDC(IntPtr.Zero), memory = CreateCompatibleDC(screen);
                IntPtr hBitmap = bitmap.GetHbitmap(Color.FromArgb(0)), old = SelectObject(memory, hBitmap);
                try
                {
                    var size = new NativeSize { Width = Width, Height = Height };
                    var source = new NativePoint();
                    var target = new NativePoint { X = Left, Y = Top };
                    var blend = new Blend { Op = 0, Flags = 0, SourceAlpha = 255, AlphaFormat = 1 };
                    UpdateLayeredWindow(Handle, screen, ref target, ref size, memory, ref source, 0, ref blend, 2);
                }
                finally
                {
                    SelectObject(memory, old);
                    DeleteObject(hBitmap);
                    DeleteDC(memory);
                    ReleaseDC(IntPtr.Zero, screen);
                }
            }
        }

        static bool FullScreenAppRunning()
        {
            try
            {
                return SHQueryUserNotificationState(out int state) == 0 && (state == 2 || state == 3 || state == 4);
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                anim.Dispose();
                tip.Dispose();
                menu.Dispose();
            }
            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct NativePoint { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        struct NativeSize { public int Width, Height; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Blend { public byte Op, Flags, SourceAlpha, AlphaFormat; }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screenDc, ref NativePoint target, ref NativeSize size, IntPtr sourceDc, ref NativePoint source, int key, ref Blend blend, int flags);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr window);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr window, IntPtr dc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr dc, IntPtr item);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr item);

        [DllImport("shell32.dll")]
        static extern int SHQueryUserNotificationState(out int state);
    }
}
