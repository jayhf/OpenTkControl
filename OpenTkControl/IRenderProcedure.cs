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
        IGraphicsContext Initialize(IWindowInfo window, GLSettings settings);

        void SizeFrame(CanvasInfo size);

        void PreRender();

        void PostRender();

        /// <summary>
        /// bind canvas to procedure, let canvas can read output from procedure
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns>indicate whether output is available</returns>
        bool BindCanvas(IRenderCanvas canvas);

        IRenderCanvas CreateCanvas();

        void SwapBuffer();
    }
}