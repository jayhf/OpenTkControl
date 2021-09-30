using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using OpenTK.Graphics;
using OpenTkWPFHost;
using TestRenderer;
using Point = System.Drawing.Point;
using Vector = System.Numerics.Vector;


namespace OpenTkControlExample
{
    public partial class MainWindow
    {
        private const int LineCount = 1;
        public const int PointsCount = 1000;

        public const int LineLength = PointsCount * 2;

        private const long MaxYAxis = (long) ((1000 + LineLength) * 0.1);

        private Color4 _lineColor = Color4.White;

        private readonly TendencyChartRenderer _renderer = new TendencyChartRenderer();

        public MainWindow()
        {
            this.InitializeComponent();
            var random = new Random();
            for (int i = 0; i < LineCount; i++)
            {
                var lineChartRenderer = new LineRenderer(PointsCount) {LineColor = _lineColor};
                var ringBuffer = lineChartRenderer.RingBuffer;
                for (int j = 0; j < LineLength; j += 2)
                {
                    ringBuffer[j] = j;
                    var next = random.Next(j, 1000 + j) * 0.1f;
                    ringBuffer[j + 1] = next;
                }

                _renderer.Add(lineChartRenderer);
            }

            Slider.Maximum = LineLength;
            Slider.Value = LineLength;
            _renderer.CurrentScrollRange = new ScrollRange(0, LineLength);
            _renderer.CurrentYAxisValue = MaxYAxis;
            _renderer.BackgroundColor = Color4.Black;
            this.OpenTkControl.Renderer = new DXProcedure(new GLSettings())
            {
                Renderer = _renderer,
            };
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            this.OpenTkControl.Start(this);
        }


        public void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _renderer.CurrentScrollRange = new ScrollRange(0, (long) e.NewValue);
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            this.OpenTkControl.Close();
        }

        private async void Test_OnClick(object sender, RoutedEventArgs e)
        {
            var argb = _lineColor.ToArgb();
            var currentYAxisValue = _renderer.CurrentYAxisValue;
            var bitmapSource = await OpenTkControl.PushRenderTask(
                (procedure => { _renderer.CurrentYAxisValue = MaxYAxis; }),
                (procedure => { _renderer.CurrentYAxisValue = currentYAxisValue; }));

            int bytesPerPixel = ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            int stride = bitmapSource.PixelWidth * bytesPerPixel;
            /*var writeableBitmap = new WriteableBitmap(bitmapSource);
            var lengthx = 10 * stride / 4;
            int[] arrayInts = new int[lengthx];
            for (int i = 0; i < lengthx; i++)
            {
                arrayInts[i] = argb;
            }

            writeableBitmap.WritePixels(new Int32Rect(0, 0, stride / 4, 10), arrayInts, stride, 0);*/
            /*using (var fileStream = new FileStream(@"C:\迅雷下载\x.png", FileMode.OpenOrCreate))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                encoder.Save(fileStream);
            }

            return;*/

            // int stride = bitmapSource.PixelWidth * bytesPerPixel;
            int bufferStride = stride / 4;
            var pixelHeight = bitmapSource.PixelHeight;
            var pixels = new int[pixelHeight * bufferStride];
            bitmapSource.CopyPixels(pixels, stride, 0);
            int pixelLine = 0;
            for (int i = 0; i < pixelHeight; i++)
            {
                for (int j = 0; j < bufferStride; j++)
                {
                    var b = pixels[j + i * bufferStride];
                    if (b == argb)
                    {
                        pixelLine = i;
                        break;
                    }
                }
            }

            if (pixelLine == 0)
            {
                return;
            }

            var height = ((double) (pixelHeight - pixelLine)) / (double) pixelHeight * MaxYAxis + 0;
            _renderer.CurrentYAxisValue = (long) height;
        }

        private void FindTop()
        {
            
        }
    }
}