using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    //maybe inefficient over 16 bytes ?
    public class BufferInfo
    {
        public Int32Rect RepaintPixelRect;

        public int BufferSize;

        public int PixelWidth => RepaintPixelRect.Width;

        public int PixelHeight => RepaintPixelRect.Height;

        public volatile int GlBufferPointer;

        public volatile bool HasBuffer;

        private volatile IntPtr _fence;

        public int Stride => PixelWidth * 4;

        public IntPtr ClientIntPtr { get; set; }

        public IntPtr Fence
        {
            get => _fence;
            set => _fence = value;
        }
    }
}