using System.Windows.Media;

namespace OpenTkControl
{
    public interface IImageBuffer
    {
        ImageSource ImageSource { get; }

        /// <summary>
        /// need to be access in background thread?
        /// </summary>
        bool IsAvailable { get; }
    }
}