using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenTkWPFHost.Core
{
    public class GenericMultiBuffer<T> : IEnumerable<T>
    {
        private readonly int _size;

        private readonly IList<T> _multiBuffer;

        public GenericMultiBuffer(int size = 3)
        {
            _size = size;
            _multiBuffer = new T[size];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factorFunc">int: index, T1: exist value</param>
        public void Allocate(Func<int, T, T> factorFunc)
        {
            if (factorFunc == null)
            {
                throw new ArgumentNullException(nameof(factorFunc));
            }

            for (int i = 0; i < _size; i++)
            {
                _multiBuffer[i] = factorFunc.Invoke(i, _multiBuffer[i]);
            }
        }

        public void ForEach(Action<int, T> action)
        {
            for (int i = 0; i < _size; i++)
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
            _currentWriteBufferIndex++;
            var writeBufferIndex = (int) _currentWriteBufferIndex % _size;
            _writeBuffer = _multiBuffer[writeBufferIndex];
            _readBuffer = _multiBuffer[writeBufferIndex - 1];
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