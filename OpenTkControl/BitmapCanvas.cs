using System;
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
        private volatile WriteableBitmap _bitmap;

        public bool Ready { get; } = true;

        private TransformGroup _transformGroup;

        public void Allocate(CanvasInfo info)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            _bitmap = new WriteableBitmap((int) (info.ActualWidth * info.DpiScaleX),
                (int) (info.ActualHeight * info.DpiScaleY), 96 * info.DpiScaleX, 96 * info.DpiScaleY,
                PixelFormats.Pbgra32, null);
        }

        public void Begin()
        {
            ReadBufferInfo = null;
            this.IsDirty = false;
        }

        public void End()
        {
            if (ReadBufferInfo != null && ReadBufferInfo.HasBuffer)
            {
                var dirtyArea = ReadBufferInfo.RepaintRect;
                if (!dirtyArea.IsEmpty)
                {
                    this.IsDirty = true;
                    _bitmap.Lock();
                    _bitmap.WritePixels(dirtyArea, ReadBufferInfo.FrameBuffer, ReadBufferInfo.BufferSize,
                        _bitmap.BackBufferStride);
                    _bitmap.AddDirtyRect(dirtyArea);
                    _bitmap.Unlock();
                }

                ReadBufferInfo.HasBuffer = false;
            }
        }

        public void FlushFrame(DrawingContext context)
        {
            context.PushTransform(this._transformGroup);
            context.DrawImage(_bitmap, new Rect(new Size(_bitmap.Width, _bitmap.Height)));
            context.Pop();
        }

        public bool CanAsyncRender { get; } = true;

        public bool IsDirty { get; set; }

        public BufferInfo ReadBufferInfo
        {
            get => _bufferInfo;
            set => _bufferInfo = value;
        }
    }
}