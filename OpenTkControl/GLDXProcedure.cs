using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using OpenTK.Platform.Windows;

namespace OpenTkControl
{
    public class DoubleDxCanvas : IDoubleBuffer
    {
        private readonly DxCanvas[] _dxCanvasArray = {new DxCanvas(), new DxCanvas()};

        private DxCanvas _backBuffer, _frontBuffer;

        public DoubleDxCanvas()
        {
            _backBuffer = _dxCanvasArray[0];
            _frontBuffer = _dxCanvasArray[1];
        }

        public DxCanvas GetWriteBuffer()
        {
            return _backBuffer;
        }

        public IRenderCanvas GetFrontBuffer()
        {
            return _frontBuffer;
        }

        public IRenderCanvas GetBackBuffer()
        {
            return _backBuffer;
        }

        public void SwapBuffer()
        {
            var mid = _backBuffer;
            _backBuffer = _frontBuffer;
            _frontBuffer = mid;
        }

        public void Create(CanvasInfo info)
        {
            foreach (var dxCanvas in _dxCanvasArray)
            {
                dxCanvas.Create(info);
            }
        }
    }

    public class DxCanvas : IRenderCanvas
    {
        private D3DImage _image;

        public D3DImage Image => _image;

        public Guid Id { get; } = Guid.NewGuid();

        public ImageSource GetSource()
        {
            return _image;
        }

        public void Create(CanvasInfo info)
        {
            _image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
        }

        public bool CanRender => _image != null && _image.Width > 0 && _image.Height > 0;
    }

    ///Renderer that uses DX_Interop for a fast-path.
    public class GLDXProcedure : IRenderProcedure
    {
        private DxGlContext _context;
        private DxGLFramebuffer _frontBuffer, backBuffer;
        private DxGLFramebuffer[] _framebuffers = new DxGLFramebuffer[2];
        private IRenderer _renderer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _frontBuffer?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => _frontBuffer?.DxRenderTargetHandle ?? IntPtr.Zero;

        /// The OpenGL Framebuffer width
        public int Width => _frontBuffer?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => _frontBuffer?.FramebufferHeight ?? 0;

        private volatile bool _rendererInitialized = false;

        private readonly DoubleDxCanvas _doubleDxCanvas = new DoubleDxCanvas();

        public void SwapBuffer()
        {
            this._doubleDxCanvas.SwapBuffer();
            var dxGlFramebuffer = _frontBuffer;
            backBuffer = _frontBuffer;
            _frontBuffer = dxGlFramebuffer;
        }

        public bool IsInitialized { get; private set; }

        public bool CanRender
        {
            get { return _framebuffers != null; }
        }

        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                _rendererInitialized = false;
            }
        }

        public GLSettings GlSettings { get; }

        public GLDXProcedure(GLSettings glSettings)
        {
            this.GlSettings = glSettings;
        }

        public IDoubleBuffer Buffer => _doubleDxCanvas;

        public void Begin()
        {
            var image = _doubleDxCanvas.GetWriteBuffer().Image;
            image.Lock();
            image.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                this.DxRenderTargetHandle);
        }

        public void End()
        {
            var image = _doubleDxCanvas.GetWriteBuffer().Image;
            image.AddDirtyRect(new Int32Rect(0, 0, this.Width,
                this.Height));
            image.Unlock();
        }

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender()
        {
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {backBuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, backBuffer.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {backBuffer.DxInteropRegisteredHandle});
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>indicate that if renderer initialize successfully or already initialized</returns>
        private bool CheckRenderer()
        {
            if (_rendererInitialized)
            {
                return true;
            }

            if (Renderer != null)
            {
                Renderer.Initialize(_context.GraphicsContext);
                _rendererInitialized = true;
                return true;
            }

            return false;
        }

        public void Initialize(IWindowInfo window)
        {
            _context = new DxGlContext(GlSettings, window);
            CheckRenderer();
            IsInitialized = true;
        }

        public void SetSize(CanvasInfo size)
        {
            var width = size.ActualWidth;
            var height = size.ActualHeight;
            var firstOrDefault = _framebuffers.FirstOrDefault();
            if (firstOrDefault == null || firstOrDefault.Width != width || firstOrDefault.Height != height)
            {
                foreach (var dxGlFramebuffer in _framebuffers)
                {
                    dxGlFramebuffer?.Dispose();
                }

                // _framebuffers = null;
                if (width > 0 && height > 0)
                {
                    for (int i = 0; i < _framebuffers.Length; i++)
                    {
                        _framebuffers[i] = new DxGLFramebuffer(_context, width, height, size.DpiScaleX, size.DpiScaleY);
                    }

                    _frontBuffer = _framebuffers[0];
                    backBuffer = _framebuffers[1];

                    if (CheckRenderer())
                    {
                        Renderer.Resize(_framebuffers.First().PixelSize);
                    }

                    // GL.Viewport(0, 0, _framebuffers.FramebufferWidth, _framebuffers.FramebufferHeight);
                }
            }
        }

        public DrawingDirective Render()
        {
            PreRender();
            Renderer?.Render(new GlRenderEventArgs(Width, Height, false));
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            PostRender();
            return new DrawingDirective(_frontBuffer.TranslateTransform, _frontBuffer.FlipYTransform);
        }

        public void Dispose()
        {
            _context?.Dispose();
            foreach (var dxGlFramebuffer in _framebuffers)
            {
                dxGlFramebuffer?.Dispose();
            }
        }
    }
}