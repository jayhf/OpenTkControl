using System;

namespace OpenTkWPFHost
{
    public class PipelineArgs
    {
        /// <summary>
        /// used to indicate buffer pixel size
        /// </summary>
        public PixelSize PixelSize { get; set; }
    }

    public abstract class RenderArgs : PipelineArgs
    {
    }

    public class BitmapRenderArgs : RenderArgs
    {
        public PixelBufferInfo BufferInfo { get; set; }
    }

    public class DXRenderArgs : RenderArgs
    {
        public IntPtr RenderTargetIntPtr { get; }

        public DXRenderArgs(PixelSize pixelSize, IntPtr renderTargetIntPtr)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
            PixelSize = pixelSize;
        }
    }
}