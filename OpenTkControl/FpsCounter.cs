using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    public class FpsCounter : IDisposable
    {
        public string Title { get; set; }

        public int Fps { get; private set; }

        private Timer _timer;

        private volatile int _frameCount = 0;

        private Color _brushColor;

        private SolidColorBrush brush;

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

        public Color Color
        {
            get => _brushColor;
            set
            {
                if (value == this._brushColor)
                {
                    return;
                }

                _brushColor = value;
                brush = new SolidColorBrush(value);
                brush.Freeze();
            }
        }

        public void Increment()
        {
            Interlocked.Increment(ref _frameCount);
        }

        public FpsCounter()
        {
            this.Color = Colors.MediumPurple;
            _timer = new Timer((state =>
            {
                Fps = _frameCount;
                _frameCount = 0;
            }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void DrawFps(DrawingContext drawingContext, Point point)
        {
            var formattedText = new FormattedText($"{Title} :{Fps}", _cultureInfo,
                FlowDirection.LeftToRight,
                _fpsTypeface, 21, brush, 1);
            drawingContext.DrawText(formattedText, point);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}