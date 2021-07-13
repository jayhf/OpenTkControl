using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkControl
{
    public class DoubleBuffer
    {
        private int _doublePixelBuffer0, _doublePixelBuffer1;

        private int _width, _height;

        private BufferInfo _copyBufferInfo;

        private BufferInfo _readBufferInfo;

        public void Allocate(int width, int height)
        {
            this._width = width;
            this._height = height;
            if (_readBufferInfo.Width != width || _readBufferInfo.Height != height)
            {
                _readBufferInfo.IsResized = true;
                /*_readBufferInfo.FrameBuffer = IntPtr.Zero;
                _copyBufferInfo.FrameBuffer = IntPtr.Zero;*/
                _readBufferInfo.RepaintRect = new Int32Rect(0, 0, _width, _height);
            }
            else
            {
                _readBufferInfo.IsResized = false;
                return;
            }

            var currentPixelBufferSize = width * height * 4;
            _readBufferInfo.BufferSize = currentPixelBufferSize;
            _doublePixelBuffer0 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _doublePixelBuffer0);
            GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                BufferUsageHint.StreamRead);
            _doublePixelBuffer1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _doublePixelBuffer1);
            GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                BufferUsageHint.StreamRead);
            _readBuffer = _doublePixelBuffer0;
            _copyBuffer = _doublePixelBuffer1;
        }

        public void Release()
        {
            if (_doublePixelBuffer0 != 0)
            {
                GL.DeleteBuffer(_doublePixelBuffer0);
            }

            if (_doublePixelBuffer1 != 0)
            {
                GL.DeleteBuffer(_doublePixelBuffer1);
            }
        }

        private int _readBuffer, _copyBuffer;

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        public void ReadCurrent()
        {
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _readBuffer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
        }

        public void SwapBuffer()
        {
            var buffer = _copyBuffer;
            _copyBuffer = _readBuffer;
            _readBuffer = buffer;
            _copyBufferInfo = _readBufferInfo;
            _readBufferInfo.IsResized = false;
        }

        public BufferInfo GetLatest()
        {
            if (_copyBufferInfo.Equals(default(BufferInfo)))
            {
                return default;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _copyBuffer);
            var mapBuffer = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            _copyBufferInfo.FrameBuffer = mapBuffer;
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            return _copyBufferInfo;
        }
    }
}