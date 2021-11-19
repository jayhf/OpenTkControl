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
        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private WriteableBitmap _bitmap;

        public IntPtr DisplayBuffer { get; private set; }

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
            DisplayBuffer = _bitmap.BackBuffer;
        }

        public void Prepare()
        {
            this.IsDirty = false;
        }

        public void Flush(FrameArgs frame)
        {
            if (frame != null && _int32Rect.Equals(frame.RepaintPixelRect))
            {
                try
                {
                    _bitmap.Lock();
                    _bitmap.AddDirtyRect(_int32Rect);
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

        public CanvasInfo Info { get; private set; }

        public bool IsDirty { get; set; }
    }
}