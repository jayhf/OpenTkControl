using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Abstraction
{
    public class PipelineArgs
    {
        /// <summary>
        /// used to indicate render background
        /// </summary>
        public RenderTargetInfo TargetInfo { get; }

        public PipelineArgs(RenderTargetInfo targetInfo)
        {
            TargetInfo = targetInfo;
        }
    }
}