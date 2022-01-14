using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public class PureFrameBuffer : IFrameBuffer
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

        public void Allocate(RenderTargetInfo renderTargetInfo)
        {
            var width = renderTargetInfo.PixelWidth;
            var height = renderTargetInfo.PixelHeight;
            _frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

            _renderBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width,
                height);

            _depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24,
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
        }

        public void Release()
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

        public void PreWrite()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _frameBuffer);
        }

        public void PostRead()
        {
            
        }
    }
}