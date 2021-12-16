using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost
{
    public class SingleBitmapCanvas
    {
        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private WriteableBitmap _bitmap;

        private IntPtr _displayBuffer;

        private Rect _dirtRect;

        private Int32Rect _int32Rect;

        public void Allocate(CanvasInfo info)
        {
            _bitmap = new WriteableBitmap(info.PixelWidth, info.PixelHeight, info.DpiX, info.DpiY,
                PixelFormats.Pbgra32, null);
            _dirtRect = info.Rect;
            _int32Rect = info.Int32Rect;
            _displayBuffer = _bitmap.BackBuffer;
        }

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public CanvasArgs Flush(FrameArgs frame)
        {
            if (frame == null)
            {
                return null;
            }

            var bitmapFrameArgs = (BitmapFrameArgs) frame;
            var bufferInfo = bitmapFrameArgs.BufferInfo;
            var bufferSize = bufferInfo.BufferSize;
            try
            {
                _readerWriterLockSlim.EnterWriteLock();
                unsafe
                {
                    Buffer.MemoryCopy(bufferInfo.MapBufferIntPtr.ToPointer(),
                        _displayBuffer.ToPointer(),
                        bufferSize, bufferSize);
                }
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }

            bufferInfo.HasBuffer = false;
            return new BitmapCanvasArgs()
            {
                PixelSize = frame.PixelSize,
                Int32Rect = _int32Rect
            };
        }

        public bool Commit(DrawingContext context, CanvasArgs args, TransformGroup transform)
        {
            var canvasArgs = (BitmapCanvasArgs) args;
            if (canvasArgs != null && _int32Rect.Equals(canvasArgs.Int32Rect))
            {
                try
                {
                    _readerWriterLockSlim.EnterReadLock();
                    _bitmap.Lock();
                    _bitmap.AddDirtyRect(_int32Rect);
                }
                finally
                {
                    _bitmap.Unlock();
                    _readerWriterLockSlim.ExitReadLock();
                }

                context.PushTransform(transform);
                context.DrawImage(_bitmap, _dirtRect);
                context.Pop();
                return true;
            }

            return false;
        }
    }
}