using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Platform.Windows;

namespace OpenTkWPFHost
{
    ///Renderer that uses DX_Interop for a fast-path.
    public class DXProcedure : IRenderProcedure
    {
        private DxGlContext _context;

        private DxGLFramebuffer _frameBuffer;

        private IRenderer _renderer;

        internal DxGLFramebuffer FrameBuffer => _frameBuffer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => FrameBuffer?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => FrameBuffer?.DxRenderTargetHandle ?? IntPtr.Zero;

        /// The OpenGL Framebuffer width
        public int Width => FrameBuffer?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => FrameBuffer?.FramebufferHeight ?? 0;

        private volatile bool _rendererInitialized = false;
        
        public bool IsInitialized { get; private set; }

        [Obsolete]
        public bool ReadyToRender => Renderer != null && Width != 0 && Height != 0;

        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                _rendererInitialized = false;
            }
        }

        public IRenderCanvas CreateCanvas(CanvasInfo info)
        {
            var dxCanvas = new DxCanvas(this);
            dxCanvas.Allocate(info);
            return dxCanvas;
        }

        public GLSettings GlSettings { get; }

        public DXProcedure(GLSettings glSettings)
        {
            this.GlSettings = glSettings;
        }

        public void SwapBuffer()
        {
        }

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender()
        {
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {FrameBuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {FrameBuffer.DxInteropRegisteredHandle});
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
            if (FrameBuffer == null || FrameBuffer.Width != width || FrameBuffer.Height != height)
            {
                FrameBuffer?.Dispose();
                if (width > 0 && height > 0)
                {
                    _frameBuffer = new DxGLFramebuffer(_context, width, height, size.DpiScaleX, size.DpiScaleY);
                    if (CheckRenderer())
                    {
                        Renderer.Resize(FrameBuffer.PixelSize);
                    }

                    // GL.Viewport(0, 0, _framebuffers.FramebufferWidth, _framebuffers.FramebufferHeight);
                }
            }
        }


        public void Render(IRenderCanvas canvas)
        {
            PreRender();
            Renderer.Render(new GlRenderEventArgs(Width, Height, false));
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            PostRender();
        }

        public IGraphicsContext Context => _context.GraphicsContext;


        public void Dispose()
        {
            FrameBuffer?.Dispose();
            _context?.Dispose();
        }
    }
}