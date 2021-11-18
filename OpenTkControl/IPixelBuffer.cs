using System;

namespace OpenTkWPFHost
{
    public interface IPixelBuffer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        void Allocate(int width, int height);

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        void FlushCurrentFrame();

        void SwapBuffer();
        bool TryReadFromBufferInfo(IntPtr ptr, out BufferInfo bufferInfo);

        void Release();
    }
}