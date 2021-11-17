using System.Windows.Media;

namespace OpenTkWPFHost
{
    /// <summary>
    /// render canvas, must operate in the same thread.
    /// </summary>
    public interface IRenderCanvas
    {
        /// <summary>
        /// whether can render simultaneously with ui thread.
        /// </summary>
        bool CanAsyncFlush { get; }

        /// <summary>
        /// if need to flush frame
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// indicate whether ready to render
        /// </summary>
        bool Ready { get; }

        void Allocate(CanvasInfo info);

        void Prepare();

        void Flush();

        /// <summary>
        /// flush frame buffer
        /// </summary>
        /// <param name="context"></param>
        void FlushFrame(DrawingContext context);
        
    }
}