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

        public CanvasInfo CanvasInfo { get; }
        
        public DXFrameArgs(PixelSize pixelSize, IntPtr renderTargetIntPtr, CanvasInfo canvasInfo)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
            CanvasInfo = canvasInfo;
            this.PixelSize = pixelSize;
        }
    }
}