using System;
using System.Collections.Concurrent;
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

        private RenderTargetInfo _canvasInfo;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        public WriteableBitmap Bitmap => _bitmap;

        private TransformGroup _transformGroup;

        public void Allocate(RenderTargetInfo canvasInfo)
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
            var bitmapFrameArgs = (BitmapFrameArgs) frame;
            var canvasInfo = bitmapFrameArgs.TargetInfo;
            try
            {
                _readerWriterLockSlim.EnterWriteLock();
                var pixelBufferInfo = bitmapFrameArgs.BufferInfo;
                if (!Equals(canvasInfo, this._canvasInfo))
                {
                    this._canvasInfo = canvasInfo;
                    return new BitmapCanvasArgs(this, frame.TargetInfo, pixelBufferInfo, true);
                }

                if (pixelBufferInfo.CopyTo(this._displayBuffer))
                {
                    return new BitmapCanvasArgs(this, frame.TargetInfo);
                }

                return null;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }


        public bool Commit(DrawingContext context, PixelBufferInfo bufferInfo, bool allocate)
        {
            bool bitmapLocked = false;
            try
            {
                _readerWriterLockSlim.EnterReadLock();
                if (allocate)
                {
                    Allocate(_canvasInfo);
                    if (!bufferInfo.CopyTo(this._displayBuffer))
                    {
                        return false;
                    }
                }

                _bitmap.Lock();
                bitmapLocked = true;
                _bitmap.AddDirtyRect(_int32Rect);
            }
            finally
            {
                if (bitmapLocked)
                {
                    _bitmap.Unlock();
                }

                _readerWriterLockSlim.ExitReadLock();
            }

            context.PushTransform(_transformGroup);
            context.DrawImage(_bitmap, _dirtRect);
            context.Pop();
            return true;
        }
    }
}