namespace OpenTkControl
{
    public readonly struct CanvasInfo
    {
        public CanvasInfo(int width, int height, int dpiScaleX, int dpiScaleY)
        {
            Width = width;
            Height = height;
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
        }

        public int DpiScaleX { get; }

        public int DpiScaleY { get; }

        public int Width { get; }
        public int Height { get; }
    }
}