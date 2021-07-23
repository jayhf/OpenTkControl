using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using OpenTK.Platform.Windows;
using Buffer = OpenTK.Graphics.OpenGL.Buffer;

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

    ///Renderer that uses DX_Interop for a fast-path.
    public class GLDXProcedure : IRenderProcedure
    {
        private DxGlContext _context;
        private DxGLFramebuffer _framebuffers;
        private IRenderer _renderer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _framebuffers?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => _framebuffers?.DxRenderTargetHandle ?? IntPtr.Zero;

        /// The OpenGL Framebuffer width
        public int Width => _framebuffers?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => _framebuffers?.FramebufferHeight ?? 0;

        private volatile bool _rendererInitialized = false;

        private readonly DoubleDxCanvas _doubleDxCanvas = new DoubleDxCanvas();

        public void SwapBuffer()
        {
            this._doubleDxCanvas.SwapBuffer();
        }

        public IRenderCanvas GetFrontBuffer()
        {
            return _doubleDxCanvas.GetFrontBuffer();
        }

        public bool IsInitialized { get; private set; }


        public bool ReadyToRender => Width != 0 && Height != 0;

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

        public void SizeCanvas(CanvasInfo info)
        {
            _doubleDxCanvas.Create(info);
        }

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
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffers.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffers.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffers.DxInteropRegisteredHandle});
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

        public void SizeFrame(CanvasInfo size)
        {
            var width = size.ActualWidth;
            var height = size.ActualHeight;
            if (_framebuffers == null || _framebuffers.Width != width || _framebuffers.Height != height)
            {
                _framebuffers?.Dispose();
                if (width > 0 && height > 0)
                {
                    _framebuffers = new DxGLFramebuffer(_context, width, height, size.DpiScaleX, size.DpiScaleY);
                    if (CheckRenderer())
                    {
                        Renderer.Resize(_framebuffers.PixelSize);
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
            return new DrawingDirective(_framebuffers.TranslateTransform, _framebuffers.FlipYTransform);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _framebuffers?.Dispose();
        }
    }
}