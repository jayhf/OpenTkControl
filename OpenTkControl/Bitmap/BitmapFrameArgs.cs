using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public class BitmapFrameArgs : FrameArgs
    {
        public PixelBufferInfo BufferInfo { get; }

        public BitmapFrameArgs(RenderTargetInfo renderTargetInfo, PixelBufferInfo bufferInfo) : base(renderTargetInfo)
        {
            BufferInfo = bufferInfo;
        }
    }
}