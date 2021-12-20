using System;

namespace OpenTkWPFHost
{
    public class PipelineArgs
    {
        /// <summary>
        /// used to indicate render background
        /// </summary>
        public RenderTargetInfo TargetInfo { get; }

        public PipelineArgs(RenderTargetInfo targetInfo)
        {
            TargetInfo = targetInfo;
        }
    }

    public abstract class RenderArgs : PipelineArgs
    {
        protected RenderArgs(RenderTargetInfo targetInfo) : base(targetInfo)
        {

        }
    }

    public class BitmapRenderArgs : RenderArgs
    {
        public PixelBufferInfo BufferInfo { get; set; }

        public BitmapRenderArgs(RenderTargetInfo targetInfo) : base(targetInfo)
        {
        }
    }

    public class DXRenderArgs : RenderArgs
    {
        public IntPtr RenderTargetIntPtr { get; }

        public DXRenderArgs(RenderTargetInfo targetInfo, IntPtr renderTargetIntPtr) : base(targetInfo)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
        }
    }
}