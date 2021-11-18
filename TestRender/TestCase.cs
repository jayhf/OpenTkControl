using System;
using OpenTK.Graphics;

namespace TestRenderer
{
    public class TestRendererCase
    {
        public const int LineCount = 1;

        public const int PointsCount = 1000;

        public const int LineLength = PointsCount * 2;

        private const long MaxYAxis = (long)((1000 + LineLength) * 0.1);

        private readonly Color4 _lineColor = Color4.White;

        public TendencyChartRenderer Renderer { get; } = new TendencyChartRenderer();

        public TestRendererCase()
        {
            var random = new Random();
            for (int i = 0; i < LineCount; i++)
            {
                var lineChartRenderer = new LineRenderer(PointsCount) { LineColor = _lineColor };
                var ringBuffer = lineChartRenderer.RingBuffer;
                for (int j = 0; j < LineLength; j += 2)
                {
                    ringBuffer[j] = j;
                    var next = random.Next(j, 1000 + j) * 0.1f;
                    ringBuffer[j + 1] = next;
                }

                Renderer.Add(lineChartRenderer);
            }

            Renderer.CurrentScrollRange = new ScrollRange(0, LineLength);
            Renderer.CurrentYAxisValue = MaxYAxis;
            Renderer.ScrollRangeChanged = false;
            Renderer.BackgroundColor = Color4.Black;
        }
    }
}