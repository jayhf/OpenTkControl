using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Graphics;

namespace TestRenderer
{
    public class TestRendererCase
    {
        public const int LineCount = 1;

        public const int PointsCount = 1000;

        private const long MaxYAxis = (long)((1000 + PointsCount) * 0.1);

        private readonly Color4 _lineColor = Color4.White;

        public TendencyChartRenderer Renderer { get; } = new TendencyChartRenderer();

        public TestRendererCase()
        {
            var random = new Random();
            for (int i = 0; i < LineCount; i++)
            {
                var pointFs = new PointF[PointsCount];
                for (int j = 0; j < PointsCount; j++)
                {
                    pointFs[j] = new PointF(j, random.Next(j, 1000 + j) * 0.1f);
                }

                var simpleLineRenderer = new AdvancedLineRenderer(PointsCount) { LineColor = _lineColor };
                simpleLineRenderer.AddPoints(pointFs);
                Renderer.Add(simpleLineRenderer);
            }

            Renderer.CurrentScrollRange = new ScrollRange(0, PointsCount);
            Renderer.CurrentYAxisValue = MaxYAxis;
            Renderer.ScrollRangeChanged = false;
            Renderer.BackgroundColor = Color4.Black;
        }
    }
}