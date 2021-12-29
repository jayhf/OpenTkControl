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
            ErrorCode = GL.GetError();
        }

        public ErrorCode ErrorCode { get; }

        public RenderPhase Phase { get; }

        public Exception Exception { get; }
    }
}