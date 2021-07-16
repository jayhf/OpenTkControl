using System;
using System.Windows.Media;
using System.Windows.Shell;
using OpenTK.Platform;

namespace OpenTkControl
{
    public interface IRenderProcedure : IDisposable
    {
        IRenderer Renderer { get; set; }

        void Initialize(IWindowInfo window);

        void SizeCanvas(CanvasInfo size);

        ImageSource Render(out DrawingDirective drawingDirective);
    }
}