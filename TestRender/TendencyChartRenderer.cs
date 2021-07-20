using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTkControl;

namespace TestRenderer
{
    public struct ScrollRange
    {
        public long Start;
        public long End;

        public ScrollRange(long start, long end)
        {
            Start = start;
            End = end;
        }
    }

    public class TendencyChartRenderer : IDisposable, IRenderer
    {
        public ConcurrentBag<LineRenderer> LineRenderers { get; set; } = new ConcurrentBag<LineRenderer>();

        public Color4 BackgroundColor { get; set; }

        /// <summary>
        /// 标签函数
        /// </summary>
        public Expression<Func<float, string>> LabelFormatExpression { get; set; }

        /// <summary>
        /// 采样间隔响应函数，如果需要渲染大量点位时，允许间隔渲染
        /// </summary>
        public Func<int, ScrollRange> ReactiveSampleIntervalFunc { get; set; }

        public long CurrentYAxisValue { get; set; }

        public ScrollRange CurrentScrollRange { get; set; }

        /// <summary>
        /// 是否自动适配Y轴
        /// </summary>
        public bool AutoYAxis { get; set; } = true;

        public int FrameRate { get; set; } = 0;

        private Shader _shader;


        public void Add(LineRenderer lineRenderer)
        {
            this.LineRenderers.Add(lineRenderer);
        }

        public void AddRange(IEnumerable<LineRenderer> lineRenderers)
        {
            foreach (var lineRenderer in lineRenderers)
            {
                this.LineRenderers.Add(lineRenderer);
            }
        }

        public void Restore(IEnumerable<LineRenderer> lineSeries)
        {
            if (LineRenderers.TryTake(out var item))
            {
                item.Dispose();
            }

            AddRange(lineSeries);
            foreach (var lineRenderer in lineSeries)
            {
                lineRenderer.Initialize(this._shader);
            }
        }

        public void Dispose()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.DeleteProgram(_shader.Handle);
        }

        public void Initialize(IGraphicsContext context)
        {
            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _shader.Use();
            foreach (var lineRenderer in LineRenderers)
            {
                lineRenderer.Initialize(this._shader);
            }
        }

        public void Render(GlRenderEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(BackgroundColor);
            if (LineRenderers.Count == 0)
            {
                return;
            }

            var transform = Matrix4.Identity;
            transform *= Matrix4.CreateScale(2f / (this.CurrentScrollRange.End - this.CurrentScrollRange.Start),
                2f / ((float) CurrentYAxisValue), 0f);
            transform *= Matrix4.CreateTranslation(-1, -1, 0);
            _shader.SetMatrix4("transform", transform);
            foreach (var lineRenderer in LineRenderers)
            {
                lineRenderer.OnRenderFrame();
            }
        }

        public void Resize(PixelSize size)
        {
            GL.Viewport(0, 0, size.Width, size.Height);
        }
    }
}