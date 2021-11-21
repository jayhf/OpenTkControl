using System;
using System.Windows.Controls;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;


namespace OpenTkWPFHost
{
    public class BitmapProcedure : IRenderProcedure
    {
        /// <summary>
        /// An OpenTK graphics context
        /// </summary>
        private IGraphicsContext _context;

        /// <summary>
        /// The OpenGL FrameBuffer
        /// </summary>
        private int _frameBuffer;

        /// <summary>
        /// The OpenGL render buffer. It stores data in Rgba8 format with color attachment 0
        /// </summary>
        private int _renderBuffer;

        /// <summary>
        /// The OpenGL depth buffer
        /// </summary>
        private int _depthBuffer;

        /// <summary>
        /// can set pixel buffer based on your machine specification. Recommend is double pbo.
        /// </summary>
        public IFrameBuffer FrameBuffer { get; set; } = new MultiStoragePixelBuffer(3);

        public bool IsInitialized { get; private set; }

        public BitmapProcedure()
        {
        }

        public void PreRender()
        {
        }

        public BufferArgs PostRender()
        {
            var flushCurrentFrame = FrameBuffer.FlushCurrentFrame();
            return new BufferArgs()
            {
                BufferInfo = flushCurrentFrame,
                HostBufferIntPtr = _currentCanvas.DisplayBuffer,
                CanvasInfo = _currentCanvas.Info,
                
            };
        }

        private BitmapCanvas _currentCanvas = null;

        public void BindCanvas(IRenderCanvas canvas)
        {
#if DEBUG
            if (canvas is BitmapCanvas bitmapCanvas)
            {
                _currentCanvas = bitmapCanvas;
            }
            else
            {
                throw new NotSupportedException("Not supported type!");
            }
#else
            _currentCanvas = (BitmapCanvas) canvas;
#endif
        }

        public IRenderCanvas CreateCanvas()
        {
            return new BitmapCanvas();
        }

        public IGraphicsContext Initialize(IWindowInfo window, GLSettings settings)
        {
            // var mode = new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false);
            _context = new GraphicsContext(settings.GraphicsMode, window, settings.MajorVersion,
                settings.MinorVersion,
                GraphicsContextFlags.Default) {SwapInterval = (int) settings.SyncMode};
            _context.LoadAll();
            _context.MakeCurrent(window);
            IsInitialized = true;
            return _context;
        }

        public void SizeFrame(PixelSize pixelSize)
        {
            var height = pixelSize.Height;
            var width = pixelSize.Width;
            ReleaseFrameBuffers();
            _frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

            _depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width,
                height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depthBuffer);

            _renderBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer, _renderBuffer);
            var error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new GraphicsErrorException("Error creating frame buffer: " + error);
            }
        }

        /// <summary>
        /// Releases all of the OpenGL buffers currently in use
        /// </summary>
        protected virtual void ReleaseFrameBuffers()
        {
            if (_frameBuffer != 0)
            {
                GL.DeleteFramebuffer(_frameBuffer);
                _frameBuffer = 0;
            }

            if (_depthBuffer != 0)
            {
                GL.DeleteRenderbuffer(_depthBuffer);
                _depthBuffer = 0;
            }

            if (_renderBuffer != 0)
            {
                GL.DeleteRenderbuffer(_renderBuffer);
                _renderBuffer = 0;
            }
        }

        public void Dispose()
        {
            ReleaseFrameBuffers();
            _context.Dispose();
            _context = null;
        }
    }
}