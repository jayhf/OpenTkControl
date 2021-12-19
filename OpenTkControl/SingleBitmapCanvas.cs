using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;

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

        private CanvasInfo _canvasInfo;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        public WriteableBitmap Bitmap => _bitmap;

        private TransformGroup _transformGroup;

        public void Allocate(CanvasInfo canvasInfo)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, canvasInfo.ActualHeight));
            _transformGroup.Freeze();
            _bitmap = new WriteableBitmap(canvasInfo.PixelWidth, canvasInfo.PixelHeight, canvasInfo.DpiX,
                canvasInfo.DpiY,
                PixelFormats.Pbgra32, null);
            _dirtRect = canvasInfo.Rect;
            _int32Rect = canvasInfo.Int32Rect;
            _displayBuffer = _bitmap.BackBuffer;
        }

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public CanvasArgs Flush([NotNull] FrameArgs frame)
        {
            var bitmapFrameArgs = (BitmapFrameArgs)frame;
            var canvasInfo = bitmapFrameArgs.CanvasInfo;
            try
            {
                _readerWriterLockSlim.EnterWriteLock();
                if (!canvasInfo.Equals(this._canvasInfo))
                {
                    this._canvasInfo = canvasInfo;
                    return new BitmapCanvasArgs(this, true) { PixelSize = frame.PixelSize };
                }

                var bufferInfo = bitmapFrameArgs.BufferInfo;
                Flush(bufferInfo);
                return new BitmapCanvasArgs(this)
                {
                    PixelSize = frame.PixelSize,
                };
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        private void Flush(PixelBufferInfo bufferInfo)
        {
            var bufferSize = bufferInfo.BufferSize;
            unsafe
            {
                Buffer.MemoryCopy(bufferInfo.MapBufferIntPtr.ToPointer(),
                    _displayBuffer.ToPointer(),
                    bufferSize, bufferSize);
            }


            bufferInfo.HasBuffer = false;
        }

        public bool Commit(DrawingContext context, bool allocate)
        {
            try
            {
                _readerWriterLockSlim.EnterReadLock();
                if (allocate)
                {
                    Allocate(_canvasInfo);
                }

                _bitmap.Lock();
                _bitmap.AddDirtyRect(_int32Rect);
            }
            finally
            {
                _bitmap.Unlock();
                _readerWriterLockSlim.ExitReadLock();
            }

            context.PushTransform(_transformGroup);
            context.DrawImage(_bitmap, _dirtRect);
            context.Pop();
            return true;
        }
    }
}