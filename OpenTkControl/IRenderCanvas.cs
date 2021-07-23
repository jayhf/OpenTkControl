using System.Windows.Media;

namespace OpenTkControl
{
    public interface IRenderCanvas
    {
        ImageSource Canvas { get; }

        void Create(CanvasInfo info);
    }
}