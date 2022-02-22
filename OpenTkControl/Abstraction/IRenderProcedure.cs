using System;
using OpenTK.Platform;
using OpenTkWPFHost.Configuration;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Abstraction
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