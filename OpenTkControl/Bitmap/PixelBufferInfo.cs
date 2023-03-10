using System;
using System.Diagnostics;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost.Core;
using Buffer = System.Buffer;

namespace OpenTkWPFHost.Bitmap
{
    public class PixelBufferInfo : IDisposable
    {
        public PixelSize PixelSize;

        public int BufferSize;

        public volatile int GlBufferPointer;

        public volatile bool HasBuffer;

        public volatile IntPtr Fence;

        public IntPtr MapBufferIntPtr;

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        private const BufferAccessMask AccessMask = BufferAccessMask.MapWriteBit | BufferAccessMask.MapCoherentBit |
                                                    BufferAccessMask.MapPersistentBit;

        private const BufferStorageFlags StorageFlags = BufferStorageFlags.MapWriteBit |
                                                        BufferStorageFlags.MapPersistentBit |
                                                        BufferStorageFlags.MapCoherentBit;

        public void AcquireWriteLock()
        {
            _lockSlim.EnterWriteLock();
        }

        public void ReleaseWriteLock()
        {
            _lockSlim.ExitWriteLock();
        }

        // private byte[] bufferBytes;

        public void Allocate(int pixelBufferSize, PixelSize pixelSize)
        {
            var writeBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelPackBuffer, writeBuffer);
            GL.BufferStorage(BufferTarget.PixelPackBuffer, pixelBufferSize, IntPtr.Zero, StorageFlags);
            var mapBufferRange = GL.MapBufferRange(BufferTarget.PixelPackBuffer, IntPtr.Zero,
                pixelBufferSize, AccessMask);
            // bufferBytes = new byte[pixelBufferSize];
            this.BufferSize = pixelBufferSize;
            this.GlBufferPointer = writeBuffer;
            this.MapBufferIntPtr = mapBufferRange;
            this.PixelSize = pixelSize;
        }

        public bool ReadBuffer()
        {
            try
            {
                _lockSlim.EnterWriteLock();
                var fence = Fence;
                if (this.HasBuffer)
                {
                    var clientWaitSync = GL.ClientWaitSync(fence, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                    if (clientWaitSync == WaitSyncStatus.AlreadySignaled ||
                        clientWaitSync == WaitSyncStatus.ConditionSatisfied)
                    {
                        this.Fence = IntPtr.Zero;
                        GL.DeleteSync(fence);
                        return true;
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

                return false;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public bool CopyTo(IntPtr destination)
        {
            try
            {
                _lockSlim.EnterReadLock();
                if (this.HasBuffer)
                {
                    var bufferSize = this.BufferSize;
                    unsafe
                    {
                        Buffer.MemoryCopy(this.MapBufferIntPtr.ToPointer(),
                            destination.ToPointer(),
                            bufferSize, bufferSize);
                    }

                    return true;
                }

                return false;
            }
            finally
            {
                this.HasBuffer = false;
                _lockSlim.ExitReadLock();
            }
        }

        public void Release()
        {
            this.HasBuffer = false;
            var intPtr = this.Fence;
            if (intPtr.Equals(IntPtr.Zero))
            {
                GL.DeleteSync(intPtr);
            }

            var writeBuffer = this.GlBufferPointer;
            if (writeBuffer != 0)
            {
                GL.DeleteBuffer(writeBuffer); //todo: release是否要删除fence?
            }
        }

        public void Dispose()
        {
            _lockSlim.Dispose();
        }
    }
}