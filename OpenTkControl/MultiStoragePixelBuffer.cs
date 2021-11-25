using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    /// <summary>
    /// highest performance, but possibly cause stuck on low end cpu (2 physical core)
    /// </summary>
    public class MultiStoragePixelBuffer : IPixelBuffer
    {
        private readonly uint _bufferCount;

        public MultiStoragePixelBuffer(uint bufferCount)
        {
            if (bufferCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount));
            }

            _bufferCount = bufferCount;

            _bufferInfos = new BufferInfo[bufferCount];

        }

        public MultiStoragePixelBuffer() : this(3)
        {
        }

        private readonly BufferInfo[] _bufferInfos;

        private int _width, _height;

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private int _currentWriteBufferIndex = 1;

        private bool _allocated = false;

        const BufferAccessMask AccessMask = BufferAccessMask.MapWriteBit | BufferAccessMask.MapCoherentBit |
                                            BufferAccessMask.MapPersistentBit;

        const BufferStorageFlags StorageFlags = BufferStorageFlags.MapWriteBit |
                                                BufferStorageFlags.MapPersistentBit |
                                                BufferStorageFlags.MapCoherentBit;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Allocate(int width, int height)
        {
            if (_allocated)
            {
                return;
            }
            _allocated = true;
            var repaintRect = new Int32Rect(0, 0, width, height);
            var currentPixelBufferSize = width * height * 4;
            this._width = width;
            this._height = height;


            for (var i = 0; i < _bufferInfos.Length; i++)
            {
                var writeBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBuffer);
                /*GL.BufferData(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero,
                    BufferUsageHint.StreamRead);*/
                GL.BufferStorage(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero, StorageFlags);
                var mapBufferRange = GL.MapBufferRange(BufferTarget.PixelPackBuffer, IntPtr.Zero,
                    currentPixelBufferSize, AccessMask);
                _bufferInfos[i] = new BufferInfo
                {
                    BufferSize = currentPixelBufferSize,
                    RepaintPixelRect = repaintRect,
                    GlBufferPointer = writeBuffer,
                    ClientIntPtr = mapBufferRange,
                    Fence = IntPtr.Zero,
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
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            for (int i = 0; i < _bufferInfos.Length; i++)
            {
                var bufferInfo = _bufferInfos[i];
                bufferInfo.HasBuffer = false;
                var writeBuffer = bufferInfo.GlBufferPointer;
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
            // GL.Flush();
            var writeBufferIndex = _currentWriteBufferIndex % _bufferCount;
            var writeBufferInfo = _bufferInfos[writeBufferIndex];
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            writeBufferInfo.Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            GL.Flush();
            writeBufferInfo.HasBuffer = true;
        }

        public void SwapBuffer()
        {
            _currentWriteBufferIndex++;
        }

        public bool TryReadFromBufferInfo(IntPtr ptr, out BufferInfo bufferInfo)
        {
            var readBufferIndex = (_currentWriteBufferIndex - 1) % 3;
            bufferInfo = _bufferInfos[readBufferIndex];
            if (!bufferInfo.HasBuffer)
            {
                return false;
            }

            var bufferSize = (long)bufferInfo.BufferSize;
            var fence = bufferInfo.Fence;
            if (fence != IntPtr.Zero)
            {
                GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                GL.DeleteSync(fence);
            }

            unsafe
            {
                System.Buffer.MemoryCopy(bufferInfo.ClientIntPtr.ToPointer(), ptr.ToPointer(), bufferSize, bufferSize);
            }

            return true;
        }

        public void Dispose()
        {
            this.Release();

        }
    }
}