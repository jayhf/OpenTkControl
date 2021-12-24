namespace OpenTkWPFHost.Core
{
    public abstract class StatefulWaiter
    {
        public abstract bool IsWaiting { get; }

        public abstract void WaitInfinity();

        public abstract void TrySet();

        public abstract void ForceSet();
    }
}