using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;

namespace OpenTkControlExample
{
    public static class Extension
    {
        public static byte[] GetBytes(this BitmapSource bitmapSource)
        {
            // Stride = (width) x (bytes per pixel)
            // int stride = (int) bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
            var stride = bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel + 7) / 8;
            var pixels = new byte[(int) bitmapSource.PixelHeight * stride];
            bitmapSource.CopyPixels(pixels, stride, 0);
            return pixels;
        }

        public static Bitmap GetBitmap(this BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
                source.PixelWidth,
                source.PixelHeight,
                PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
                new Rectangle(Point.Empty, bmp.Size),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);
            source.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }
    }
}