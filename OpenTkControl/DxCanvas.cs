using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    public class DxCanvas : IRenderCanvas
    {
        private D3DImage _image;

        private double _dpiScaleX, _dpiScaleY;

        private FieldInfo _fieldInfo;

        private readonly DXProcedure _dxProcedure;

        public bool CanAsyncRender { get; set; } = false;

        public bool IsDirty { get; set; }

        private bool _isFrontBufferAvailable;

        public bool Ready => _isFrontBufferAvailable && !this.D3DImageDirty;

        public DxCanvas(DXProcedure dxProcedure)
        {
            this._dxProcedure = dxProcedure;
        }

        public void Allocate(CanvasInfo info)
        {
            if (info.DpiScaleX.Equals(_dpiScaleX)
                && info.DpiScaleY.Equals(_dpiScaleY))
            {
                return;
            }

            this._dpiScaleX = info.DpiScaleX;
            this._dpiScaleY = info.DpiScaleY;
            if (_image != null)
            {
                _image.IsFrontBufferAvailableChanged -= _image_IsFrontBufferAvailableChanged;
            }

            _image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
            this._isFrontBufferAvailable = _image.IsFrontBufferAvailable;
            _image.IsFrontBufferAvailableChanged += _image_IsFrontBufferAvailableChanged;
            var bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            | BindingFlags.Static;
            _fieldInfo = typeof(D3DImage).GetField("_isDirty", bindFlags);
        }

        private void _image_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._isFrontBufferAvailable = (bool) e.NewValue;
        }

        public void Begin()
        {
            IsDirty = true;
            _image.Lock();
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                _dxProcedure.DxRenderTargetHandle);
        }

        public void End()
        {
            var frameBuffer = _dxProcedure.FrameBuffer;
            var preDirtRect = new Int32Rect(0, 0, frameBuffer.Width,
                frameBuffer.Height);
            if (!preDirtRect.IsEmpty)
            {
                IsDirty = true;
                _image.AddDirtyRect(preDirtRect);
            }

            _image.Unlock();
        }

        public void FlushFrame(DrawingContext drawingContext)
        {
            var transformGroup = this._dxProcedure.FrameBuffer.TransformGroup;
            drawingContext.PushTransform(transformGroup);
            drawingContext.DrawImage(_image, new Rect(new Size(_image.Width, _image.Height)));
            drawingContext.Pop();
        }

        public bool D3DImageDirty
        {
            get { return (bool) _fieldInfo.GetValue(this._image); }
        }
    }
}