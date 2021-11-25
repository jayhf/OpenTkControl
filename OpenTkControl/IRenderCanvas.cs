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
        /// indicate whether ready to render
        /// </summary>
        bool Ready { get; }

        void Allocate(CanvasInfo info);

        CanvasArgs Flush(FrameArgs frame);

        void Swap();

        /// <summary>
        /// commit to ui
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        bool Commit(DrawingContext context, CanvasArgs args);
    }
}