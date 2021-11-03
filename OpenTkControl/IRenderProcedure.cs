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

        void Render(IRenderCanvas canvas, IRenderer renderer);

        IRenderCanvas CreateCanvas();

        void SwapBuffer();
    }
}