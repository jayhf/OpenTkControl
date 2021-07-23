using OpenTK.Platform;

namespace OpenTkControl
{
    public interface IOpenGlRender
    {
        GLSettings GlSettings { get; }

        void Initialize(IWindowInfo window);

        void SizeFrame(CanvasInfo size);

        DrawingDirective Render();
    }
}