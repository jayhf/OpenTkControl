using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost
{
    public class CanvasInfo
    {
        public CanvasInfo(int width, int height, double dpiScaleX, double dpiScaleY)
        {
            ActualWidth = width;
            ActualHeight = height;
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
            Rect = new Rect(new Size(width, height));
        }

        public Rect Rect { get; }

        public double DpiScaleX { get; }

        public double DpiScaleY { get; }

        /// <summary>
        /// device independent
        /// </summary>
        public int ActualWidth { get; }

        /// <summary>
        /// device independent
        /// </summary>
        public int ActualHeight { get; }

        public bool IsEmpty => ActualWidth == 0 || ActualHeight == 0;

        public RenderTargetBitmap CreateRenderTargetBitmap()
        {
            return new RenderTargetBitmap(this.ActualWidth, this.ActualHeight, this.DpiScaleX * 96, this.DpiScaleY * 96,
                PixelFormats.Pbgra32);
        }

        public PixelSize GetPixelSize()
        {
            var width = (int) Math.Ceiling(this.ActualWidth * this.DpiScaleX);
            var height = (int) Math.Ceiling(this.ActualHeight * this.DpiScaleY);
            return new PixelSize(width, height);
        }

        public bool Equals(CanvasInfo other)
        {
            return DpiScaleX.Equals(other.DpiScaleX) && DpiScaleY.Equals(other.DpiScaleY) &&
                   ActualWidth == other.ActualWidth &&
                   ActualHeight == other.ActualHeight;
        }

        public override bool Equals(object obj)
        {
            return obj is CanvasInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DpiScaleX.GetHashCode();
                hashCode = (hashCode * 397) ^ DpiScaleY.GetHashCode();
                hashCode = (hashCode * 397) ^ ActualWidth;
                hashCode = (hashCode * 397) ^ ActualHeight;
                return hashCode;
            }
        }
    }
}