using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public class BitmapRenderArgs : RenderArgs
    {
        public PixelBufferInfo BufferInfo { get; set; }

        public BitmapRenderArgs(RenderTargetInfo targetInfo) : base(targetInfo)
        {
        }
    }
}