using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace OpenTkWPFHost.Core
{
    public class FpsCounter : IDisposable
    {
        public string Title { get; set; }

        public int Fps { get; private set; }

        private readonly Timer _timer;

        private volatile int _frameCount = 0;

        private Brush _brush;

        private Typeface _fpsTypeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal,
            FontWeights.Medium,
            FontStretches.Normal);

        private CultureInfo _cultureInfo = CultureInfo.CurrentCulture;
        private volatile int _emSize = 18;

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

        public int EmSize
        {
            get => _emSize;
            set => _emSize = value;
        }

        public Brush Brush
        {
            get => _brush;
            set
            {
                this._brush = value.Clone();
                _brush.Freeze();
            }
        }

        public void Increment()
        {
            Interlocked.Increment(ref _frameCount);
        }

        public FpsCounter(Brush brush, Action<FpsCounter> onTick)
        {
            this.Brush = brush;
            if (onTick == null)
            {
                _timer = new Timer((state =>
                {
                    Fps = _frameCount;
                    _frameCount = 0;
                }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
            else
            {
                _timer = new Timer((state =>
                {
                    Fps = _frameCount;
                    _frameCount = 0;
                    onTick(this);
                }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }

        public FpsCounter() : this(Brushes.Black, null)
        {
        }

        public double DrawFps(DrawingContext drawingContext, Point point)
        {
            var formattedText = new FormattedText($"{Title} :{Fps}", _cultureInfo,
                FlowDirection.LeftToRight,
                _fpsTypeface, _emSize, _brush, 1);
            
            drawingContext.DrawText(formattedText, point);
            return formattedText.Height;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}