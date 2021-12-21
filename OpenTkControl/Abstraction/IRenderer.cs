using OpenTK.Graphics;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Abstraction
{
    public interface IRenderer
    {
        bool IsInitialized { get; }

        /// <summary>
        /// will be called automatically on render thread.
        /// </summary>
        /// <param name="context"></param>
        void Initialize(IGraphicsContext context);

        /// <summary>
        /// can prevent render
        /// </summary>
        /// <returns></returns>
        bool PreviewRender();

        void Render(GlRenderEventArgs args);

        void Resize(PixelSize size);

        /// <summary>
        /// differ from disposable, will be called automatically on render thread.
        /// </summary>
        void Uninitialize();
    }
}