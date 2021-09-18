using System.Windows;

namespace OpenTkWPFHost
{
    public readonly struct CanvasInfo
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

        public int ActualWidth { get; }

        public int ActualHeight { get; }

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