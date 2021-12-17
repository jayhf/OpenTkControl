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

        public IFrameBuffer CreateFrameBuffer()
        {
            throw new NotImplementedException();
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
        public RenderArgs PostRender()
        {
            throw new NotImplementedException();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_frameBuffer.DxInteropRegisteredHandle});
            if (EnableFlush)
            {
                GL.Flush();
            }

            return null; //todo:
        }

        public GLContextBinding Initialize(IWindowInfo window, GLSettings settings)
        {
            if (IsInitialized)
            {
                throw new NotSupportedException("Initialized already!");
            }

            _context = new DxGlContext(settings, window);
            IsInitialized = true;
            return new GLContextBinding(_context.GraphicsContext, window);
        }

        public IFrameBuffer FrameBuffer { get; }

        public void SizeFrame(PixelSize pixelSize)
        {
            var width = pixelSize.Width;
            var height = pixelSize.Height;
            if (_frameBuffer == null || _frameBuffer.FramebufferWidth != width ||
                _frameBuffer.FramebufferHeight != height)
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