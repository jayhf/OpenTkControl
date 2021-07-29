using System.Windows.Media;

namespace OpenTkWPFHost
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