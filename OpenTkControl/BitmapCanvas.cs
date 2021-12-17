using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace OpenTkWPFHost
{
    public class BitmapCanvas : IRenderCanvas
    {
        private readonly int _bufferCount;

        private readonly SingleBitmapCanvas[] _bitmapCanvasCollection;

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private int _currentWriteCanvasIndex = 0;

        public BitmapCanvas() : this(2)
        {
        }

        public BitmapCanvas(int bufferSize)
        {
            _bufferCount = bufferSize;
            _bitmapCanvasCollection = new SingleBitmapCanvas[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                _bitmapCanvasCollection[i] = new SingleBitmapCanvas();
            }
        }

        public bool Ready { get; } = true;

        public void Allocate(CanvasInfo info)
        {
            foreach (var canvas in _bitmapCanvasCollection)
            {
                canvas.Allocate(info);
            }

            Swap();
        }


        private SingleBitmapCanvas _writeCanvas;

        public CanvasArgs Flush(FrameArgs frame)
        {
            return _writeCanvas.Flush(frame);
        }

        public void Swap()
        {
            _currentWriteCanvasIndex++;
            var writeBufferIndex = _currentWriteCanvasIndex % _bufferCount;
            _writeCanvas = _bitmapCanvasCollection[writeBufferIndex];
        }

        public bool Commit(DrawingContext context, CanvasArgs args)
        {
            return _writeCanvas.Commit(context, args);
        }

        public bool CanAsyncFlush { get; } = true;
    }
}