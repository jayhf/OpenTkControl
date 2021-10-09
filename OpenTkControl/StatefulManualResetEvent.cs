using System;
using System.Threading;

namespace OpenTkWPFHost
{
    public class StatefulManualResetEvent : IDisposable
    {
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        private volatile int _isWaiting = IdleStatus;

        private const int WaitingStatus = 1;

        private const int IdleStatus = 0;

        public bool IsWaiting => _isWaiting == WaitingStatus;

        public void WaitInfinity()
        {
            if (Interlocked.CompareExchange(ref _isWaiting, WaitingStatus, IdleStatus) == IdleStatus)
            {
                _manualResetEvent.WaitOne();
                _manualResetEvent.Reset();
                Interlocked.Exchange(ref _isWaiting, IdleStatus);
            }
        }

        public void TrySet()
        {
            if (Interlocked.CompareExchange(ref _isWaiting, IdleStatus, WaitingStatus) == WaitingStatus)
            {
                _manualResetEvent.Set();
            }
        }

        public void ForceSet()
        {
            _manualResetEvent.Set();
        }


        public void Dispose()
        {
            _manualResetEvent?.Dispose();
        }
    }
}