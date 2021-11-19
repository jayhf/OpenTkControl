using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    /// <summary>
    /// highest performance, but possibly cause stuck on low end cpu (2 physical core)
    /// </summary>
    public class MultiStoragePixelBuffer : IFrameBuffer
    {
        public MultiStoragePixelBuffer(uint bufferCount)
        {
            if (bufferCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount));
            }

            _bufferInfos = new BufferInfo[bufferCount];
        }

        public MultiStoragePixelBuffer() : this(5)
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
            foreach (var bufferInfo in _bufferInfos)
            {
                bufferInfo.Fence = IntPtr.Zero;
                bufferInfo.HasBuffer = false;
                var writeBuffer = bufferInfo.GlBufferPointer;
                if (writeBuffer != 0)
                {
                    GL.DeleteBuffer(writeBuffer); //todo: release是否要删除fence?
                }
            }
        }

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        public BufferInfo FlushCurrentFrame()
        {
            // GL.Flush();
            var writeBufferIndex = _currentWriteBufferIndex % 3;
            var writeBufferInfo = _bufferInfos[writeBufferIndex];
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            writeBufferInfo.Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            writeBufferInfo.HasBuffer = true;
            GL.Flush();
            return writeBufferInfo;
        }

        public void SwapBuffer()
        {
            _currentWriteBufferIndex++;
        }

        public bool TryReadFrames(BufferArgs args, out FrameArgs frameArgs)
        {
            var bufferInfo = args.BufferInfo;
            var bufferSize = (long) bufferInfo.BufferSize;
            var fence = bufferInfo.Fence;
            if (fence != IntPtr.Zero)
            {
                try
                {
                    var clientWaitSync = GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                }
                catch (Exception e)
                {
                    Debugger.Break();
                }

                GL.DeleteSync(fence);
                unsafe
                {
                    System.Buffer.MemoryCopy(bufferInfo.ClientIntPtr.ToPointer(), args.HostBufferIntPtr.ToPointer(),
                        bufferSize, bufferSize);
                }

                var int32Rect = bufferInfo.RepaintPixelRect;
                frameArgs = new FrameArgs()
                {
                    RepaintPixelRect = int32Rect,
                };
                return true;
                /*var clientWaitSync = 
                if (clientWaitSync == WaitSyncStatus.AlreadySignaled ||
                    clientWaitSync == WaitSyncStatus.ConditionSatisfied)
                {
                    
                }*/
            }

            frameArgs = null;
            return false;
        }

        public void Dispose()
        {
            this.Release();
        }
    }
}