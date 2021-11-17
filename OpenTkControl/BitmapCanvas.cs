using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace OpenTkWPFHost
{
    public class BitmapCanvas : IRenderCanvas
    {
        private volatile BufferInfo _bufferInfo;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private WriteableBitmap _bitmap;

        public IntPtr DisplayBuffer { get; private set; }

        public bool Ready { get; } = true;

        private TransformGroup _transformGroup;

        private Rect _dirtRect;


        public void Allocate(CanvasInfo info)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            _bitmap = new WriteableBitmap((int)(info.ActualWidth * info.DpiScaleX),
                (int)(info.ActualHeight * info.DpiScaleY), 96 * info.DpiScaleX, 96 * info.DpiScaleY,
                PixelFormats.Pbgra32, null);
            _dirtRect = info.Rect;
            DisplayBuffer = _bitmap.BackBuffer;
        }

        public void Prepare()
        {
            ReadBufferInfo = null;
            this.IsDirty = false;
        }

        public void Flush()
        {
            if (ReadBufferInfo != null && ReadBufferInfo.HasBuffer)
            {
                try
                {
                    _bitmap.Lock();
                    _bitmap.AddDirtyRect(ReadBufferInfo.RepaintPixelRect);
                    this.IsDirty = true;
                }
                finally
                {
                    _bitmap.Unlock();
                }
            }
        }

        public void FlushFrame(DrawingContext context)
        {
            context.PushTransform(this._transformGroup);
            context.DrawImage(_bitmap, _dirtRect);
            context.Pop();
        }

        public bool CanAsyncFlush { get; } = true;

        public bool IsDirty { get; set; }

        public BufferInfo ReadBufferInfo
        {
            get => _bufferInfo;
            set => _bufferInfo = value;
        }
    }
}