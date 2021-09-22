using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public interface IOpenGlRender
    {
        GLSettings GlSettings { get; }

        void Initialize(IWindowInfo window);

        void SizeFrame(CanvasInfo size);

        bool Render();
    }
}