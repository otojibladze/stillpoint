using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Stillpoint
{
    static class Icons
    {
        public static readonly Color On = Color.FromArgb(0x1E, 0x8E, 0x4E);
        public static readonly Color Off = Color.FromArgb(0x86, 0x8C, 0x96);
        static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };

        public static Icon Load(Color fill) => new Icon(new MemoryStream(BuildIco(fill)));

        public static Icon Load(Color fill, Size size) => new Icon(new MemoryStream(BuildIco(fill)), size);

        public static byte[] BuildIco(Color fill)
        {
            var images = Sizes.Select(size => size >= 256 ? Png(size, fill) : Dib(size, fill)).ToArray();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)Sizes.Length);
                int offset = 6 + 16 * Sizes.Length;
                for (int i = 0; i < Sizes.Length; i++)
                {
                    byte dim = (byte)(Sizes[i] >= 256 ? 0 : Sizes[i]);
                    writer.Write(dim);
                    writer.Write(dim);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(images[i].Length);
                    writer.Write(offset);
                    offset += images[i].Length;
                }
                foreach (var image in images) writer.Write(image);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static Bitmap Draw(int size, Color fill)
        {
            var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float s = size, inset = s * 0.03f, c = s / 2f;
                using (var brush = new SolidBrush(fill))
                    g.FillEllipse(brush, inset, inset, s - 2 * inset, s - 2 * inset);
                using (var pen = new Pen(Color.White, Math.Max(1.5f, s * 0.1f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pen, c, c, c, s * 0.2f);
                    g.DrawLine(pen, c, c, s * 0.7f, s * 0.64f);
                }
            }
            return bitmap;
        }

        static byte[] Dib(int size, Color fill)
        {
            var pixels = new byte[size * size * 4];
            using (var bitmap = Draw(size, fill))
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                for (int y = 0; y < size; y++)
                    Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * size * 4, size * 4);
                bitmap.UnlockBits(data);
            }
            int maskStride = (size + 31) / 32 * 4;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(40);
                writer.Write(size);
                writer.Write(size * 2);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(0);
                writer.Write(pixels.Length + maskStride * size);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                for (int y = size - 1; y >= 0; y--)
                    writer.Write(pixels, y * size * 4, size * 4);
                var mask = new byte[maskStride];
                for (int y = size - 1; y >= 0; y--)
                {
                    Array.Clear(mask, 0, mask.Length);
                    for (int x = 0; x < size; x++)
                        if (pixels[(y * size + x) * 4 + 3] == 0) mask[x / 8] |= (byte)(0x80 >> (x % 8));
                    writer.Write(mask);
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        static byte[] Png(int size, Color fill)
        {
            using (var bitmap = Draw(size, fill))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
