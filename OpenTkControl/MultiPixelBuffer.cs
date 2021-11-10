using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    [Obsolete("在5820k gtx970下，通过对1080p的测试，超过双重缓冲并不能提高帧率")]
    public class MultiPixelBuffer
    {
        private readonly BufferInfo[] _bufferInfos = new BufferInfo[3];

        private int _width, _height;

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private int _currentWriteBufferIndex = 1;

        private bool allocated = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Allocate(int width, int height)
        {
            allocated = true;
            var repaintRect = new Int32Rect(0, 0, width, height);
            var currentPixelBufferSize = width * height * 4;
            this._width = width;
            this._height = height;
            for (var i = 0; i < _bufferInfos.Length; i++)
            {
                var writeBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBuffer);
                GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                    BufferUsageHint.StreamRead);
                _bufferInfos[i] = new BufferInfo
                {
                    BufferSize = currentPixelBufferSize,
                    RepaintRect = repaintRect,
                    GlBufferPointer = writeBuffer
                };
            }
        }

        public void Release()
        {
            if (!allocated)
            {
                return;
            }

            allocated = false;
            for (int i = 0; i < _bufferInfos.Length; i++)
            {
                var writeBufferInfo = _bufferInfos[i];
                var writeBuffer = writeBufferInfo.GlBufferPointer;
                if (writeBuffer != 0)
                {
                    GL.DeleteBuffer(writeBuffer);
                }
            }
        }

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        public void FlushCurrentFrame()
        {
            var writeBufferIndex = _currentWriteBufferIndex % 3;
            var writeBufferInfo = _bufferInfos[writeBufferIndex];
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            writeBufferInfo.HasBuffer = true;
        }

        public void SwapBuffer()
        {
            _currentWriteBufferIndex++;
        }

        public bool TryReadFromBufferInfo(IntPtr ptr, out BufferInfo bufferInfo)
        {
            var readBufferIndex = (_currentWriteBufferIndex - 1) % 3;
            var readBufferInfo = _bufferInfos[readBufferIndex];
            if (!readBufferInfo.HasBuffer)
            {
                bufferInfo = default;
                return false;
            }
            GL.BindBuffer(BufferTarget.PixelPackBuffer, readBufferInfo.GlBufferPointer);
            /*GL.BufferData(BufferTarget.PixelPackBuffer, _readBufferInfo.BufferSize, IntPtr.Zero,
                BufferUsageHint.StreamRead); */ //增加该指令有助于提升性能，但画面会闪烁
            var mapBuffer = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            readBufferInfo.FrameBuffer = mapBuffer;
            var bufferSize = (long)readBufferInfo.BufferSize;
            unsafe
            {
                System.Buffer.MemoryCopy(mapBuffer.ToPointer(), ptr.ToPointer(), bufferSize, bufferSize);
            }

            bufferInfo = readBufferInfo;
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            return true;
        }

        public BufferInfo GetReadBufferInfo()
        {
            var readBufferIndex = (_currentWriteBufferIndex - 1) % 3;
            var readBufferInfo = _bufferInfos[readBufferIndex];
            if (!readBufferInfo.HasBuffer)
            {
                return default;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, readBufferInfo.GlBufferPointer);
            var mapBuffer = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            readBufferInfo.FrameBuffer = mapBuffer;
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            return readBufferInfo;
        }
    }
}