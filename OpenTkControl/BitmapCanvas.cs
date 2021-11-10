using System;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost
{
    public class BitmapCanvas : IRenderCanvas
    {
        private volatile BufferInfo _bufferInfo;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private WriteableBitmap _readBitmap;

        private WriteableBitmap _writeBitmap;

        public IntPtr DisplayBuffer { get; set; }

        public bool Ready { get; } = true;

        private TransformGroup _transformGroup;

        public void Allocate(CanvasInfo info)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            _readBitmap = new WriteableBitmap((int) (info.ActualWidth * info.DpiScaleX),
                (int) (info.ActualHeight * info.DpiScaleY), 96 * info.DpiScaleX, 96 * info.DpiScaleY,
                PixelFormats.Pbgra32, null);
            _writeBitmap = new WriteableBitmap((int) (info.ActualWidth * info.DpiScaleX),
                (int) (info.ActualHeight * info.DpiScaleY), 96 * info.DpiScaleX, 96 * info.DpiScaleY,
                PixelFormats.Pbgra32, null);
        }


        public void Swap()
        {
            (_readBitmap, _writeBitmap) = (_writeBitmap, _readBitmap);
        }

        public void Begin()
        {
            this.DisplayBuffer = _writeBitmap.BackBuffer;
            ReadBufferInfo = null;
            this.IsDirty = false;
            _writeBitmap.Lock();
        }

        public void End()
        {
            try
            {
                if (ReadBufferInfo != null && ReadBufferInfo.HasBuffer)
                {
                    this.IsDirty = true;
                    _writeBitmap.AddDirtyRect(ReadBufferInfo.RepaintRect);
                }
            }
            finally
            {
                _writeBitmap.Unlock();
            }
        }

        public void FlushFrame(DrawingContext context)
        {
            context.PushTransform(this._transformGroup);
            context.DrawImage(_readBitmap, new Rect(new Size(_readBitmap.Width, _readBitmap.Height)));
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