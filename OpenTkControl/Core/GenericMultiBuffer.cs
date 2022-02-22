using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenTkWPFHost.Core
{
    public class GenericMultiBuffer<T> : IEnumerable<T>
    {
        private readonly int _bufferCount;

        public IReadOnlyCollection<T> MultiBuffer => new ReadOnlyCollection<T>(_multiBuffer);

        private readonly IList<T> _multiBuffer;

        public GenericMultiBuffer(int bufferCount = 3, Func<int, T, T> factorFunc = null)
        {
            if (bufferCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount));
            }

            _bufferCount = bufferCount;
            _multiBuffer = new T[bufferCount];
            if (factorFunc != null)
            {
                Instantiate(factorFunc);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factorFunc">int: index, T1: exist value</param>
        public void Instantiate(Func<int, T, T> factorFunc)
        {
            if (factorFunc == null)
            {
                throw new ArgumentNullException(nameof(factorFunc));
            }

            for (int i = 0; i < _bufferCount; i++)
            {
                _multiBuffer[i] = factorFunc.Invoke(i, _multiBuffer[i]);
            }

            _writeBuffer = _multiBuffer[0];
        }

        public void ForEach(Action<int, T> action)
        {
            for (int i = 0; i < _bufferCount; i++)
            {
                action.Invoke(i, _multiBuffer[i]);
            }
        }

        /// <summary>
        /// 先写入缓冲，然后才能读取，所以写入缓冲=读取缓冲+1
        /// </summary>
        private long _currentWriteBufferIndex = 0;

        private T _writeBuffer, _readBuffer;

        public void Swap()
        {
            _readBuffer = _writeBuffer;
            _currentWriteBufferIndex++;
            var writeBufferIndex = (int)_currentWriteBufferIndex % _bufferCount;
            _writeBuffer = _multiBuffer[writeBufferIndex];
        }

        public T GetFrontBuffer()
        {
            return _readBuffer;
        }

        public T GetBackBuffer()
        {
            return _writeBuffer;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _multiBuffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}