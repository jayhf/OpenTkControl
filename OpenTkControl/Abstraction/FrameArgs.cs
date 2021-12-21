using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Abstraction
{
    public abstract class FrameArgs : PipelineArgs
    {
        protected FrameArgs(RenderTargetInfo renderTargetInfo) : base(renderTargetInfo)
        {
        }
    }
}