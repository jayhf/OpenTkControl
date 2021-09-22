using System;

namespace OpenTkWPFHost
{
    public interface IRenderProcedure : IOpenGlRender, IDisposable
    {
        void SizeCanvas(CanvasInfo size);

        void Begin();

        void End();

        void SwapBuffer();

        IRenderBuffer GetFrontBuffer();

        bool IsInitialized { get; }

        bool ReadyToRender { get; }

        IRenderer Renderer { get; set; }
    }
}