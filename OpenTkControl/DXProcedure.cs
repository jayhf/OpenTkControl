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

        internal DxGLFramebuffer FrameBuffer => _frameBuffer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => FrameBuffer?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => FrameBuffer?.DxRenderTargetHandle ?? IntPtr.Zero;

        /// The OpenGL Framebuffer width
        public int Width => FrameBuffer?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => FrameBuffer?.FramebufferHeight ?? 0;

        public bool IsInitialized { get; private set; }

        public IRenderCanvas CreateCanvas()
        {
            return new DxCanvas(this);
        }

        public DXProcedure()
        {
            
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

        public IGraphicsContext Initialize(IWindowInfo window, GLSettings settings)
        {
            if (IsInitialized)
            {
                return this._context.GraphicsContext;
            }

            IsInitialized = true;
            _context = new DxGlContext(settings, window);
            return _context.GraphicsContext;
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
                }
            }
        }

        public void Render(IRenderCanvas canvas, IRenderer renderer)
        {
            PreRender();
            renderer.Render(new GlRenderEventArgs(Width, Height, false));
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            PostRender();
        }

        public void Dispose()
        {
            FrameBuffer?.Dispose();
            _context?.Dispose();
        }
    }
}