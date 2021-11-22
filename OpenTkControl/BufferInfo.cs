using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    //maybe inefficient over 16 bytes ?
    public class BufferInfo
    {
        public PixelSize PixelSize;

        public int BufferSize;
        
        public volatile int GlBufferPointer;

        public volatile bool HasBuffer;

        public volatile IntPtr Fence;

        public IntPtr MapBufferIntPtr;
    }
}