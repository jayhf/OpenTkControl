using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
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

        private RenderTargetInfo _targetInfo;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        public WriteableBitmap Bitmap => _bitmap;

        private TransformGroup _transformGroup;

        public void Allocate(RenderTargetInfo targetInfo)
        {
            this._targetInfo = targetInfo;
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, targetInfo.ActualHeight));
            _transformGroup.Freeze();
            _bitmap = new WriteableBitmap(targetInfo.PixelWidth, targetInfo.PixelHeight, targetInfo.DpiX,
                targetInfo.DpiY,
                PixelFormats.Pbgra32, null);
            _dirtRect = targetInfo.Rect;
            _int32Rect = targetInfo.Int32Rect;
            _displayBuffer = _bitmap.BackBuffer;
        }

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public BitmapCanvasArgs Flush([NotNull] FrameArgs frame)
        {
            var bitmapFrameArgs = (BitmapFrameArgs) frame;
            var canvasInfo = bitmapFrameArgs.TargetInfo;
            try
            {
                _readerWriterLockSlim.EnterWriteLock();
                var pixelBufferInfo = bitmapFrameArgs.BufferInfo;
                if (!Equals(canvasInfo, this._targetInfo))
                {
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


        public bool Commit(DrawingContext context, PixelBufferInfo bufferInfo, RenderTargetInfo targetInfo,
            bool allocate)
        {
            bool bitmapLocked = false;
            try
            {
                _readerWriterLockSlim.EnterWriteLock();
                if (allocate)
                {
                    Allocate(targetInfo);
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

                _readerWriterLockSlim.ExitWriteLock();
            }

            context.PushTransform(_transformGroup);
            context.DrawImage(_bitmap, _dirtRect);
            context.Pop();
            return true;
        }
    }
}