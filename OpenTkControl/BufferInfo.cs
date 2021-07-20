using System;
using System.Windows;

namespace OpenTkControl
{
    //maybe inefficient over 16 bytes ?
    public struct BufferInfo
    {
        public Int32Rect RepaintRect { get; set; }

        public bool IsResized { get; set; }

        public int BufferSize { get; set; }

        public IntPtr FrameBuffer { get; set; }

        public int Width => RepaintRect.Width;

        public int Height => RepaintRect.Height;
    }
}