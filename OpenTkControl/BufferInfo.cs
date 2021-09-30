using System;
using System.Windows;

namespace OpenTkWPFHost
{
    //maybe inefficient over 16 bytes ?
    public class BufferInfo
    {
        public Int32Rect RepaintRect;

        public bool IsResized;

        public int BufferSize;

        public IntPtr FrameBuffer;

        public int Width => RepaintRect.Width;

        public int Height => RepaintRect.Height;

        public int GlBufferPointer;

        public bool HasBuffer;

    }
}