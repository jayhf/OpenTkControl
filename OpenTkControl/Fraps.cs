using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    public class Fraps : IDisposable
    {
        public string Name { get; set; }

        public int Fps { get; private set; }

        private Timer _timer;

        private volatile int _frameCount = 0;

        private SolidColorBrush _brush = new SolidColorBrush(Colors.DeepSkyBlue);

        private Typeface _fpsTypeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal,
            FontWeights.Black,
            FontStretches.Normal);

        private CultureInfo _cultureInfo = CultureInfo.CurrentCulture;

        public CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set => _cultureInfo = value;
        }

        public Typeface FpsTypeface
        {
            get => _fpsTypeface;
            set
            {
                if (value != null && !Equals(value, _fpsTypeface))
                {
                    _fpsTypeface = value;
                }
            }
        }

        public SolidColorBrush Brush
        {
            get => _brush;
            set
            {
                if (value != null && value != _brush)
                {
                    _brush = value;
                }
            }
        }

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
            drawingContext.DrawText(new FormattedText($"{Name} :{Fps}", _cultureInfo,
                    FlowDirection.LeftToRight,
                    _fpsTypeface, 21, _brush, 1),
                point);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}