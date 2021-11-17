using System;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    public class MultiStoragePixelBuffer : IPixelBuffer
    {
        private readonly BufferInfo[] _bufferInfos = new BufferInfo[3];

        private int _width, _height;

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private int _currentWriteBufferIndex = 1;

        private bool _allocated = false;

        const BufferAccessMask mapPersistentBit = BufferAccessMask.MapWriteBit | BufferAccessMask.MapCoherentBit |
                                                  BufferAccessMask.MapPersistentBit;

        const BufferStorageFlags flags = BufferStorageFlags.MapWriteBit |
                                         BufferStorageFlags.MapPersistentBit |
                                         BufferStorageFlags.MapCoherentBit;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Allocate(int width, int height)
        {
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
                GL.BufferStorage(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero, flags);
                var mapBufferRange = GL.MapBufferRange(BufferTarget.PixelPackBuffer, IntPtr.Zero,
                    currentPixelBufferSize, mapPersistentBit);
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
            var writeBufferIndex = _currentWriteBufferIndex % 3;
            var writeBufferInfo = _bufferInfos[writeBufferIndex];
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            /*var fence = writeBufferInfo.Fence;
            if (fence != IntPtr.Zero)
            {
                GL.DeleteSync(fence);
            }*/

            writeBufferInfo.Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
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

            var bufferSize = (long) bufferInfo.BufferSize;
            var fence = bufferInfo.Fence;
            if (fence != IntPtr.Zero)
            {
                GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                /*while (true)
                {
                    var clientWaitSync = GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 1);
                    if (clientWaitSync == WaitSyncStatus.AlreadySignaled ||
                        clientWaitSync == WaitSyncStatus.ConditionSatisfied)
                    {
                        break;
                    }
                }*/
                GL.DeleteSync(fence);
            }

            unsafe
            {
                System.Buffer.MemoryCopy(bufferInfo.ClientIntPtr.ToPointer(), ptr.ToPointer(), bufferSize, bufferSize);
            }


            /*GL.BindBuffer(BufferTarget.PixelPackBuffer, bufferInfo.GlBufferPointer);
            var mapBuffer = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            var bufferSize = (long) bufferInfo.BufferSize;
            unsafe
            {
                System.Buffer.MemoryCopy(mapBuffer.ToPointer(), ptr.ToPointer(), bufferSize, bufferSize);
            }

            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);*/
            return true;
        }
    }
}