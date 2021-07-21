using System;
using System.Windows.Shell;
using OpenTK.Platform;

namespace OpenTkControl
{
    public interface IRenderProcedure : IDisposable
    {
        IRenderCanvas Canvas { get; }

        bool IsInitialized { get; }

        bool CanRender { get; }

        IRenderer Renderer { get; set; }

        GLSettings GlSettings { get; }

        void Initialize(IWindowInfo window);

        void SizeCanvas(CanvasInfo size);

        DrawingDirective Render();
    }
}