using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using OpenTK.Graphics.OpenGL4;
using ArbSync = OpenTK.Graphics.OpenGL.ArbSync;
using Buffer = OpenTK.Graphics.OpenGL4.Buffer;

namespace OpenTkWPFHost
{
    /// <summary>
    /// highest performance, but possibly cause stuck on low end cpu (2 physical core)
    /// </summary>
    public class MultiStoragePixelBuffer : IFrameBuffer
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
        private int _currentWriteBufferIndex = 0;

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
                    GlBufferPointer = writeBuffer,
                    MapBufferIntPtr = mapBufferRange,
                    Fence = IntPtr.Zero,
                    PixelSize = pixelSize,
                };
            }

            _writeBufferInfo = _bufferInfos[_currentWriteBufferIndex];
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

        private SpinWait _spinWait = new SpinWait();

        /// <summary>
        /// write current frame to buffer
        /// </summary>
        public BufferInfo FlushAsync()
        {
            while (_writeBufferInfo.HasBuffer)
            {
                _spinWait.SpinOnce();
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _writeBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            _writeBufferInfo.Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            _writeBufferInfo.HasBuffer = true;
            GL.Flush();
            return _writeBufferInfo;
        }

        private BufferInfo _writeBufferInfo;

        public void SwapBuffer()
        {
            _currentWriteBufferIndex++;
            var writeBufferIndex = _currentWriteBufferIndex % _bufferCount;
            _writeBufferInfo = _bufferInfos[writeBufferIndex];
        }

        public FrameArgs ReadFrames(RenderArgs args)
        {
            if (args == null)
            {
                return null;
            }

            var bufferInfo = ((BitmapRenderArgs) args).BufferInfo;
            var fence = bufferInfo.Fence;
            if (fence != IntPtr.Zero)
            {
                // GL.GetSync(fence, SyncParameterName.SyncStatus, 1, out int length, out int status);

                // GL.GetSync(fence,SyncParameterName.SyncStatus,sizeof(IntPtr));
                // GL.WaitSync(fence, WaitSyncFlags.None, 0);
                var clientWaitSync = GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                if (clientWaitSync == WaitSyncStatus.AlreadySignaled ||
                    clientWaitSync == WaitSyncStatus.ConditionSatisfied)
                {
                    GL.DeleteSync(fence);
                    return new BitmapFrameArgs()
                    {
                        PixelSize = args.PixelSize,
                        BufferInfo = bufferInfo,
                    };
                }

                var errorCode = GL.GetError();
                Debug.WriteLine(errorCode.ToString());
                // Debugger.Break();
                /*
                if (errorCode != ErrorCode.NoError)
                {
                    throw new Exception(errorCode.ToString());
                }*/
            }

            bufferInfo.HasBuffer = false;
            return null;
        }

        public void Dispose()
        {
            this.Release();
        }
    }
}