using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

namespace TestRenderer
{
    public interface ILineRenderer : IDisposable
    {
        void Initialize(Shader shader);
        void OnRenderFrame(LineRenderArgs args);
        void AddPoints(IList<PointF> points);

        void AddPoint(PointF point);
    }
}