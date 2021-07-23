using System;
using System.Windows.Media;

namespace OpenTkControl
{
    public interface IRenderCanvas
    {
        Guid Id { get; }

        ImageSource GetSource();

        void Create(CanvasInfo info);

        bool CanRender { get; }
    }   
}