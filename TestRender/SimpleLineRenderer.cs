using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace TestRenderer
{
    public class SimpleLineRenderer : ILineRenderer
    {
        public int PointCount { get; }

        public Color4 LineColor { get; set; }

        protected int VertexBufferObject;

        protected int VertexArrayObject;

        private Shader _shader;

        /// <summary>
        ///  
        /// </summary>
        public int SampleInterval { get; set; } = 1;

        public int BufferSize { get; }

        /// <summary>
        /// demo 不演示循环缓冲
        /// </summary>
        public float[] RingBuffer { get; private set; }

        public SimpleLineRenderer(int pointCount)
        {
            this.PointCount = pointCount;
            this.BufferSize = pointCount * 2;
            this.RingBuffer = new float[this.BufferSize];
        }

        public virtual void Initialize(Shader shader)
        {
            unsafe
            {
                this._shader = shader;
                VertexBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, BufferSize * sizeof(float),
                    RingBuffer,
                    BufferUsageHint.DynamicDraw); //new IntPtr(RingBuffer.Buffer.GetPointer()
                VertexArrayObject = GL.GenVertexArray();
                GL.BindVertexArray(VertexArrayObject);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, SampleInterval * 2 * sizeof(float),
                    0);
                GL.EnableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }
        }

        public virtual void OnRenderFrame(LineRenderArgs args)
        {
            GL.BindVertexArray(VertexArrayObject);
            _shader.SetColor("linecolor", LineColor);
            GL.DrawArrays(PrimitiveType.LineStrip, 0, PointCount / SampleInterval);
            /*var LineCount = 1;
            int[] indirect = new int[LineCount * 4];
            for (int i = 0; i < LineCount; i++)
            {
                indirect[0 + i * 4] = PointCount;
                indirect[1 + i * 4] = 1;
                indirect[2 + i * 4] = PointCount * i;
                indirect[3 + i * 4] = i;
            }*/

            // GL.MultiDrawArraysIndirect(PrimitiveType.LineStrip, indirect, LineCount, 0);
            GL.BindVertexArray(0);
        }

        public void AddPoints(IList<PointF> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var pointF = points[i];
                var j = i * 2;
                RingBuffer[j] = pointF.X;
                RingBuffer[j + 1] = pointF.Y;
            }
        }

        public void AddPoint(PointF point)
        {
            RingBuffer[0] = point.X;
            RingBuffer[1] = point.Y;
        }


        public virtual Task OnUpdateFrame()
        {
            return Task.CompletedTask;
        }


        public void Append()
        {
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VertexBufferObject);
            GL.DeleteVertexArray(VertexArrayObject);
        }
    }
}