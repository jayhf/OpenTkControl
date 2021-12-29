using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTkWPFHost;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

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

        public long CurrentYAxisValue
        {
            get => _currentYAxisValue;
            set
            {
                if (CurrentYAxisValue.Equals(value))
                {
                    return;
                }

                _currentYAxisValue = value;
                CalculateTransformMatrix(this.CurrentScrollRange, value);
            }
        }

        private Matrix4 _transformMatrix4;

        private volatile bool _scrollRangeChanged;

        public ScrollRange CurrentScrollRange
        {
            get => _currentScrollRange;
            set
            {
                if (_currentScrollRange.Equals(value))
                {
                    return;
                }

                _currentScrollRange = value;
                CalculateTransformMatrix(value, this.CurrentYAxisValue);
            }
        }

        /// <summary>
        /// 是否自动适配Y轴顶点
        /// </summary>
        public bool AutoYAxisApex { get; set; } = true;

        public int FrameRate { get; set; } = 0;

        public bool ScrollRangeChanged
        {
            get => _scrollRangeChanged;
            set => _scrollRangeChanged = value;
        }

        private Shader _shader;

        private void CalculateTransformMatrix(ScrollRange xRange, long yAxisApex)
        {
            var transform = Matrix4.Identity;
            transform *= Matrix4.CreateScale(2f / (xRange.End - xRange.Start),
                2f / ((float) yAxisApex), 0f);
            transform *= Matrix4.CreateTranslation(-1, -1, 0);
            _transformMatrix4 = transform;
        }

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


        private int _yAxisSsbo;
        private readonly int[] _yAxisRaster = new int[300];
        private ScrollRange _currentScrollRange;
        private long _currentYAxisValue=100;

        public bool IsInitialized { get; private set; }

        public TendencyChartRenderer()
        {

        }

        public void Initialize(IGraphicsContext context)
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _shader.Use();
            _yAxisSsbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _yAxisSsbo);
            GL.BufferData<int>(BufferTarget.ShaderStorageBuffer, _yAxisRaster.Length * sizeof(int), _yAxisRaster,
                BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _yAxisSsbo);
            foreach (var lineRenderer in LineRenderers)
            {
                lineRenderer.Initialize(this._shader);
            }
            var lineFloats = new float[2];
            GL.GetFloat(GetPName.LineWidthRange, lineFloats);
            GL.LineWidth(1);
            GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Fastest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        }

        public bool PreviewRender()
        {
            return true;
        }

        public void Render(GlRenderEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(BackgroundColor);
            if (LineRenderers.Count == 0)
            {
                return;
            }

            _shader.SetMatrix4("transform", _transformMatrix4);
            if (AutoYAxisApex && ScrollRangeChanged)
            {
                var empty = new int[300];
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _yAxisSsbo);
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, empty.Length * sizeof(int), empty);
            }

            foreach (var lineRenderer in LineRenderers)
            {
                lineRenderer.OnRenderFrame();
            }

            if (AutoYAxisApex && ScrollRangeChanged)
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _yAxisSsbo);
                var ptr = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);
                Marshal.Copy(ptr, _yAxisRaster, 0, _yAxisRaster.Length);
                GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                int i;
                for (i = _yAxisRaster.Length - 1; i > 0; i--)
                {
                    if (_yAxisRaster[i] == 1)
                    {
                        break;
                    }
                }

                /*1. 当最高点依然在ssbo的299位置时，应用变换然后在下次渲染再次检查，直到最高点不在299
                 2. 每次都检查，当差额小于3%时停止变换。
                 */
                /*if (CurrentYAxisValue==0)
                {
                    CurrentYAxisValue=
                }*/
                var adjustYAxisValue = (i * 1.1f) * CurrentYAxisValue / 200f;
                if (Math.Abs(CurrentYAxisValue - adjustYAxisValue) > CurrentYAxisValue * 0.03f)
                {
                    CurrentYAxisValue = (long) adjustYAxisValue;
                }
                else
                {
                    ScrollRangeChanged = false;
                }
            }
        }

        public void Resize(PixelSize size)
        {
            GL.Viewport(0, 0, size.Width, size.Height);
        }

        public void Uninitialize()
        {
            if (!IsInitialized)
            {
                return;
            }

            IsInitialized = false;
            foreach (var lineRenderer in LineRenderers)
            {
                lineRenderer.Dispose();
            }

            GL.DeleteBuffer(_yAxisSsbo);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.DeleteProgram(_shader.Handle);
        }

        public void Dispose()
        {
        }
    }
}