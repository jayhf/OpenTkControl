using System;
using System.Windows.Shell;
using OpenTK.Platform;

namespace OpenTkControl
{
    public interface IRenderProcedure : IDisposable
    {
        IDoubleBuffer Buffer { get; }

        void Begin();

        void End();

        /// <summary>
        /// not required
        /// </summary>
        void SwapBuffer();

        bool IsInitialized { get; }

        bool CanRender { get; }

        IRenderer Renderer { get; set; }

        GLSettings GlSettings { get; }

        void Initialize(IWindowInfo window);

        void SetSize(CanvasInfo size);

        void Begin();

        void End();

        DrawingDirective Render();
    }
}