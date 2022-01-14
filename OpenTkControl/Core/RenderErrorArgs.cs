using System;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost.Configuration;

namespace OpenTkWPFHost.Core
{
    public class RenderErrorArgs : EventArgs
    {
        public RenderErrorArgs(RenderPhase phase, Exception exception)
        {
            Phase = phase;
            Exception = exception;
        }

        public RenderPhase Phase { get; }

        public Exception Exception { get; }
    }
}