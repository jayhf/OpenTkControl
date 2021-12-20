using System;
using System.Windows;

namespace OpenTkWPFHost
{
    public abstract class FrameArgs : PipelineArgs
    {
        protected FrameArgs(RenderTargetInfo renderTargetInfo) : base(renderTargetInfo)
        {
        }
    }

    public class BitmapFrameArgs : FrameArgs
    {
        public PixelBufferInfo BufferInfo { get; }

        public BitmapFrameArgs(RenderTargetInfo renderTargetInfo, PixelBufferInfo bufferInfo) : base(renderTargetInfo)
        {
            BufferInfo = bufferInfo;
        }
    }

    public class DXFrameArgs:FrameArgs
    {
        public IntPtr RenderTargetIntPtr { get; }

        public DXFrameArgs(IntPtr renderTargetIntPtr, RenderTargetInfo renderTargetInfo) : base(renderTargetInfo)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
        }
    }
}