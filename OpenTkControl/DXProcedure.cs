using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Platform.Windows;

namespace OpenTkWPFHost
{
    internal class DXRenderBuffer : IRenderBuffer
    {
        public void Allocate(CanvasInfo canvasInfo)
        {
        }

        public void Swap()
        {
        }

        public FrameArgs ReadFrames(RenderArgs args)
        {
            return new DXFrameArgs(args.PixelSize, ((DXRenderArgs) args).RenderTargetIntPtr);
        }

        public void Release()
        {
            
        }
    }


    ///Renderer that uses DX_Interop for a fast-path.
    public class DXProcedure : IRenderProcedure
    {
        private DxGlContext _context;

        private DxGLFramebuffer _frameBuffer;

        public bool EnableFlush { get; set; } = true;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _frameBuffer?.GLFramebufferHandle ?? 0;

        public bool IsInitialized { get; private set; }

        public IRenderCanvas CreateCanvas()
        {
            return new DxCanvas();
        }

        public IRenderBuffer CreateFrameBuffer()
        {
            return new DXRenderBuffer();
        }

        public DXProcedure()
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
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_frameBuffer.DxInteropRegisteredHandle});
            if (EnableFlush)
            {
                GL.Flush();
            }

            return new DXRenderArgs(_frameBuffer.PixelSize, _frameBuffer.DxRenderTargetHandle);
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

        public void SizeFrame(PixelSize pixelSize)
        {
            if (_frameBuffer == null)
            {
                _frameBuffer = new DxGLFramebuffer(_context, pixelSize);
                return;
            }

            _frameBuffer.Release();
            _frameBuffer = new DxGLFramebuffer(_context, pixelSize);

            /*if (!_frameBuffer.PixelSize.Equals(pixelSize))
            {
                _frameBuffer.Release();
                _frameBuffer = new DxGLFramebuffer(_context, pixelSize);
            }*/
        }

        public void Dispose()
        {
            _frameBuffer?.Release();
            _context?.Dispose();
        }
    }
}