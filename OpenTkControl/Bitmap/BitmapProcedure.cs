using System;
using System.Diagnostics;
using System.Drawing;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Configuration;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public class BitmapProcedure : IRenderProcedure
    {
        public bool EnableMultiSamples { get; private set; }

        private IGraphicsContext _context;

        private IFrameBuffer _frameBuffer;

        private RenderTargetInfo _renderTargetInfo;

        private MultiStoragePixelBuffer _multiStoragePixelBuffer;

        public bool IsInitialized { get; private set; }

        public BitmapProcedure()
        {
        }

        public void PreRender()
        {
            _frameBuffer.PreWrite();
            /*GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);*/
        }

        public RenderArgs PostRender()
        {
            _frameBuffer.PostRead();
            var pixelBufferInfo = _multiStoragePixelBuffer.ReadPixel();
            return new BitmapRenderArgs(this._renderTargetInfo)
            {
                BufferInfo = pixelBufferInfo,
            };
        }

        public void Swap()
        {
            _multiStoragePixelBuffer.Swap();
        }

        public IRenderCanvas CreateCanvas()
        {
            //over than 2 will occasion flash
            return new BitmapCanvas(2);
        }

        public IRenderBuffer CreateRenderBuffer()
        {
            return _multiStoragePixelBuffer;
        }

        private GLSettings _settings;

        public GLContextBinding Initialize(IWindowInfo window, GLSettings settings)
        {
            if (IsInitialized)
            {
                throw new NotSupportedException("Initialized already!");
            }

            _context = settings.CreateContext(window);
            _context.LoadAll();
            _context.MakeCurrent(window);
            _multiStoragePixelBuffer = new MultiStoragePixelBuffer(3);
            this._settings = settings;
            var samples = settings.GraphicsMode.Samples;
            if (samples > 1)
            {
                this._frameBuffer = new OffScreenMSAAFrameBuffer(samples);
                this.EnableMultiSamples = true;
                GL.Enable(EnableCap.Multisample);
            }
            else
            {
                this._frameBuffer = new PureFrameBuffer();
                this.EnableMultiSamples = false;
            }

            IsInitialized = true;
            return new GLContextBinding(_context, window);
        }

        public void Apply(RenderTargetInfo renderTargetInfo)
        {
            this._renderTargetInfo = renderTargetInfo;
            _multiStoragePixelBuffer.Release();
            _frameBuffer.Release();
            _frameBuffer.Allocate(renderTargetInfo);
            _multiStoragePixelBuffer.Allocate(renderTargetInfo);
        }

        public void Dispose()
        {
            _multiStoragePixelBuffer?.Release();
            _multiStoragePixelBuffer?.Dispose();
            _frameBuffer?.Release();
            _context?.Dispose();
            _context = null;
        }
    }
}