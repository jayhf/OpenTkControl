using OpenTK.Graphics;

namespace OpenTkWPFHost
{
    public interface IRenderer
    {
        void Initialize(IGraphicsContext context);

        void Render(GlRenderEventArgs args);

        void Resize(PixelSize size);
    }

    
}