using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    public class SolitaryPixelBuffer : IFrameBuffer
    {
        /// <summary>
        /// Indicate whether call 'glFlush' before read buffer
        /// <para>Recommend be true, but possibly cause stuck on low end cpu (2 physical core)</para>
        /// </summary>
        public bool EnableFlush { get; set; } = true;

        private int _width, _height;

        /// <summary>
        /// buffer which are reading
        /// </summary>
        private BufferInfo _bufferInfo = new BufferInfo();

        public void Allocate(PixelSize pixelSize)
        {
            var width = pixelSize.Width;
            var height = pixelSize.Height;
            this._width = width;
            this._height = height;
            var repaintRect = new Int32Rect(0, 0, width, height);
            var currentPixelBufferSize = width * height * 4;
            var bufferObject = GL.GenBuffer();
            _bufferInfo.GlBufferPointer = bufferObject;
            _bufferInfo.BufferSize = currentPixelBufferSize;
            _bufferInfo.RepaintPixelRect = repaintRect;
            GL.BindBuffer(BufferTarget.PixelPackBuffer, bufferObject);
            GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                BufferUsageHint.StaticDraw);
        }

        public BufferInfo FlushCurrentFrame()
        {
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _bufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            _bufferInfo.HasBuffer = true;
            if (EnableFlush)
            {
                GL.Flush();
            }

            return null; //todo:
        }

        public void SwapBuffer()
        {
        }

        public bool TryReadFrames(BufferArgs args, out FrameArgs frameArgs)
        {
            //todo:
            throw new NotImplementedException();
            var bufferInfo = args.BufferInfo;
            if (!bufferInfo.HasBuffer)
            {
                return false;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, this._bufferInfo.GlBufferPointer);
            GL.GetBufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, this._bufferInfo.BufferSize, IntPtr.Zero);
            return true;
        }

        public void Release()
        {
            var pointer = _bufferInfo.GlBufferPointer;
            if (pointer != 0)
            {
                GL.DeleteBuffer(pointer);
                _bufferInfo = new BufferInfo();
            }
        }

        public void Dispose()
        {
            Release();
        }
    }
}