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

        private int lineColorINT = Color4.White.ToArgb();

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
            _renderer.ScrollRangeChanged = false;
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


        public async void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            /*1. 插帧检查，在每次更改后都会输出一个用户不可见的测试帧，检查实际的上下界限，一个变种的是先计算上下界限才设置实际渲染位置，会产生明显的拖动延迟
              2. 带阈值的插帧检查，给上下界限一个阈值，实时检查输出图像的上下限，超过阈值才会启用变换，而且如果是上下限缩小可以跳过测试帧输出环节
              3. 多线程测试，创建一个专用线程输出和处理测试帧，一个变种是在等待ui线程同步（只适合dxprocedure）的时候额外渲染
              以上做法都会导致额外的渲染开销
              4. 变换渲染：在移动过程中渲染最大上下限然后裁剪，然后再按照实际值进行输出，既不会产生渲染开销，也不会产生太大的视觉效果损害
              但是依然会有很大的计算开销，应该精细的实现相关性能
              5. 网格 类似碰撞检测，使用一个网格储存点位，可以以极少的开销发现上下限，但是当点位数量巨大，比如高达100000个网格时，开销会直线上升
              6. shader 利用shader计算得到上限，然后相应调整
             */
            _renderer.CurrentScrollRange = new ScrollRange(0, (long) e.NewValue);
            _renderer.ScrollRangeChanged = true;
            /*var currentYAxisValue = _renderer.CurrentYAxisValue;
            var bitmapSource = await OpenTkControl.PushRenderTask(
                (procedure => { _renderer.CurrentYAxisValue = MaxYAxis; }),
                (procedure => { _renderer.CurrentYAxisValue = currentYAxisValue; }));
            int bytesPerPixel = ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            int stride = bitmapSource.PixelWidth * bytesPerPixel;
            int bufferStride = stride / 4;
            var pixelHeight = bitmapSource.PixelHeight;
            var pixels = new int[pixelHeight * bufferStride];
            bitmapSource.CopyPixels(pixels, stride, 0);
            var topLine = FindTop(pixels, pixelHeight, bufferStride, lineColorINT);
            if (topLine == 0)
            {
                return;
            }

            var height = ((double)(pixelHeight - topLine)) / (double)pixelHeight * MaxYAxis + 0;
            _renderer.CurrentYAxisValue = (long)height;*/
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            this.OpenTkControl.Close();
        }

        private async void Test_OnClick(object sender, RoutedEventArgs e)
        {
            this.OpenTkControl.IsRenderContinuously = !this.OpenTkControl.IsRenderContinuously;
        }

        private int FindTop(int[] pixels, int pixelHeight, int bufferStride, int argb)
        {
            for (int i = 0; i < pixelHeight; i++)
            {
                for (int j = 0; j < bufferStride; j++)
                {
                    var b = pixels[j + i * bufferStride];
                    if (b == argb)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void FrameRate_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OpenTkControl.MaxFrameRate = (int) e.NewValue;
        }
    }
}