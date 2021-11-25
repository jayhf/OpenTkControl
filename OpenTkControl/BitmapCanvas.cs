using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace OpenTkWPFHost
{
    public class BitmapCanvas : IRenderCanvas
    {
        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private WriteableBitmap _bitmap;

        private IntPtr _displayBuffer;

        public bool Ready { get; } = true;

        private TransformGroup _transformGroup;

        private Rect _dirtRect;

        private Int32Rect _int32Rect;

        public void Allocate(CanvasInfo info)
        {
            this.Info = info;
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            _bitmap = new WriteableBitmap(info.PixelWidth, info.PixelHeight, info.DpiX, info.DpiY,
                PixelFormats.Pbgra32, null);
            _dirtRect = info.Rect;
            _int32Rect = info.Int32Rect;
            _displayBuffer = _bitmap.BackBuffer;
        }


        private ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();
        
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
                readerWriterLockSlim.EnterWriteLock();
                unsafe
                {
                    Buffer.MemoryCopy(bufferInfo.MapBufferIntPtr.ToPointer(),
                        _displayBuffer.ToPointer(),
                        bufferSize, bufferSize);
                }
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
            
            bufferInfo.HasBuffer = false;
            return new BitmapCanvasArgs()
            {
                PixelSize = frame.PixelSize,
                Int32Rect = _int32Rect
            };
        }

        public bool Commit(DrawingContext context, CanvasArgs args)
        {
            var canvasArgs = (BitmapCanvasArgs) args;
            if (canvasArgs != null && _int32Rect.Equals(canvasArgs.Int32Rect))
            {
                try
                {
                    readerWriterLockSlim.EnterReadLock();
                    _bitmap.Lock();
                    _bitmap.AddDirtyRect(_int32Rect);
                }
                finally
                {
                    _bitmap.Unlock();
                    readerWriterLockSlim.ExitReadLock();
                }

                context.PushTransform(this._transformGroup);
                context.DrawImage(_bitmap, _dirtRect);
                context.Pop();
                return true;
            }

            return false;
        }

        public bool CanAsyncFlush { get; } = true;

        public CanvasInfo Info { get; private set; }
    }
}