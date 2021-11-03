using OpenTK.Graphics;

namespace OpenTkWPFHost
{
    public interface IRenderer
    {
        bool IsInitialized { get; }
        
        void Initialize(IGraphicsContext context);

        /// <summary>
        /// can prevent render
        /// </summary>
        /// <returns></returns>
        bool PreviewRender();
        
        void Render(GlRenderEventArgs args);

        void Resize(PixelSize size);

        /// <summary>
        /// renderer can pass render procedure
        /// </summary>
        /// <returns></returns>
        // bool CheckPassRender();
    }
}