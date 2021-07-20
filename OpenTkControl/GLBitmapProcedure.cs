using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTkControl
{
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

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private volatile WriteableBitmap _bitmap;

        private IRenderer _renderer;

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

        /// <summary>
        /// A Task that represents updating the screen with the current WriteableBitmap back buffer
        /// </summary>
        private Task _previousUpdateImageTask;

        public IRenderer Renderer { get; set; }

        private readonly DoublePixelBuffer _doubleBuffer = new DoublePixelBuffer();

        private volatile bool _rendererInitialized = false;

        public GLSettings GlSettings { get; }

        public GLBitmapProcedure(GLSettings glSettings)
        {
            GlSettings = glSettings;
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
            if (Renderer != null)
            {
                this.Renderer.Initialize(_context);
                _rendererInitialized = true;
            }
        }

        /// <summary>
        /// Determines the current buffer size based on the ActualWidth and ActualHeight of the control
        /// </summary>
        /// <param name="info"></param>
        /// <param name="width">The new buffer width</param>
        /// <param name="height">The new buffer height</param>
        private void CalculateBufferSize(CanvasInfo info, out int width, out int height)
        {
            width = (int) (info.Width / info.DpiScaleX);
            height = (int) (info.Height / info.DpiScaleY);
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

        public void SizeCanvas(CanvasInfo canvas)
        {
            CalculateBufferSize(canvas, out var width, out var height);
            //Need Abs(...) > 1 to handle an edge case where the resizing the bitmap causes the height to increase in an infinite loop
            if (Math.Abs(_bitmapWidth - width) > 1 || Math.Abs(_bitmapHeight - height) > 1)
            {
                _bitmapWidth = width;
                _bitmapHeight = height;

                if (Renderer != null)
                {
                    if (!_rendererInitialized)
                    {
                        Renderer.Initialize(_context);
                    }

                    Renderer.Resize(new PixelSize(width, height));
                }

                AllocateFrameBuffers(width, height);
            }
        }

        public DrawingDirective Render()
        {
            if (!ReferenceEquals(GraphicsContext.CurrentContext, _context))
                _context.MakeCurrent(_windowInfo);
            if (!_rendererInitialized)
            {
                Renderer.Initialize(this._context);
            }

            var args =
                new GlRenderEventArgs(_bitmapWidth, _bitmapHeight, CheckNewContext());
            OnGlRender(args);
            var dirtyArea = args.RepaintRect;
            if (dirtyArea.Width <= 0 || dirtyArea.Height <= 0)
                return null;

            try
            {
                _previousUpdateImageTask?.Wait();
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                _previousUpdateImageTask = null;
            }

            _doubleBuffer.SwapBuffer();
            _doubleBuffer.ReadCurrent();
            var copyLatest = _doubleBuffer.GetLatest();
            if (copyLatest.FrameBuffer == IntPtr.Zero)
            {
                return null;
            }

            UpdateImage(copyLatest);
            return new DrawingDirective(null, null, _bitmap, true); //允许异步
        }

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
        /// Updates what is currently being drawn on the screen from the back buffer.
        /// Must be called from the UI thread
        /// </summary>
        /// <param name="info"></param>
        private void UpdateImage(BufferInfo info)
        {
            var dirtyArea = info.RepaintRect;
            if (info.IsResized)
            {
                _bitmap = new WriteableBitmap(info.Width, info.Height, 96, 96, PixelFormats.Pbgra32, null);
            }

            _bitmap.Lock();
            _bitmap.WritePixels(dirtyArea, info.FrameBuffer, info.BufferSize, _bitmap.BackBufferStride);
            _bitmap.AddDirtyRect(dirtyArea);
            _bitmap.Unlock();
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