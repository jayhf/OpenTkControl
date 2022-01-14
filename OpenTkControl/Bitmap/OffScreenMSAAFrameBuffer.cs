using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public class OffScreenMSAAFrameBuffer : IFrameBuffer
    {
        /// <summary>
        /// The OpenGL FrameBuffer
        /// </summary>
        public int FrameBufferObject => _frameBuffer;

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

        private readonly PureFrameBuffer _pureFrameBuffer;

        private readonly int _multiSamples;

        private RenderTargetInfo _renderTargetInfo;

        public OffScreenMSAAFrameBuffer(int multiSamples)
        {
            if (multiSamples < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(multiSamples));
            }

            var integer = GL.GetInteger(GetPName.MaxSamples);
            if (multiSamples > integer)
            {
                throw new ArgumentOutOfRangeException(nameof(multiSamples), multiSamples,
                    $"Can't set multi-samples over than {multiSamples}");
            }

            _multiSamples = multiSamples;
            _pureFrameBuffer = new PureFrameBuffer();
        }


        public void Allocate(RenderTargetInfo renderTargetInfo)
        {
            this._renderTargetInfo = renderTargetInfo;
            var width = renderTargetInfo.PixelWidth;
            var height = renderTargetInfo.PixelHeight;
            _frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferObject);

            _renderBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _multiSamples, RenderbufferStorage.Rgba8,
                width,
                height);

            _depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _multiSamples,
                RenderbufferStorage.DepthComponent24,
                width, height);

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer, _renderBuffer);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depthBuffer);
            var error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new GraphicsErrorException("Error creating frame buffer: " + error);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _pureFrameBuffer.Allocate(renderTargetInfo);
        }

        public void Release()
        {
            _pureFrameBuffer.Release();
            if (FrameBufferObject != 0)
            {
                GL.DeleteFramebuffer(FrameBufferObject);
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

        public void PreWrite()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _frameBuffer);
        }

        public void PostRead()
        {
            var width = _renderTargetInfo.PixelWidth;
            var height = _renderTargetInfo.PixelHeight;
            // GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _frameBuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _pureFrameBuffer.FrameBufferObject);
            // GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _pureFrameBuffer.FrameBufferObject);
        }
    }
}