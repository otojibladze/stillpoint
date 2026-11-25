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
        public static readonly Color Background = Color.FromArgb(0xEC, 0xEA, 0xE7);
        public static readonly Color Surface = Color.FromArgb(0xF6, 0xF5, 0xF3);
        public static readonly Color Ink = Color.FromArgb(0x1C, 0x1A, 0x19);
        public static readonly Color InkHover = Color.FromArgb(0x3A, 0x37, 0x35);
        public static readonly Color Muted = Color.FromArgb(0x5F, 0x5C, 0x5A);
        public static readonly Color Track = Color.FromArgb(0xD5, 0xD2, 0xCF);
        public static readonly Color Line = Color.FromArgb(0xD9, 0xD6, 0xD3);
        public static readonly Color Border = Color.FromArgb(0xBD, 0xB9, 0xB6);
        public static readonly Color Accent = Color.FromArgb(0xC4, 0x60, 0x40);
        public static readonly Color AccentText = Color.FromArgb(0x9A, 0x3F, 0x22);
        public static readonly Color Tile = Color.FromArgb(0xE2, 0xDF, 0xDB);
        public static readonly Color TileSelected = Color.FromArgb(0xF3, 0xE3, 0xDC);
        public static readonly Color OnInk = Color.FromArgb(0xF7, 0xF6, 0xF4);

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
                int c = ColorRef(caption), t = ColorRef(text);
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
