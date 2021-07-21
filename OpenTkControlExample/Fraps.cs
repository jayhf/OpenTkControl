using System;
using System.Threading;

namespace OpenTkControlExample
{
    public class Fraps : IDisposable
    {
        private Timer timer;
        private volatile int currentFrameCount, fps;

        public Fraps()
        {
        }

        public void Start()
        {
            timer = new Timer((state =>
            {
                fps = currentFrameCount;
                currentFrameCount = 0;
            }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public int FPS => fps;

        public void Increase()
        {
            Interlocked.Increment(ref currentFrameCount);
        }


        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}