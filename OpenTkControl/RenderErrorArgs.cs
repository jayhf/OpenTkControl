using System;

namespace OpenTkWPFHost
{
    public class RenderErrorArgs : EventArgs
    {
        public RenderErrorArgs(RenderPhase phase, Exception exception)
        {
            Phase = phase;
            Exception = exception;
        }

        public RenderPhase Phase { get; set; }

        public Exception Exception { get; set; }
    }
}