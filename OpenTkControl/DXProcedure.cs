using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Platform.Windows;

namespace OpenTkWPFHost
{
    ///Renderer that uses DX_Interop for a fast-path.
    public class DXProcedure : IRenderProcedure, IRenderBuffer
    {
        private DxGlContext _context;

        private DxGLFramebuffer _frameBuffer;

        public bool EnableFlush { get; set; } = true;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _frameBuffer?.GLFramebufferHandle ?? 0;

        public bool IsInitialized { get; private set; }

        public void Swap()
        {
        }

        public IRenderCanvas CreateCanvas()
        {
            return new MultiDxCanvas();
        }

        public IRenderBuffer CreateRenderBuffer()
        {
            return this;
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

            return new DXRenderArgs(_renderTargetInfo, _frameBuffer.DxRenderTargetHandle);
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

        private RenderTargetInfo _renderTargetInfo;

        public void Apply(RenderTargetInfo renderTarget)
        {
            this._renderTargetInfo = renderTarget;
            var pixelSize = renderTarget.PixelSize;
            if (_frameBuffer == null)
            {
                _frameBuffer = new DxGLFramebuffer(_context, pixelSize);
                return;
            }

            _frameBuffer.Release();
            _frameBuffer = new DxGLFramebuffer(_context, pixelSize);
        }

        public void Dispose()
        {
            _frameBuffer?.Release();
            _context?.Dispose();
        }

        public FrameArgs ReadFrames(RenderArgs args)
        {
            return new DXFrameArgs(((DXRenderArgs) args).RenderTargetIntPtr, args.TargetInfo);
        }
    }
}