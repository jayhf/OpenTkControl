using System;
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

        public bool EnableFlush { get; set; } = true;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _frameBuffer?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => _frameBuffer?.DxRenderTargetHandle ?? IntPtr.Zero;

        public bool IsInitialized { get; private set; }

        public void BindCanvas(IRenderCanvas canvas)
        {
            ((DxCanvas) canvas).FrameBuffer = this.DxRenderTargetHandle;
        }

        public IRenderCanvas CreateCanvas()
        {
            return new DxCanvas();
        }


        public DXProcedure()
        {
        }

        public void SwapBuffer()
        {
        }

        /// Sets up the framebuffer, directx stuff for rendering.
        public void PreRender()
        {
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {_frameBuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        public BufferArgs PostRender()
        {
            throw new NotImplementedException();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_frameBuffer.DxInteropRegisteredHandle});
            if (EnableFlush)
            {
                GL.Flush();
            }

            return null;//todo:
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

        public IFrameBuffer FrameBuffer { get; }


        public void SizeFrame(PixelSize pixelSize)
        {
            var width = pixelSize.Width;
            var height = pixelSize.Height;
            if (_frameBuffer == null || _frameBuffer.FramebufferWidth != width || _frameBuffer.FramebufferHeight != height)
            {
                _frameBuffer?.Dispose();
                _frameBuffer = null;
                if (width > 0 && height > 0)
                {
                    _frameBuffer = new DxGLFramebuffer(_context, width, height);
                }
            }
        }

        public void Dispose()
        {
            _frameBuffer?.Dispose();
            _context?.Dispose();
        }
    }
}