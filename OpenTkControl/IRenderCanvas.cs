using System;
using System.Windows.Media;

namespace OpenTkControl
{
    /// <summary>
    /// render canvas, better to operate in ui thread.
    /// </summary>
    public interface IRenderCanvas
    {
        void Create(CanvasInfo info);
    }   
}