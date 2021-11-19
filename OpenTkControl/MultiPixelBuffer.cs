using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    [Obsolete(
        "Cannot surge fps when count of buffers are more than 2, test in  @5820k-gtx970/10700-uhd630/6300u-hd520")]
    public class MultiPixelBuffer : IFrameBuffer
    {
        public MultiPixelBuffer(uint bufferCount)
        {
            if (bufferCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount));
            }

            _bufferInfos = new BufferInfo[bufferCount];
        }

        public MultiPixelBuffer() : this(3)
        {
        }

        /// <summary>
        /// Indicate whether call 'glFlush' before read buffer
        /// <para>Recommend be true, but possibly cause stuck on low end cpu (2 physical core)</para>
        /// </summary>
        public bool EnableFlush { get; set; } = true;

        private readonly BufferInfo[] _bufferInfos;

        private int _width, _height;

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private int _currentWriteBufferIndex = 1;

        private bool _allocated = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixelSize"></param>
        public void Allocate(PixelSize pixelSize)
        {
            if (_allocated)
            {
                return;
            }

            _allocated = true;
            var width = pixelSize.Width;
            var height = pixelSize.Height;
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
                    RepaintPixelRect = repaintRect,
                    GlBufferPointer = writeBuffer
                };
            }
        }

        public void Release()
        {
            if (!_allocated)
            {
                return;
            }

            _allocated = false;
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
        public BufferInfo FlushCurrentFrame()
        {
            var writeBufferIndex = _currentWriteBufferIndex % 3;
            var writeBufferInfo = _bufferInfos[writeBufferIndex];
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            writeBufferInfo.HasBuffer = true;
            if (EnableFlush)
            {
                GL.Flush();
            }

            return writeBufferInfo;
        }

        public void SwapBuffer()
        {
            _currentWriteBufferIndex++;
        }

        public bool TryReadFrames(BufferArgs args, out FrameArgs bufferInfo)
        {
            //todo:
            throw new NotImplementedException();
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
            var bufferSize = (long) readBufferInfo.BufferSize;
            unsafe
            {
                System.Buffer.MemoryCopy(mapBuffer.ToPointer(), IntPtr.Zero.ToPointer(), bufferSize, bufferSize);
            }

            
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            return true;
        }

        public void Dispose()
        {
            Release();
        }
    }
}