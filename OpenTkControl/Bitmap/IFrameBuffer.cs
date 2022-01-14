using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public interface IFrameBuffer
    {
        int FrameBufferObject { get; }

        void Allocate(RenderTargetInfo renderTargetInfo);

        void Release();

        void PreWrite();

        void PostRead();
    }
}