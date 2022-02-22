using System;
using System.Threading;

namespace OpenTkWPFHost.Core
{
    public class EventWaiter : StatefulWaiter, IDisposable
    {
        protected volatile int Status = Idle;

        public const int Waiting = 1;

        public const int Idle = 0;

        public override bool IsWaiting => Status == Waiting;

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        public override void WaitInfinity()
        {
            if (Interlocked.CompareExchange(ref Status, Waiting, Idle) == Idle)
            {
                _autoResetEvent.WaitOne();
                // _autoResetEvent.Reset();
                Interlocked.Exchange(ref Status, Idle);
            }
        }

        public override void TrySet()
        {
            if (Interlocked.CompareExchange(ref Status, Idle, Waiting) == Waiting)
            {
                _autoResetEvent.Set();
            }
        }

        public override void ForceSet()
        {
            _autoResetEvent.Set();
        }


        public void Dispose()
        {
            _autoResetEvent?.Dispose();
        }
    }
}