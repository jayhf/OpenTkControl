using System;
using System.Threading;

namespace OpenTkWPFHost.Core
{
    public class SemaphoreEvent : IDisposable
    {
        public WaitHandle WaitHandle => _manualResetEvent;

        private readonly ManualResetEvent _manualResetEvent;
        private volatile bool _isWaiting = false;

        public SemaphoreEvent(bool initialState = false)
        {
            _manualResetEvent = new ManualResetEvent(initialState);
        }

        public void Set()
        {
            _manualResetEvent.Set();
        }

        public void Reset()
        {
            _manualResetEvent.Reset();
        }

        public void WaitOne()
        {
            _isWaiting = true;
            _manualResetEvent.WaitOne();
            _isWaiting = false;
        }

        public bool IsWaiting => _isWaiting;

        public void Dispose()
        {
            _manualResetEvent?.Dispose();
        }
    }
}