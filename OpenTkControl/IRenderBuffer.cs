using System.Windows.Media;

namespace OpenTkWPFHost
{
    public interface IRenderBuffer
    {
        /// <summary>
        /// need to be access in background thread?
        /// </summary>
        bool IsAvailable { get; }

        
    }
}