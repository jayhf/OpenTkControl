using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost
{
    public class DxCanvas : IRenderCanvas
    {
        private D3DImage _image;

        private double _dpiScaleX, _dpiScaleY;

        private FieldInfo _fieldInfo;

        public bool CanAsyncFlush { get; set; } = false;
        public CanvasInfo Info { get; }

        public IntPtr FrameBuffer { get; set; }

        public bool IsDirty { get; set; }

        private bool _isFrontBufferAvailable;

        private TransformGroup _transformGroup;

        public bool Ready => _isFrontBufferAvailable && !this.D3DImageDirty;

        private CanvasInfo _canvasInfo;

        public DxCanvas()
        {
        }

        public void Allocate(CanvasInfo info)
        {
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            if (info.DpiScaleX.Equals(_dpiScaleX)
                && info.DpiScaleY.Equals(_dpiScaleY))
            {
                return;
            }

            _canvasInfo = info;
            this._dpiScaleX = info.DpiScaleX;
            this._dpiScaleY = info.DpiScaleY;
            if (_image != null)
            {
                _image.IsFrontBufferAvailableChanged -= _image_IsFrontBufferAvailableChanged;
            }

            _image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
            _image.IsFrontBufferAvailableChanged += _image_IsFrontBufferAvailableChanged;
            this._isFrontBufferAvailable = _image.IsFrontBufferAvailable;
            var bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            | BindingFlags.Static;
            _fieldInfo = typeof(D3DImage).GetField("_isDirty", bindFlags);
        }

        private void _image_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._isFrontBufferAvailable = (bool) e.NewValue;
        }

        public void Prepare()
        {
            IsDirty = false;
        }

        public void Flush(FrameArgs frame)
        {
            var preDirtRect = new Int32Rect(0, 0, _canvasInfo.ActualWidth,
                _canvasInfo.ActualHeight);
            if (this.FrameBuffer != IntPtr.Zero && !preDirtRect.IsEmpty)
            {
                _image.Lock();
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, this.FrameBuffer);
                _image.AddDirtyRect(preDirtRect);
                _image.Unlock();
                IsDirty = true;
            }
        }

        public void FlushFrame(DrawingContext drawingContext)
        {
            drawingContext.PushTransform(_transformGroup);
            drawingContext.DrawImage(_image, new Rect(new Size(_image.Width, _image.Height)));
            drawingContext.Pop();
        }

        public void Swap()
        {
        }

        public bool D3DImageDirty
        {
            get { return (bool) _fieldInfo.GetValue(this._image); }
        }
    }
}