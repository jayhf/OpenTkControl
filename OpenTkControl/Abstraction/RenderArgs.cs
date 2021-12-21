using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Abstraction
{
    public abstract class RenderArgs : PipelineArgs
    {
        protected RenderArgs(RenderTargetInfo targetInfo) : base(targetInfo)
        {

        }
    }
}