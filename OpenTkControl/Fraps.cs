using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace OpenTkControl
{
    public class Fraps
    {
        public int FPS { get; private set; }

        private Timer timer;

        private volatile int framecounts = 0;

        private Typeface mFpsTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold,
            FontStretches.Normal);

        private SolidColorBrush brush = new SolidColorBrush(Colors.DeepSkyBlue);

        public void Increment()
        {
            Interlocked.Increment(ref framecounts);
        }

        public void Start()
        {
            timer = new Timer((state =>
            {
                FPS = framecounts;
                framecounts = 0;
            }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void DrawFps(DrawingContext drawingContext, Point point)
        {
            drawingContext.DrawText(new FormattedText($"fps:{FPS}", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    mFpsTypeface, 26, brush, 1),
                point);
        }
    }
}