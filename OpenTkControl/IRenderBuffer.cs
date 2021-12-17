using System;

namespace OpenTkWPFHost
{
    /// <summary>
    /// render buffer, abstract conception
    /// responsible for 
    /// </summary>
    public interface IRenderBuffer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canvasInfo"></param>
        void Allocate(CanvasInfo canvasInfo);

        void Swap();
        
        FrameArgs ReadFrames(RenderArgs args);

        void Release();
    }
}