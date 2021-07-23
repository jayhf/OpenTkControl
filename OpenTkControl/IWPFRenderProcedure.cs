using System;
using System.Windows.Shell;

namespace OpenTkControl
{
    public interface IRenderProcedure : IOpenGlRender, IDisposable
    {
        void SizeCanvas(CanvasInfo size);

        void Begin();

        void End();

        void SwapBuffer();

        IRenderCanvas GetFrontBuffer();


        bool IsInitialized { get; }

        bool ReadyToRender { get; }

        IRenderer Renderer { get; set; }
    }
}