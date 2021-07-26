using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace OpenTkControl
{
    public class Fraps : IDisposable
    {
        public string Name { get; set; }

        public int Fps { get; private set; }

        private Timer _timer;

        private volatile int _frameCount = 0;

        public Typeface FpsTypeface { get; } = new Typeface(new FontFamily("Arial"), FontStyles.Normal,
            FontWeights.Black,
            FontStretches.Normal);

        public SolidColorBrush Brush { get; } = new SolidColorBrush(Colors.DeepSkyBlue);

        public void Increment()
        {
            Interlocked.Increment(ref _frameCount);
        }

        public void Start()
        {
            _timer = new Timer((state =>
            {
                Fps = _frameCount;
                _frameCount = 0;
            }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void DrawFps(DrawingContext drawingContext, Point point)
        {
            drawingContext.DrawText(new FormattedText($"{Name} fps:{Fps}", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    FpsTypeface, 21, Brush, 1),
                point);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}