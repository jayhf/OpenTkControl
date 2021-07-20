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
    ///Renderer that uses DX_Interop for a fast-path.
    public class GLDXProcedure : IRenderProcedure
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private DxGlContext _context;

        public event Action<TimeSpan> GLRender;

        private DxGLFramebuffer _framebuffer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _framebuffer?.GLFramebufferHandle ?? 0;

        /// The OpenGL Framebuffer width
        public int Width => _framebuffer?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => _framebuffer?.FramebufferHeight ?? 0;

        private TimeSpan _lastFrameStamp;

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender()
        {
            _framebuffer.D3dImage.Lock();
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.GLFramebufferHandle);
            GL.Viewport(0, 0, _framebuffer.FramebufferWidth, _framebuffer.FramebufferHeight);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] {_framebuffer.DxInteropRegisteredHandle});
            _framebuffer.D3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _framebuffer.DxRenderTargetHandle);
            _framebuffer.D3dImage.AddDirtyRect(new Int32Rect(0, 0, _framebuffer.FramebufferWidth,
                _framebuffer.FramebufferHeight));
            _framebuffer.D3dImage.Unlock();
        }

        public IRenderer Renderer { get; set; }


        public GLDXProcedure(GLSettings glSettings)
        {
            this.GlSettings = glSettings;
        }

        public GLSettings GlSettings { get; }

        public void Initialize(IWindowInfo window)
        {
            _context = new DxGlContext(GlSettings, window);
        }

        public void SizeCanvas(CanvasInfo size)
        {
            var width = size.Width;
            var height = size.Height;
            if (_framebuffer == null || _framebuffer.Width != width || _framebuffer.Height != height)
            {
                _framebuffer?.Dispose();
                _framebuffer = null;
                if (width > 0 && height > 0)
                {
                    _framebuffer = new DxGLFramebuffer(_context, width, height, size.DpiScaleX, size.DpiScaleY);
                }
            }
        }

        public DrawingDirective Render()
        {
            if (_framebuffer == null)
            {
                return null;
            }

            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;
            PreRender();
            GLRender?.Invoke(deltaT);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            return new DrawingDirective(_framebuffer.TranslateTransform, _framebuffer.FlipYTransform,_framebuffer.D3dImage);
            
        }

        public void Dispose()
        {
            _context?.Dispose();
            _framebuffer?.Dispose();
        }
    }
}