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
        private MultiStoragePixelBuffer _multiStoragePixelBuffer = new MultiStoragePixelBuffer(5);

        public bool IsInitialized { get; private set; }

        public BitmapProcedure()
        {
        }

        public void PreRender()
        {
        }

        public RenderArgs PostRender()
        {
            var pixelBufferInfo = _multiStoragePixelBuffer.ReadPixel();
            return new BitmapRenderArgs()
            {
                BufferInfo = pixelBufferInfo,
                PixelSize = pixelBufferInfo.PixelSize,
            };
        }

        public IRenderCanvas CreateCanvas()
        {
            //over than 2 will occasion flash
            return new BitmapCanvas(2);
        }

        public IRenderBuffer CreateFrameBuffer()
        {
            return _multiStoragePixelBuffer;
        }

        public GLContextBinding Initialize(IWindowInfo window, GLSettings settings)
        {
            if (IsInitialized)
            {
                throw new NotSupportedException("Initialized already!");
            }

            _context = settings.CreateContext(window);
            _context.LoadAll();
            _context.MakeCurrent(window);
            IsInitialized = true;
            return new GLContextBinding(_context, window);
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