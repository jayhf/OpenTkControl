using System;
using System.Diagnostics;
using System.Windows;
using OpenTK.Graphics.OpenGL4;
using Buffer = OpenTK.Graphics.OpenGL4.Buffer;

namespace OpenTkWPFHost
{
    public class DoublePixelBuffer
    {
        private int _width, _height;

        /// <summary>
        /// buffer which are reading
        /// </summary>
        private BufferInfo _readBufferInfo = new BufferInfo();

        /// <summary>
        /// buffer which are writing
        /// </summary>
        private BufferInfo _writeBufferInfo = new BufferInfo();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Allocate(int width, int height)
        {
            this._width = width;
            this._height = height;
            var repaintRect = new Int32Rect(0, 0, width, height);
            var currentPixelBufferSize = width * height * 4;
            _writeBufferInfo.BufferSize = currentPixelBufferSize;
            _writeBufferInfo.RepaintRect = repaintRect;
            var writeBuffer = GL.GenBuffer();
            _writeBufferInfo.GlBufferPointer = writeBuffer;
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBuffer);
            GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                BufferUsageHint.StreamRead);
            var readBuffer = GL.GenBuffer();
            _readBufferInfo.GlBufferPointer = readBuffer;
            _readBufferInfo.BufferSize = currentPixelBufferSize;
            _readBufferInfo.RepaintRect = repaintRect;
            GL.BindBuffer(BufferTarget.PixelPackBuffer, readBuffer);
            GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                BufferUsageHint.StreamRead);
        }

        public void Release()
        {
            var writeBuffer = _writeBufferInfo.GlBufferPointer;
            if (writeBuffer != 0)
            {
                GL.DeleteBuffer(writeBuffer);
                _writeBufferInfo = new BufferInfo();
            }

            var readBuffer = _readBufferInfo.GlBufferPointer;
            if (readBuffer != 0)
            {
                GL.DeleteBuffer(readBuffer);
                _readBufferInfo = new BufferInfo();
            }
        }

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        public void FlushCurrentFrame()
        {
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            _writeBufferInfo.HasBuffer = true;
        }

        public void SwapBuffer()
        {
            (_readBufferInfo, _writeBufferInfo) = (_writeBufferInfo, _readBufferInfo);
        }
        
        public bool TryReadFromBufferInfo(IntPtr ptr, out BufferInfo bufferInfo)
        {
            if (!_readBufferInfo.HasBuffer)
            {
                bufferInfo = default;
                return false;
            }
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _readBufferInfo.GlBufferPointer);
            /*GL.BufferData(BufferTarget.PixelPackBuffer, _readBufferInfo.BufferSize, IntPtr.Zero,
                BufferUsageHint.StreamRead); */ //增加该指令有助于提升性能，但画面会闪烁
            var mapBuffer = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            _readBufferInfo.FrameBuffer = mapBuffer;
            var bufferSize = (long) _readBufferInfo.BufferSize;
            unsafe
            {
                System.Buffer.MemoryCopy(mapBuffer.ToPointer(), ptr.ToPointer(), bufferSize, bufferSize);
            }

            bufferInfo = _readBufferInfo;
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            return true;
        }
    }
}