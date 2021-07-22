using System;
using System.Diagnostics;
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
        private readonly DxCanvas[] _dxCanvasArray = new DxCanvas[] {new DxCanvas(), new DxCanvas()};

        private DxCanvas _backBuffer, _frontBuffer;

        public DoubleDxCanvas()
        {
            _backBuffer = _dxCanvasArray[0];
            _frontBuffer = _dxCanvasArray[1];
        }

        public DxCanvas GetReadBuffer()
        {
            return _frontBuffer;
        }

        public DxCanvas GetWriteBuffer()
        {
            return _backBuffer;
        }

        public void SwapBuffer()
        {
            var mid = _backBuffer;
            _backBuffer = _frontBuffer;
            _frontBuffer = mid;
        }

        public ImageSource GetFrontSource()
        {
            return GetReadBuffer().GetFrontSource();
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

        public ImageSource GetFrontSource()
        {
            return _image;
        }

        public void Create(CanvasInfo info)
        {
            _image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
        }
    }

    ///Renderer that uses DX_Interop for a fast-path.
    public class GLDXProcedure : IRenderProcedure
    {
        private DxGlContext _context;
        private DxGLFramebuffer _framebuffer;
        private IRenderer _renderer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _framebuffer?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => _framebuffer?.DxRenderTargetHandle ?? IntPtr.Zero;

        /// The OpenGL Framebuffer width
        public int Width => _framebuffer?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => _framebuffer?.FramebufferHeight ?? 0;

        private volatile bool _rendererInitialized = false;

        private DoubleDxCanvas doubleDxCanvas = new DoubleDxCanvas();

        public void SwapBuffer()
        {
            this.doubleDxCanvas.SwapBuffer();
        }

        public bool IsInitialized { get; private set; }

        public bool CanRender
        {
            get { return _framebuffer != null; }
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

        public IRenderCanvas Buffer => doubleDxCanvas;

        public void Begin()
        {
            var writeBuffer = doubleDxCanvas.GetWriteBuffer().Image;
            writeBuffer.Lock();
        }

        public void End()
        {
            var dxCanvasImage = doubleDxCanvas.GetWriteBuffer().Image;
            dxCanvasImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                this.DxRenderTargetHandle);
            dxCanvasImage.AddDirtyRect(new Int32Rect(0, 0, this.Width,
                this.Height));
            dxCanvasImage.Unlock();
        }

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender()
        {
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffer.DxInteropRegisteredHandle});
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
            if (_framebuffer == null || _framebuffer.Width != width || _framebuffer.Height != height)
            {
                _framebuffer?.Dispose();
                _framebuffer = null;
                if (width > 0 && height > 0)
                {
                    _framebuffer = new DxGLFramebuffer(_context, width, height, size.DpiScaleX, size.DpiScaleY);
                    if (CheckRenderer())
                    {
                        Renderer.Resize(_framebuffer.PixelSize);
                    }

                    GL.Viewport(0, 0, _framebuffer.FramebufferWidth, _framebuffer.FramebufferHeight);
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
            return new DrawingDirective(_framebuffer.TranslateTransform, _framebuffer.FlipYTransform,
                Buffer.GetFrontSource());
        }

        public void Dispose()
        {
            _context?.Dispose();
            _framebuffer?.Dispose();
        }
    }
}