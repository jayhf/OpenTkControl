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

        private SingleBitmapCanvas[] bitmapCanvasArray;

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private int _currentWriteBufferIndex = 0;

        public BitmapCanvas():this(2)
        {
            
        }

        public BitmapCanvas(int bufferSize)
        {
            _bufferCount = bufferSize;
            bitmapCanvasArray = new SingleBitmapCanvas[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                bitmapCanvasArray[i] = new SingleBitmapCanvas();
            }
        }

        public bool Ready { get; } = true;

        private TransformGroup _transformGroup;

        public void Allocate(CanvasInfo info)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            foreach (var canvas in bitmapCanvasArray)
            {
                canvas.Allocate(info);
            }

            Swap();
        }

        
        private SingleBitmapCanvas _writeBufferInfo;

        public CanvasArgs Flush(FrameArgs frame)
        {
            return _writeBufferInfo.Flush(frame);
        }

        public void Swap()
        {
            _currentWriteBufferIndex++;
            var writeBufferIndex = _currentWriteBufferIndex % _bufferCount;
            _writeBufferInfo = bitmapCanvasArray[writeBufferIndex];
        }

        public ImageSource GetSource()
        {
            return _writeBufferInfo.Bitmap;
        }

        public bool Commit(DrawingContext context, CanvasArgs args)
        {
            return _writeBufferInfo.Commit(context, args, this._transformGroup);
        }

        public bool CanAsyncFlush { get; } = true;
    }
}