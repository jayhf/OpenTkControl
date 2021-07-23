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
    public class DxCanvas : IRenderCanvas
    {
        public D3DImage Image { get; private set; }

        public ImageSource Canvas => Image;

        private GLDXProcedure dxProcedure;

        public DxCanvas(GLDXProcedure dxProcedure)
        {
            this.dxProcedure = dxProcedure;
        }

        public void Create(CanvasInfo info)
        {
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            Image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
        }

        public void Begin()
        {
            Image.Lock();
        }

        public void End()
        {
            var dxCanvasImage = this.Image;
            dxCanvasImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                dxProcedure.DxRenderTargetHandle);
            dxCanvasImage.AddDirtyRect(new Int32Rect(0, 0, dxProcedure.Width,
                dxProcedure.Height));
            dxCanvasImage.Unlock();
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

        private DxCanvas dxCanvas;

        public IRenderCanvas Canvas => dxCanvas;

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
            dxCanvas = new DxCanvas(this);
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

        public void SizeCanvas(CanvasInfo size)
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

        public void Begin()
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }

        public DrawingDirective Render()
        {
            PreRender();
            Renderer?.Render(new GlRenderEventArgs(Width, Height, false));
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            PostRender();
            return new DrawingDirective(_framebuffer.TranslateTransform, _framebuffer.FlipYTransform);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _framebuffer?.Dispose();
        }
    }
}