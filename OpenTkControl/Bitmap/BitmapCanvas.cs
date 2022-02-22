using System;
using OpenTkWPFHost.Abstraction;

namespace OpenTkWPFHost.Bitmap
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
            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _bufferCount = bufferSize;
            _bitmapCanvasCollection = new SingleBitmapCanvas[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                _bitmapCanvasCollection[i] = new SingleBitmapCanvas();
            }

            _writeCanvas = _bitmapCanvasCollection[0];
        }

        public bool Ready { get; } = true;


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

        public bool CanAsyncFlush { get; } = true;
    }
}