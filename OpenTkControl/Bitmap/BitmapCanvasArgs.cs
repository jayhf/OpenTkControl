using System.Windows.Media;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public class BitmapCanvasArgs : CanvasArgs
    {
        private readonly SingleBitmapCanvas _canvas;

        private readonly bool _needFlush;

        private readonly PixelBufferInfo _bufferInfo;

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
            return _canvas.Commit(context,_bufferInfo, TargetInfo, _needFlush);
        }
    }
}