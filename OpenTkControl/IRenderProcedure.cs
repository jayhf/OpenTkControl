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
        GLSettings GlSettings { get; }

        void Initialize(IWindowInfo window);

        void SizeFrame(CanvasInfo size);

        void Render(IRenderCanvas canvas);

        IGraphicsContext Context { get; }

        IRenderCanvas CreateCanvas(CanvasInfo info);

        void SwapBuffer();

        // bool ReadyToRender { get; }

        bool IsInitialized { get; }

        IRenderer Renderer { get; set; }
    }
}