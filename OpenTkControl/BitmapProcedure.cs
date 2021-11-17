using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public class BitmapProcedure : IRenderProcedure
    {
        
        /// <summary>
        /// Information about the current window
        /// </summary>
        private IWindowInfo _windowInfo;

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

        private readonly IPixelBuffer _doublePixelBuffer = new MultiStoragePixelBuffer();

        public bool IsInitialized { get; private set; }

        public BitmapProcedure()
        {
        }

        public void BindCanvas(IRenderCanvas canvas)
        {
            var bitmapCanvas = (BitmapCanvas)canvas;
            if (_doublePixelBuffer.TryReadFromBufferInfo(IntPtr.Zero, out var bufferInfo))
            {
                bitmapCanvas.ReadBufferInfo = bufferInfo;
            }
        }

        public IRenderCanvas CreateCanvas()
        {
            return new BitmapCanvas();
        }

        public IGraphicsContext Initialize(IWindowInfo window, GLSettings settings)
        {
            _windowInfo = window;
            var mode = new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false);
            _context = new GraphicsContext(mode, _windowInfo, settings.MajorVersion, settings.MinorVersion,
                GraphicsContextFlags.Default);
            _context.LoadAll();
            _context.MakeCurrent(_windowInfo);
            IsInitialized = true;
            return _context;
        }

        public void SwapBuffer()
        {
            _doublePixelBuffer.SwapBuffer();
        }

        /// <summary>
        /// Determines the current buffer size based on the ActualWidth and ActualHeight of the control
        /// </summary>
        /// <param name="info"></param>
        /// <param name="width">The new buffer width</param>
        /// <param name="height">The new buffer height</param>
        private static void CalculateBufferSize(CanvasInfo info, out int width, out int height)
        {
            width = (int)Math.Ceiling(info.ActualWidth * info.DpiScaleX);
            height = (int)Math.Ceiling(info.ActualHeight * info.DpiScaleY);
        }

        public void SizeFrame(CanvasInfo canvas)
        {
            CalculateBufferSize(canvas, out var width, out var height);
            AllocateFrameBuffers(width, height);
        }

        public void PreRender()
        {

        }

        public void PostRender()
        {

            _doublePixelBuffer.FlushCurrentFrame();
            // GL.Flush();//从结果（intel uhd630）来看，不适用该指令会导致帧率大幅下降
        }

        /// <summary>
        /// Creates new OpenGl buffers of the specified size, including <see cref="_frameBuffer"/>, <see cref="_depthBuffer"/>,
        /// and <see cref="_renderBuffer" />. This method is virtual so the behavior can be overriden, but the default behavior
        /// should work for most purposes.
        /// </summary>
        /// <param name="width">The width of the new buffers</param>
        /// <param name="height">The height of the new buffers</param>
        protected virtual void AllocateFrameBuffers(int width, int height)
        {
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

            _doublePixelBuffer.Allocate(width, height);
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

            _doublePixelBuffer.Release();
        }

        public void Dispose()
        {
            ReleaseFrameBuffers();
            _context.Dispose();
            _context = null;
        }
    }
}