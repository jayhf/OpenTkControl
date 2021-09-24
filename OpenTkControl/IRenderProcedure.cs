using System;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    public interface IRenderProcedure : IOpenGlRender, IDisposable
    {
        IRenderCanvas Canvas { get; }

        /// <summary>
        /// whether can render simultaneously with ui thread.
        /// </summary>
        bool CanAsyncRender { get; }

        void Begin();

        void End();

        void SwapBuffer();

        /// <summary>
        /// flush frame buffer
        /// </summary>
        /// <param name="context"></param>
        void FlushFrame(DrawingContext context);

        bool IsInitialized { get; }

        bool ReadyToRender { get; }

        IRenderer Renderer { get; set; }
    }
}