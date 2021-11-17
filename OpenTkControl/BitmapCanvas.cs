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
        // private WriteableBitmap _bitmap;

        // public IntPtr DisplayBuffer { get; set; }

        public bool Ready { get; } = true;

        private ImageSource imageSource = new BitmapImage();

        private Dictionary<IntPtr, ImageSource> bitmapSources = new Dictionary<IntPtr, ImageSource>();

        private TransformGroup _transformGroup;

        private Rect _dirtRect;


        public void Allocate(CanvasInfo info)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            /*_bitmap = new WriteableBitmap((int) (info.ActualWidth * info.DpiScaleX),
                (int) (info.ActualHeight * info.DpiScaleY), 96 * info.DpiScaleX, 96 * info.DpiScaleY,
                PixelFormats.Pbgra32, null);*/
            _dirtRect = info.Rect;
            // _dirtRect = new Rect(new Size(_bitmap.Width, _bitmap.Height));
            // DisplayBuffer = _bitmap.BackBuffer;
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
                    if (!bitmapSources.TryGetValue(_bufferInfo.ClientIntPtr, out imageSource))
                    {
                        var bitmap = new Bitmap(_bufferInfo.PixelWidth, _bufferInfo.PixelHeight, _bufferInfo.Stride,
                            PixelFormat.Format32bppRgb, _bufferInfo.ClientIntPtr);
                        imageSource = Convert(bitmap);
                        bitmapSources.Add(_bufferInfo.ClientIntPtr, imageSource);
                    }


                    /*imageSource = Imaging.CreateBitmapSourceFromMemorySection(new IntPtr(_bufferInfo.ClientIntPtr.ToInt64()), bufferInfoPixelWidth,
                        _bufferInfo.PixelHeight, PixelFormats.Pbgra32, stride, 0);*/
                    /*_bitmap.Lock();
                    _bitmap.AddDirtyRect(ReadBufferInfo.RepaintPixelRect);*/
                    this.IsDirty = true;
                }
                finally
                {
                    // _bitmap.Unlock();
                }
            }
        }


        public static BitmapSource Convert(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        public void FlushFrame(DrawingContext context)
        {
            context.PushTransform(this._transformGroup);
            context.DrawImage(imageSource, _dirtRect);
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