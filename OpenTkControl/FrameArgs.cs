using System;
using System.Windows;

namespace OpenTkWPFHost
{
    public abstract class FrameArgs : PipelineArgs
    {
        
    }

    public class BitmapFrameArgs : FrameArgs
    {
        public PixelBufferInfo BufferInfo { get; set; }

        public CanvasInfo CanvasInfo { get; set; }
    }

    public class DXFrameArgs:FrameArgs
    {
        public IntPtr RenderTargetIntPtr { get; }

        public DXFrameArgs(PixelSize pixelSize, IntPtr renderTargetIntPtr)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
            this.PixelSize = pixelSize;
        }
    }
}