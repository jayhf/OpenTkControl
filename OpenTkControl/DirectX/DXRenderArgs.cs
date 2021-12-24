using System;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.DirectX
{
    public class DXRenderArgs : RenderArgs
    {
        public IntPtr RenderTargetIntPtr { get; }

        public DXRenderArgs(RenderTargetInfo targetInfo, IntPtr renderTargetIntPtr) : base(targetInfo)
        {
            RenderTargetIntPtr = renderTargetIntPtr;
        }
    }
}