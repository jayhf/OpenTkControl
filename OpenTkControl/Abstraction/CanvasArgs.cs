using System.Windows.Media;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Abstraction
{
    public abstract class CanvasArgs : PipelineArgs
    {
        /// <summary>
        /// commit to ui
        /// </summary>
        /// <param name="context"></param>
        public abstract bool Commit(DrawingContext context);

        protected CanvasArgs(RenderTargetInfo renderTargetInfo) : base(renderTargetInfo)
        {
        }
    }
}