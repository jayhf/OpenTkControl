using OpenTK.Graphics;

namespace OpenTkWPFHost
{
    public interface IRenderer
    {
        bool IsInitialized { get; }
        
        void Initialize(IGraphicsContext context);

        void Render(GlRenderEventArgs args);

        void Resize(PixelSize size);

        /// <summary>
        /// renderer can pass render procedure
        /// </summary>
        /// <returns></returns>
        // bool CheckPassRender();
    }
}