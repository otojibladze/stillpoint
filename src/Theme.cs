using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stillpoint
{
    static class Theme
    {
        sealed class Palette
        {
            public Color[] Colors;
            public Palette(params int[] rgb) { Colors = Array.ConvertAll(rgb, c => Color.FromArgb((c >> 16) & 255, (c >> 8) & 255, c & 255)); }
        }

        static readonly int[] LightBase = { 0xECEAE7, 0xF6F5F3, 0x1C1A19, 0x3A3735, 0x5F5C5A, 0xD5D2CF, 0xD9D6D3, 0xBDB9B6, 0, 0, 0xE2DFDB, 0xF7F6F4, 0x4F6B3D };
        static readonly int[] DarkBase = { 0x1D1C1B, 0x272624, 0xECE8E3, 0xD4CFC9, 0xA6A19B, 0x3B3936, 0x353330, 0x4C4946, 0, 0, 0x302E2C, 0x1A1918, 0x9CC48A };
        static readonly Dictionary<Season, int[]> Accents = new Dictionary<Season, int[]>
        {
            [Season.Autumn] = new[] { 0xC46040, 0x9A3F22, 0xE0896A },
            [Season.Winter] = new[] { 0x4E7BA6, 0x335C85, 0x8DB3DA },
            [Season.Spring] = new[] { 0xC1607A, 0x974460, 0xE08FA6 },
            [Season.Summer] = new[] { 0x7F9A3A, 0x5A7124, 0xA9C268 },
        };

        static Palette current = Build(false, Season.Autumn);

        static Palette Build(bool dark, Season season)
        {
            var colors = (int[])(dark ? DarkBase : LightBase).Clone();
            var accent = Accents[season];
            colors[8] = accent[0];
            colors[9] = dark ? accent[2] : accent[1];
            return new Palette(colors);
        }

        public static Color Background => current.Colors[0];
        public static Color Surface => current.Colors[1];
        public static Color Ink => current.Colors[2];
        public static Color InkHover => current.Colors[3];
        public static Color Muted => current.Colors[4];
        public static Color Track => current.Colors[5];
        public static Color Line => current.Colors[6];
        public static Color Border => current.Colors[7];
        public static Color Accent => current.Colors[8];
        public static Color AccentText => current.Colors[9];
        public static Color Tile => current.Colors[10];
        public static Color OnInk => current.Colors[11];
        public static Color Positive => current.Colors[12];
        public static bool Dark { get; private set; }
        public static event EventHandler Changed;

        public static void Apply(bool dark, Season season)
        {
            var next = Build(dark, season);
            if (dark == Dark && SameColors(next, current)) return;
            var old = current;
            current = next;
            Dark = dark;
            foreach (Form form in Application.OpenForms)
            {
                if (!form.TopLevel) continue;
                Recolor(form, old, next);
                if (form.IsHandleCreated) StyleTitleBar(form, Background, Muted);
                form.Invalidate(true);
            }
            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static bool WindowsDark()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool SameColors(Palette a, Palette b)
        {
            for (int i = 0; i < a.Colors.Length; i++)
                if (a.Colors[i].ToArgb() != b.Colors[i].ToArgb()) return false;
            return true;
        }

        static Color Map(Color color, Palette from, Palette to)
        {
            for (int i = 0; i < from.Colors.Length; i++)
                if (from.Colors[i].ToArgb() == color.ToArgb()) return to.Colors[i];
            return color;
        }

        static void Recolor(Control control, Palette from, Palette to)
        {
            control.BackColor = Map(control.BackColor, from, to);
            control.ForeColor = Map(control.ForeColor, from, to);
            if (control is LinkLabel link)
            {
                link.LinkColor = Map(link.LinkColor, from, to);
                link.ActiveLinkColor = Map(link.ActiveLinkColor, from, to);
                link.VisitedLinkColor = Map(link.VisitedLinkColor, from, to);
            }
            foreach (Control child in control.Controls) Recolor(child, from, to);
        }

        static readonly Dictionary<string, FontFamily> families = new Dictionary<string, FontFamily>();
        static readonly PrivateFontCollection collection = new PrivateFontCollection();
        static readonly List<IntPtr> pinned = new List<IntPtr>();

        public static Font Light(float points) => Make("Hanken Grotesk Light", "Segoe UI Light", points);
        public static Font Regular(float points) => Make("Hanken Grotesk", "Segoe UI", points);
        public static Font SemiBold(float points) => Make("Hanken Grotesk SemiBold", "Segoe UI Semibold", points);

        public static void LoadFonts()
        {
            var assembly = typeof(Theme).Assembly;
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) continue;
                using (var stream = assembly.GetManifestResourceStream(name))
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    byte[] data = buffer.ToArray();
                    IntPtr memory = Marshal.AllocCoTaskMem(data.Length);
                    Marshal.Copy(data, 0, memory, data.Length);
                    pinned.Add(memory);
                    collection.AddMemoryFont(memory, data.Length);
                    uint count = 0;
                    AddFontMemResourceEx(memory, (uint)data.Length, IntPtr.Zero, ref count);
                }
            }
            foreach (var family in collection.Families) families[family.Name] = family;
        }

        public static void StyleTitleBar(Form form, Color caption, Color text)
        {
            try
            {
                int c = ColorRef(caption), t = ColorRef(text), dark = Dark ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, 20, ref dark, 4);
                DwmSetWindowAttribute(form.Handle, 35, ref c, 4);
                DwmSetWindowAttribute(form.Handle, 36, ref t, 4);
            }
            catch (DllNotFoundException)
            {
            }
        }

        public static void RoundCorners(Form form)
        {
            try
            {
                int round = 2;
                DwmSetWindowAttribute(form.Handle, 33, ref round, 4);
            }
            catch (DllNotFoundException)
            {
            }
        }

        static Font Make(string family, string fallback, float points) =>
            families.TryGetValue(family, out var f) ? new Font(f, points, FontStyle.Regular, GraphicsUnit.Point) : new Font(fallback, points);

        static int ColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        [DllImport("gdi32.dll")]
        static extern IntPtr AddFontMemResourceEx(IntPtr font, uint length, IntPtr reserved, ref uint fonts);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
