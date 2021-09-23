using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public class BitmapCanvas : IRenderCanvas, IRenderBuffer
    {
        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private volatile WriteableBitmap _bitmap;

        public void Create(CanvasInfo info)
        {
            Bitmap = new WriteableBitmap((int)(info.ActualWidth * info.DpiScaleX),
                (int)(info.ActualHeight * info.DpiScaleY), 96 * info.DpiScaleX, 96 * info.DpiScaleY,
                PixelFormats.Pbgra32, null);
        }

        public ImageSource ImageSource => Bitmap;

        public bool IsAvailable => Bitmap != null && Bitmap.Width > 0 && Bitmap.Height > 0;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set => _bitmap = value;
        }
    }

    public class GLBitmapProcedure : IRenderProcedure
    {
        /// <summary>
        /// True if a new OpenGL context has been created since the last render call
        /// </summary>
        private bool _newContext;

        /// <summary>
        /// Information about the current window
        /// </summary>
        private IWindowInfo _windowInfo;

        /// <summary>
        /// An OpenTK graphics context
        /// </summary>
        private IGraphicsContext _context;

        /// <summary>
        /// The width of <see cref="_bitmap"/> in pixels/>
        /// </summary>
        private int _bitmapWidth;

        /// <summary>
        /// The height of <see cref="_bitmap"/> in pixels/>
        /// </summary>
        private int _bitmapHeight;

        private readonly BitmapCanvas _bitmapCanvas = new BitmapCanvas();

        /// <summary>
        /// The OpenGL framebuffer
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

        public void SwapBuffer()
        {
            _doubleBuffer.SwapBuffer();
        }

        public void FlushFrame(DrawingContext context)
        {
            if (_bitmapCanvas.IsAvailable)
            {
                var imageSource = _bitmapCanvas.ImageSource;
                context.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));
            }
        }

        public bool IsInitialized { get; private set; }

        public bool ReadyToRender => _bitmapWidth != 0 && _bitmapHeight != 0;

        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                _rendererInitialized = false;
            }
        }

        private readonly DoublePixelBuffer _doubleBuffer = new DoublePixelBuffer();

        private volatile bool _rendererInitialized = false;
        private IRenderer _renderer;

        public GLSettings GlSettings { get; }

        public GLBitmapProcedure(GLSettings glSettings)
        {
            GlSettings = glSettings;
        }

        public bool CanAsync { get; set; } = true;

        public void SizeCanvas(CanvasInfo size)
        {
            _bitmapCanvas.Create(size);
        }

        public void Begin()
        {
        }

        private BufferInfo _bufferInfo;
        
        public void End()
        {
            if (_bufferInfo.FrameBuffer != IntPtr.Zero)
            {
                var dirtyArea = _bufferInfo.RepaintRect;
                var bitmap = _bitmapCanvas.Bitmap;
                bitmap.Lock();
                bitmap.WritePixels(dirtyArea, _bufferInfo.FrameBuffer, _bufferInfo.BufferSize, bitmap.BackBufferStride);
                bitmap.AddDirtyRect(dirtyArea);
                bitmap.Unlock();
            }
        }

        public void Initialize(IWindowInfo window)
        {
            _windowInfo = window;
            var mode = new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false);
            _context = new GraphicsContext(mode, _windowInfo, GlSettings.MajorVersion, GlSettings.MinorVersion,
                GraphicsContextFlags.Default);
            _newContext = true;
            _context.LoadAll();
            _context.MakeCurrent(_windowInfo);
            CheckRenderer();
            IsInitialized = true;
        }

        /// <summary>
        /// Determines the current buffer size based on the ActualWidth and ActualHeight of the control
        /// </summary>
        /// <param name="info"></param>
        /// <param name="width">The new buffer width</param>
        /// <param name="height">The new buffer height</param>
        private void CalculateBufferSize(CanvasInfo info, out int width, out int height)
        {
            width = (int)(info.ActualWidth * info.DpiScaleX);
            height = (int)(info.ActualHeight * info.DpiScaleY);
        }

        /// <summary>
        /// A helper to actually invoke <see cref="GlRender"/>
        /// </summary>
        /// <param name="args">The render arguments</param>
        private void OnGlRender(GlRenderEventArgs args)
        {
            Renderer.Render(args);
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
                throw new GraphicsException(error.ToString());
        }

        public void SizeFrame(CanvasInfo canvas)
        {
            CalculateBufferSize(canvas, out var width, out var height);
            //Need Abs(...) > 1 to handle an edge case where the resizing the bitmap causes the height to increase in an infinite loop
            if (Math.Abs(_bitmapWidth - width) > 1 || Math.Abs(_bitmapHeight - height) > 1)
            {
                _bitmapWidth = width;
                _bitmapHeight = height;
                if (CheckRenderer())
                {
                    Renderer.Resize(new PixelSize(width, height));
                }

                AllocateFrameBuffers(width, height);
            }
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
                Renderer.Initialize(_context);
                _rendererInitialized = true;
                return true;
            }

            return false;
        }

        public bool Render()
        {
            if (!CheckRenderer())
            {
                return false;
            }

            /*if (!ReferenceEquals(GraphicsContext.CurrentContext, _context))
                _context.MakeCurrent(_windowInfo);*/
            var args =
                new GlRenderEventArgs(_bitmapWidth, _bitmapHeight, CheckNewContext());
            OnGlRender(args);
            /*var dirtyArea = args.RepaintRect;
            if (dirtyArea.Width <= 0 || dirtyArea.Height <= 0)
                return null;*/
            _doubleBuffer.ReadCurrent();
            _bufferInfo = _doubleBuffer.GetLatest();
            return true;
        }

        public IGraphicsContext Context => _context;

        /// <summary>
        /// Updates <see cref="_newContext"/>
        /// </summary>
        /// <returns>True if there is a new context</returns>
        private bool CheckNewContext()
        {
            if (!_newContext) return false;
            _newContext = false;
            return true;
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

            _doubleBuffer.Allocate(width, height);
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

            _doubleBuffer.Release();
        }


        public void Dispose()
        {
            ReleaseFrameBuffers();
            _context.Dispose();
            _context = null;
            _windowInfo.Dispose();
        }
    }
}