using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost.Core
{
    public class RenderTargetInfo
    {
        public RenderTargetInfo(int width, int height, double dpiScaleX, double dpiScaleY)
        {
            ActualWidth = width;
            ActualHeight = height;
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
            PixelWidth = (int) Math.Ceiling(width * dpiScaleX);
            PixelHeight = (int) Math.Ceiling(height * dpiScaleY);
            PixelSize = new PixelSize(PixelWidth, PixelHeight);
        }

        public Rect Rect => new Rect(new Size(ActualWidth, ActualHeight));

        public double DpiScaleX { get; }

        public double DpiX => 96 * this.DpiScaleX;

        public double DpiScaleY { get; }

        public double DpiY => 96 * DpiScaleY;

        /// <summary>
        /// device independent
        /// </summary>
        public int ActualWidth { get; }

        public int PixelWidth { get; }

        /// <summary>
        /// device independent
        /// </summary>
        public int ActualHeight { get; }

        public int PixelHeight { get; }

        public PixelSize PixelSize { get; }

        public bool IsEmpty => ActualWidth == 0 || ActualHeight == 0;

        public int BufferSize => PixelWidth * PixelHeight* 4;

        public Int32Rect Int32Rect => new Int32Rect(0, 0, ActualWidth, ActualHeight);

        public RenderTargetBitmap CreateRenderTargetBitmap()
        {
            return new RenderTargetBitmap(this.ActualWidth, this.ActualHeight, this.DpiScaleX * 96, this.DpiScaleY * 96,
                PixelFormats.Pbgra32);
        }

        public GlRenderEventArgs GetRenderEventArgs()
        {
            return new GlRenderEventArgs(PixelWidth, PixelHeight, false);
        }
        
        public bool Equals(RenderTargetInfo other)
        {
            return DpiScaleX.Equals(other.DpiScaleX) && DpiScaleY.Equals(other.DpiScaleY) &&
                   ActualWidth == other.ActualWidth &&
                   ActualHeight == other.ActualHeight;
        }

        public override bool Equals(object obj)
        {
            return obj is RenderTargetInfo other && Equals(other);
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