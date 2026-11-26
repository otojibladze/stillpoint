using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Stillpoint
{
    static class Icons
    {
        static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };

        public static byte[] BuildIco(Func<int, Bitmap> draw) =>
            Ico(Sizes, Sizes.Select(size =>
            {
                using (var bitmap = draw(size)) return size >= 256 ? Png(bitmap) : Dib(bitmap);
            }).ToArray());

        public static Icon FromBitmap(Bitmap bitmap)
        {
            using (var stream = new MemoryStream(Ico(new[] { bitmap.Width }, new[] { Dib(bitmap) })))
                return new Icon(stream);
        }

        static byte[] Ico(int[] sizes, byte[][] images)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
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

        static byte[] Dib(Bitmap bitmap)
        {
            int size = bitmap.Width;
            var pixels = new byte[size * size * 4];
            var data = bitmap.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < size; y++)
                Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * size * 4, size * 4);
            bitmap.UnlockBits(data);
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

        static byte[] Png(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
