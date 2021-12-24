using System;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.DirectX
{
    public class DXFrameArgs:FrameArgs
    {
        public IntPtr RenderTargetIntPtr { get; }

        public DXFrameArgs(IntPtr renderTargetIntPtr, RenderTargetInfo renderTargetInfo) : base(renderTargetInfo)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
        }
    }
}