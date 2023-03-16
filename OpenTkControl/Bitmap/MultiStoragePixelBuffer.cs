using System;
using System.Diagnostics;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Bitmap
{
    /// <summary>
    /// highest performance, but possibly cause stuck on low end cpu (2 physical core)
    /// </summary>
    public class MultiStoragePixelBuffer : IDisposable
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
        public PixelBufferInfo ReadPixelAndSwap()
        {
            var writeBufferIndex = _currentWriteBufferIndex % _bufferCount;
            var writePixelBufferInfo = _bufferInfos[writeBufferIndex];
            while (writePixelBufferInfo.HasBuffer)
            {
                _spinWait.SpinOnce();
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, writePixelBufferInfo.GlBufferPointer);
            GL.ReadPixels(0, 0, _width, _height, PixelFormat.Bgra, PixelType.UnsignedByte,
                IntPtr.Zero);
            writePixelBufferInfo.Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            GL.Finish();
            writePixelBufferInfo.HasBuffer = true;
            _currentWriteBufferIndex++;
            return writePixelBufferInfo;
        }


        public BitmapFrameArgs ReadFrames(BitmapRenderArgs args)
        {
            if (args == null)
            {
                return null;
            }

            var bufferInfo = args.BufferInfo;
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