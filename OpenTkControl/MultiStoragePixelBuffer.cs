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
    public enum GLSignalStatus : int
    {
        Signaled = 0x9119,
        UnSignaled = 0x9118,
    }

    /// <summary>
    /// highest performance, but possibly cause stuck on low end cpu (2 physical core)
    /// </summary>
    public class MultiStoragePixelBuffer : IRenderBuffer
    {
        private readonly uint _bufferCount;

        public MultiStoragePixelBuffer(uint bufferCount)
        {
            if (bufferCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount));
            }

            _bufferCount = bufferCount;

            _bufferInfos = new PixelBufferInfo[bufferCount];
            for (int i = 0; i < bufferCount; i++)
            {
                _bufferInfos[i] = new PixelBufferInfo();
            }
        }

        public MultiStoragePixelBuffer() : this(3)
        {
        }

        private readonly PixelBufferInfo[] _bufferInfos;

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

        private CanvasInfo _canvasInfo;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canvasInfo"></param>
        public void Allocate(CanvasInfo canvasInfo)
        {
            if (_allocated)
            {
                return;
            }

            _allocated = true;
            _canvasInfo = canvasInfo;
            var currentPixelBufferSize = canvasInfo.BufferSize;
            this._width = canvasInfo.PixelWidth;
            this._height = canvasInfo.PixelHeight;
            foreach (var bufferInfo in _bufferInfos)
            {
                var writeBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBuffer);
                GL.BufferStorage(BufferTarget.PixelPackBuffer, currentPixelBufferSize, IntPtr.Zero, StorageFlags);
                var mapBufferRange = GL.MapBufferRange(BufferTarget.PixelPackBuffer, IntPtr.Zero,
                    currentPixelBufferSize, AccessMask);
                bufferInfo.BufferSize = currentPixelBufferSize;
                bufferInfo.GlBufferPointer = writeBuffer;
                bufferInfo.MapBufferIntPtr = mapBufferRange;
                bufferInfo.PixelSize = canvasInfo.PixelSize;
            }

            this.Swap();
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
        public PixelBufferInfo ReadPixel()
        {
            while (_writePixelBufferInfo.HasBuffer)
            {
                _spinWait.SpinOnce();
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _writePixelBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            _writePixelBufferInfo.Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            _writePixelBufferInfo.HasBuffer = true;
            GL.Finish();
            return _writePixelBufferInfo;
        }

        private PixelBufferInfo _writePixelBufferInfo;

        public void Swap()
        {
            _currentWriteBufferIndex++;
            var writeBufferIndex = _currentWriteBufferIndex % _bufferCount;
            _writePixelBufferInfo = _bufferInfos[writeBufferIndex];
        }

        public FrameArgs ReadFrames(RenderArgs args)
        {
            if (args == null)
            {
                return null;
            }

            var bufferInfo = ((BitmapRenderArgs) args).BufferInfo;
            var fence = bufferInfo.Fence;
            if (fence != IntPtr.Zero && bufferInfo.HasBuffer)
            {
                var clientWaitSync = GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                if (clientWaitSync == WaitSyncStatus.AlreadySignaled ||
                    clientWaitSync == WaitSyncStatus.ConditionSatisfied)
                {
                    GL.DeleteSync(fence);
                    return new BitmapFrameArgs()
                    {
                        PixelSize = args.PixelSize,
                        BufferInfo = bufferInfo,
                        CanvasInfo = this._canvasInfo,
                    };
                }
#if DEBUG
                GL.GetSync(fence, SyncParameterName.SyncStatus, 1, out int length, out int status);
                if (status == (int) GLSignalStatus.UnSignaled)
                {
                    var errorCode = GL.GetError();
                    Debug.WriteLine(errorCode.ToString());
                }

                Debug.WriteLine(clientWaitSync.ToString());
#endif
            }

            bufferInfo.HasBuffer = false;
            return null;
        }
    }
}