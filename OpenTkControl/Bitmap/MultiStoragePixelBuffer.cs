using System;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    public enum GLSignalStatus : int
    {
        Signaled = 0x9119,
        UnSignaled = 0x9118,
    }

    /// <summary>
    /// highest performance, but possibly cause stuck on low end cpu (2 physical core)
    /// </summary>
    public class MultiStoragePixelBuffer : IRenderBuffer, IDisposable
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
        private long _currentWriteBufferIndex = 0;

        private bool _allocated = false;


        private RenderTargetInfo _renderTargetInfo;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="renderTargetInfo"></param>
        public void Allocate(RenderTargetInfo renderTargetInfo)
        {
            if (_allocated)
            {
                return;
            }

            _allocated = true;
            _renderTargetInfo = renderTargetInfo;
            var currentPixelBufferSize = renderTargetInfo.BufferSize;
            var pixelSize = renderTargetInfo.PixelSize;
            this._width = renderTargetInfo.PixelWidth;
            this._height = renderTargetInfo.PixelHeight;
            foreach (var bufferInfo in _bufferInfos)
            {
                bufferInfo.Allocate(currentPixelBufferSize, pixelSize);
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
            GetAllLocks();
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            foreach (var bufferInfo in _bufferInfos)
            {
                try
                {
                    bufferInfo.Release();
                }
                finally
                {
                    bufferInfo.ReleaseWriteLock();
                }
            }
        }

        private void GetAllLocks()
        {
            foreach (var pixelBufferInfo in _bufferInfos)
            {
                pixelBufferInfo.AcquireWriteLock();
            }
        }

        private void ReleaseLocks()
        {
            foreach (var pixelBufferInfo in _bufferInfos)
            {
                pixelBufferInfo.ReleaseWriteLock();
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
            GL.Finish();
            _writePixelBufferInfo.HasBuffer = true;
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
            if (bufferInfo.ReadBuffer())
            {
                return new BitmapFrameArgs(args.TargetInfo, bufferInfo);
            }

            return null;
        }

        public void Dispose()
        {
            foreach (var pixelBufferInfo in _bufferInfos)
            {
                pixelBufferInfo.Dispose();
            }
        }
    }
}