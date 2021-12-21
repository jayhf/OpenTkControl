namespace OpenTkWPFHost.Core
{
    public struct PixelSize
    {
        public int Width;
        public int Height;

        public PixelSize(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public bool Equals(PixelSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is PixelSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }
    }
}