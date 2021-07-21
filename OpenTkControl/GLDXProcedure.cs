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

        public void Create(CanvasInfo info)
        {
            Image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
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

        /// The OpenGL Framebuffer width
        public int Width => _framebuffer?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => _framebuffer?.FramebufferHeight ?? 0;

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender()
        {
            dxCanvas.Image.Lock();
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffer.DxInteropRegisteredHandle});
            var dxCanvasImage = dxCanvas.Image;
            dxCanvasImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                _framebuffer.DxRenderTargetHandle);
            dxCanvasImage.AddDirtyRect(new Int32Rect(0, 0, _framebuffer.FramebufferWidth,
                _framebuffer.FramebufferHeight));
            dxCanvasImage.Unlock();
        }


        private volatile bool _rendererInitialized = false;

        DxCanvas dxCanvas = new DxCanvas();
        public IRenderCanvas Canvas => dxCanvas;

        public bool IsInitialized { get; private set; }

        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                _rendererInitialized = false;
            }
        }


        public GLDXProcedure(GLSettings glSettings)
        {
            this.GlSettings = glSettings;
        }

        public GLSettings GlSettings { get; }

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

        public DrawingDirective Render()
        {
            if (_framebuffer == null)
            {
                return null;
            }

            if (!CheckRenderer())
            {
                return null;
            }

            PreRender();
            Renderer?.Render(new GlRenderEventArgs(Width, Height, false));
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            PostRender();
            return new DrawingDirective(_framebuffer.TranslateTransform, _framebuffer.FlipYTransform,
                Canvas.Canvas);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _framebuffer?.Dispose();
        }
    }
}