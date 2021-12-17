using System;
using System.Windows.Media;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost
{

    /// <summary>
    /// 渲染过程
    /// </summary>
    public interface IRenderProcedure : IDisposable
    {
        GLContextBinding Initialize(IWindowInfo window, GLSettings settings);

        /// <summary>
        /// set frame size
        /// </summary>
        /// <param name="pixelSize"></param>
        void SizeFrame(PixelSize pixelSize);

        void PreRender();

        RenderArgs PostRender();

        /// <summary>
        /// canvas factory
        /// </summary>
        /// <returns></returns>
        IRenderCanvas CreateCanvas();

        IRenderBuffer CreateFrameBuffer();
    }
}