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

        void Apply(RenderTargetInfo canvasInfo);

        void PreRender();

        RenderArgs PostRender();

        /// <summary>
        /// swap buffer
        /// </summary>
        void Swap();
        /// <summary>
        /// canvas factory
        /// </summary>
        /// <returns></returns>
        IRenderCanvas CreateCanvas();

        /// <summary>
        /// create or get render buffer
        /// </summary>
        /// <returns></returns>
        IRenderBuffer CreateRenderBuffer();

    }
}