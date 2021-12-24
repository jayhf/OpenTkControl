using System;
using System.Windows.Media;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.DirectX
{
    public class DxCanvasArgs : CanvasArgs
    {
        private readonly DxCanvas _dxCanvas;

        private readonly IntPtr _frameBuffer;

        public DxCanvasArgs(IntPtr frameBuffer, DxCanvas dxCanvas, RenderTargetInfo renderTargetInfo) : base(
            renderTargetInfo)
        {
            this._frameBuffer = frameBuffer;
            this._dxCanvas = dxCanvas;
        }

        public override bool Commit(DrawingContext context)
        {
            return _dxCanvas.Commit(context, this._frameBuffer, this.TargetInfo);
        }
    }
}