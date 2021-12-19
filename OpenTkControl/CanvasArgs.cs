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
        /// <param name="args"></param>
        public abstract bool Commit(DrawingContext context);
    }

    public class BitmapCanvasArgs : CanvasArgs
    {
        private SingleBitmapCanvas _canvas;
        private readonly bool _needFlush;

        public BitmapCanvasArgs(SingleBitmapCanvas canvas, bool needFlush = false)
        {
            this._canvas = canvas;
            _needFlush = needFlush;
        }

        public override bool Commit(DrawingContext context)
        {
            return _canvas.Commit(context, _needFlush);
        }
    }


    public class DXCanvasArgs : CanvasArgs
    {
        private readonly DxCanvas dxCanvas;

        private CanvasInfo canvasInfo;
        
        private readonly IntPtr _frameBuffer;
        
        public DXCanvasArgs(IntPtr frameBuffer, DxCanvas dxCanvas, CanvasInfo canvasInfo)
        {
            this._frameBuffer = frameBuffer;
            this.dxCanvas = dxCanvas;
            this.canvasInfo = canvasInfo;
        }
        public override bool Commit(DrawingContext context)
        {
            return dxCanvas.Commit(context, this._frameBuffer);
        }
    }
}