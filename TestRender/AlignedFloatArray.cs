using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TestRenderer
{
    /// <summary>
    /// source code from internet
    /// </summary>
    public class AlignedFloatArray : IDisposable
    {
        private byte[] _buffer;
        private GCHandle _bufferHandle;

        private IntPtr _bufferPointer;
        private readonly int _length;

        public AlignedFloatArray(int length, int byteAlignment)
        {
            this._length = length;
            _buffer = new byte[length * sizeof(float) + byteAlignment];
            _bufferHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            long ptr = _bufferHandle.AddrOfPinnedObject().ToInt64();
            // round up ptr to nearest 'byteAlignment' boundary
            ptr = (ptr + byteAlignment - 1) & ~(byteAlignment - 1);
            _bufferPointer = new IntPtr(ptr);
        }

        private bool isDisposed;

        ~AlignedFloatArray()
        {
            if (!isDisposed)
            {
                Dispose(false);
            }
        }

        public virtual void Dispose(bool disposing)
        {
            if (_bufferHandle.IsAllocated)
            {
                _bufferHandle.Free();
                _buffer = null;
            }

            GC.SuppressFinalize(this);
            isDisposed = true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public float this[int index]
        {
            get
            {
                unsafe
                {
                    return GetPointer()[index];
                }
            }
            set
            {
                unsafe
                {
                    GetPointer()[index] = value;
                }
            }
        }

        public int Length
        {
            get { return _length; }
        }

        public void Write(int index, float[] src, int srcIndex, int count)
        {
            if (index < 0 || index >= _length) throw new IndexOutOfRangeException();

            if ((index + count) > _length) count = Math.Max(0, _length - index);

            System.Runtime.InteropServices.Marshal.Copy(
                src,
                srcIndex,
                new IntPtr(_bufferPointer.ToInt64() + index * sizeof(float)),
                count);
        }

        public void Read(int index, float[] dest, int dstIndex, int count)
        {
            if (index < 0 || index >= _length) throw new IndexOutOfRangeException();
            if ((index + count) > _length) count = Math.Max(0, _length - index);
            Marshal.Copy(
                new IntPtr(_bufferPointer.ToInt64() + index * sizeof(float)),
                dest,
                dstIndex,
                count);
        }

        public float[] GetManagedArray()
        {
            return GetManagedArray(0, _length);
        }

        public float[] GetManagedArray(int index, int count)
        {
            float[] result = new float[count];

            Read(index, result, 0, count);
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            for (int t = 0; t < _length; t++)
            {
                sb.Append(this[t].ToString());

                if (t < (_length - 1)) sb.Append(',');
            }

            sb.Append(']');
            return sb.ToString();
        }

        public unsafe float* GetPointer(int index)
        {
            return GetPointer() + index;
        }

        public unsafe float* GetPointer()
        {
            return ((float*) _bufferPointer.ToPointer());
        }
    }
}