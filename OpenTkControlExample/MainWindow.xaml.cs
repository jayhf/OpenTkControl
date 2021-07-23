using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using OpenTkControl;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics;
using TestRenderer;



namespace OpenTkControlExample
{
    public partial class MainWindow
    {
        private const int LineCount = 10;
        public const int PointsCount = 10000;

        public const int LineLength = PointsCount * 2;

        private TendencyChartRenderer renderer = new TendencyChartRenderer();

        public MainWindow()
        {
            this.InitializeComponent();

            var dateTime = DateTime.Now;
            var start = dateTime.Ticks;
            var random = new Random();
            for (int i = 0; i < LineCount; i++)
            {
                var lineChartRenderer = new LineRenderer(PointsCount) {LineColor = Color4.White};
                var ringBuffer = lineChartRenderer.RingBuffer;
                for (int j = 0; j < LineLength; j += 2)
                {
                    var time = dateTime.AddSeconds(j);
                    var ticks = (float) (time.Ticks - start);
                    ringBuffer[j] = ticks;
                    ringBuffer[j + 1] = random.Next(0, 10000) * 0.1f;
                }

                renderer.Add(lineChartRenderer);
            }

            var end = (long) renderer.LineRenderers.First().RingBuffer[LineLength - 2];
            Slider.Maximum = end;
            Slider.Value = end;
            renderer.CurrentScrollRange = new ScrollRange(0, end);
            renderer.CurrentYAxisValue = 1000;
            renderer.BackgroundColor = Color4.Black;
            this.OpenTkControl.Renderer = new GLDXProcedure(new GLSettings())
            {
                Renderer = renderer,
            };
        }


        private void OpenTkControl_OnExceptionOccurred(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.ExceptionObject);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            this.OpenTkControl.Renderer = null;
            foreach (var historySource in OpenTkControl.HistorySources)
            {
                Box.Items.Add(historySource);
            }
        }

        private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            renderer.CurrentScrollRange = new ScrollRange(0, (long) e.NewValue);
        }
    }
}