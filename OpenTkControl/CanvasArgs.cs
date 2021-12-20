using System;
using System.Windows;
using System.Windows.Media;

namespace OpenTkWPFHost
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

    public class BitmapCanvasArgs : CanvasArgs
    {
        private readonly SingleBitmapCanvas _canvas;
        private readonly bool _needFlush;

        private PixelBufferInfo _bufferInfo;

        public BitmapCanvasArgs(SingleBitmapCanvas canvas, RenderTargetInfo renderTargetInfo,
            PixelBufferInfo bufferInfo = null, bool needFlush = false)
            : base(renderTargetInfo)
        {
            this._canvas = canvas;
            this._bufferInfo = bufferInfo;
            this._needFlush = needFlush;
        }

        public override bool Commit(DrawingContext context)
        {
            return _canvas.Commit(context,_bufferInfo, _needFlush);
        }
    }


    public class DXCanvasArgs : CanvasArgs
    {
        private readonly DxCanvas _dxCanvas;

        private readonly IntPtr _frameBuffer;

        public DXCanvasArgs(IntPtr frameBuffer, DxCanvas dxCanvas, RenderTargetInfo renderTargetInfo) : base(
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