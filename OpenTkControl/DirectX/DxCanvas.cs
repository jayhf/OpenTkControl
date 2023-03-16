using System;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.DirectX
{
    public class DxCanvas 
    {
        private D3DImage _image;

        private double _dpiScaleX, _dpiScaleY;

        private FieldInfo _fieldInfo;

        public bool CanAsyncFlush { get; set; } = false;
        
        private bool _isFrontBufferAvailable = true;

        private TransformGroup _transformGroup;

        public bool Ready => _isFrontBufferAvailable && !this.D3DImageDirty;

        private RenderTargetInfo _renderTarget;

        public DxCanvas()
        {
        }

        public void Allocate(RenderTargetInfo info)
        {
            this._renderTarget = info;
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(new ScaleTransform(1, -1));
            _transformGroup.Children.Add(new TranslateTransform(0, info.ActualHeight));
            _transformGroup.Freeze();
            //only when dpi changed new d3dimage will be created
            if (info.DpiScaleX.Equals(_dpiScaleX)
                && info.DpiScaleY.Equals(_dpiScaleY))
            {
                return;
            }

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

        public CanvasArgs Flush(FrameArgs frame)
        {
            var dxFrameArgs = (DXFrameArgs) frame;
            return new DxCanvasArgs(dxFrameArgs.RenderTargetIntPtr, this, dxFrameArgs.TargetInfo);
        }

        public bool Commit(DrawingContext drawingContext, IntPtr frameBuffer, RenderTargetInfo canvasInfo)
        {
            try
            {
                if (!Equals(canvasInfo, _renderTarget))
                {
                    Allocate(canvasInfo);
                }

                var preDirtRect = new Int32Rect(0, 0, _renderTarget.ActualWidth,
                    _renderTarget.ActualHeight);
                _image.Lock();
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, frameBuffer);
                _image.AddDirtyRect(preDirtRect);
                _image.Unlock();
                drawingContext.PushTransform(_transformGroup);
                drawingContext.DrawImage(_image, new Rect(new Size(_image.Width, _image.Height)));
                drawingContext.Pop();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
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