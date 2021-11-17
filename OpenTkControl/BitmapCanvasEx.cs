using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost
{
    public class BitmapCanvasEx : IRenderCanvas
    {
        private volatile BufferInfo _bufferInfo;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private WriteableBitmap _readBitmap;

        private WriteableBitmap _writeBitmap;

        private bool _readDirt, _writeDirt;

        public IntPtr DisplayBuffer { get; set; }

        public bool Ready { get; } = true;

        private TransformGroup _transformGroup;

        private Rect dirtRect;

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
            dirtRect = new Rect(new Size(_readBitmap.Width, _readBitmap.Height));
        }


        public void Swap()
        {
            (_readBitmap, _writeBitmap) = (_writeBitmap, _readBitmap);
            (_readDirt, _writeDirt) = (_writeDirt, _readDirt);
        }

        public void Prepare()
        {
            this.DisplayBuffer = _writeBitmap.BackBuffer;
            ReadBufferInfo = null;
        }
        
        public void Flush()
        {
            if (ReadBufferInfo != null && ReadBufferInfo.HasBuffer)
            {
                try
                {
                    _writeBitmap.Lock();
                    _writeBitmap.AddDirtyRect(ReadBufferInfo.RepaintPixelRect);
                    _writeDirt = true;
                }
                finally
                {
                    _writeBitmap.Unlock();
                }
            }
        }

        public void FlushFrame(DrawingContext context)
        {
            context.PushTransform(this._transformGroup);
            context.DrawImage(_readBitmap, dirtRect);
            context.Pop();
            _readDirt = false;
        }

        public bool CanAsyncFlush { get; } = true;

        public bool IsDirty => _readDirt;

        public BufferInfo ReadBufferInfo
        {
            get => _bufferInfo;
            set => _bufferInfo = value;
        }
    }
}