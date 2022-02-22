using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TestRenderer
{
    /// <summary>
    /// thread-safe,vector instruction interoperable and flexible ring render ring array
    /// </summary>
    public class RenderRingBuffer : IEnumerable<float>, IDisposable
    {
        public const int AVXAlignment = 32;

        public const int SSEAlignment = 16;

        private ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

        private readonly AlignedFloatArray _buffer;

        /// <summary>
        /// 从设计上来说不太可能到尽头
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        /// 当前头指针与尾指针
        /// </summary>
        private int _tail, _head = 0;

        public RenderRingBuffer(int length, int alignment = AVXAlignment)
        {
            _capacity = length;
            _buffer = new AlignedFloatArray(length, alignment);
        }

        /// <summary>
        /// 当前头指针与尾指针
        /// </summary>
        public int Tail => _tail;

        /// <summary>
        /// 当前头指针与尾指针
        /// </summary>
        public int Head => _head;

        public AlignedFloatArray Buffer => _buffer;

        public void Append(float item)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                if (Tail >= _capacity)
                {
                    _tail = 0;
                    _head = Head + 1;
                }
                else if (Head > 0)
                {
                    _head = Head + 1;
                }
                Buffer[Tail] = item;
                _tail = Tail + 1;
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public IEnumerator<float> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public float this[int index]
        {
            get
            {
                if (index > _capacity || index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                float result;
                readerWriterLockSlim.EnterReadLock();
                try
                {
                    var arrayIndex = Head + index;
                    arrayIndex %= _capacity;
                    result = Buffer[arrayIndex];
                }
                finally
                {
                    readerWriterLockSlim.ExitReadLock();
                }

                return result;
            }
            set
            {
                if (index > _capacity || index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                readerWriterLockSlim.EnterWriteLock();

                try
                {
                    var arrayIndex = Head + index;
                    arrayIndex %= _capacity;
                    Buffer[arrayIndex] = value;
                }
                finally
                {
                    readerWriterLockSlim.ExitWriteLock();
                }
            }
        }

        public void Dispose()
        {
            readerWriterLockSlim?.Dispose();
            Buffer?.Dispose();
        }

        public override string ToString()
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                var stringBuilder = new StringBuilder();
                for (int i = 0; i < _capacity; i++)
                {
                    var arrayIndex = Head + i;
                    arrayIndex %= _capacity;
                    var f = Buffer[arrayIndex];
                    stringBuilder.Append(f);
                    stringBuilder.Append(',');
                }

                return stringBuilder.ToString();
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }
        }
    }
}