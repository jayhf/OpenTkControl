using System;

namespace OpenTkWPFHost
{
    /// <summary>
    /// 帧缓冲，不同于OpenGL的framebuffer
    /// </summary>
    public interface IFrameBuffer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixelSize"></param>
        void Allocate(PixelSize pixelSize);

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        BufferInfo FlushCurrentFrame();

        void SwapBuffer();

        bool TryReadFrames(BufferArgs args, out FrameArgs frame);

        void Release();
    }
}