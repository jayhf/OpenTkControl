namespace OpenTkControl
{
    public readonly struct CanvasInfo
    {
        public CanvasInfo(int width, int height, double dpiScaleX, double dpiScaleY)
        {
            Width = width;
            Height = height;
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
        }

        public double DpiScaleX { get; }

        public double DpiScaleY { get; }

        public int Width { get; }
        public int Height { get; }

        public bool Equals(CanvasInfo other)
        {
            return DpiScaleX.Equals(other.DpiScaleX) && DpiScaleY.Equals(other.DpiScaleY) && Width == other.Width &&
                   Height == other.Height;
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
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                return hashCode;
            }
        }
    }
}