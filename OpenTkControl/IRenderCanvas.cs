using System.Windows.Media;

namespace OpenTkControl
{
    public interface IRenderCanvas
    {
        ImageSource GetFrontSource();

        void Create(CanvasInfo info);
    }
}