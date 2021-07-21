using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using OpenTkControl;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Linq;
using OpenTK.Graphics;
using TestRenderer;

namespace OpenTkControlExample
{
    public partial class MainWindow
    {
        private readonly DispatcherTimer _fpsTimer = new DispatcherTimer();
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private int _fps;
        private float _angle;
        private int _displayList;

        private const int LineCount = 10;
        public const int PointsCount = 10;

        public const int LineLength = PointsCount * 2;


        public MainWindow()
        {
            this.InitializeComponent();
            _fpsTimer.Interval = TimeSpan.FromSeconds(1);
            _fpsTimer.Tick += (sender, args) =>
            {
                var seconds = _sw.Elapsed.TotalSeconds;
                _sw.Restart();
                Title = (Interlocked.Exchange(ref _fps, 0) / seconds).ToString("F1") + " FPS";
            };
            _fpsTimer.Start();
            var renderer = new TendencyChartRenderer();
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
            renderer.CurrentScrollRange = new ScrollRange(0, end);
            renderer.CurrentYAxisValue = 1000;
            renderer.BackgroundColor = Color4.Black;
            this.OpenTkControl.Renderer = new GLDXProcedure(new GLSettings())
            {
                Renderer = renderer,
            };
        }


        private void OpenTkControl_OnGlRender(object sender)
        {
            /*GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            float halfWidth = e.Width / 2f;
            float halfHeight = e.Height / 2f;
            GL.Ortho(-halfWidth, halfWidth, -halfHeight, halfHeight, 1000, -1000);
            GL.Viewport(0, 0, e.Width, e.Height);

            //Using the same example as used by https://github.com/freakinpenguin/OpenTK-WPF
            //to make it easier to compare different approaches
            if (_displayList <= 0 || e.NewContext)
            {
                _displayList = GL.GenLists(1);
                GL.NewList(_displayList, ListMode.Compile);

                GL.Color3(Color.Red);

                GL.Begin(PrimitiveType.Points);

                Random rnd = new Random();
                for (int i = 0; i < 1000000; i++)
                {
                    float factor = 0.2f;
                    Vector3 position = new Vector3(
                        rnd.Next(-1000, 1000) * factor,
                        rnd.Next(-1000, 1000) * factor,
                        rnd.Next(-1000, 1000) * factor);
                    GL.Vertex3(position);

                    position.Normalize();
                    GL.Normal3(position);
                }

                GL.End();

                GL.EndList();
            }

            // GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);

            GL.ClearColor(Color.FromArgb(200, Color.LightBlue));
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            
            _angle += 1f;
            GL.Rotate(_angle, Vector3.UnitZ);
            GL.Rotate(_angle, Vector3.UnitY);
            GL.Rotate(_angle, Vector3.UnitX);
            GL.Translate(0.5f, 0, 0);

            GL.CallList(_displayList);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Begin(PrimitiveType.Triangles);

            GL.Color4(Color.Green);
            GL.Vertex3(0, 300, 0);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(300, 0, 0);

            GL.End();*/

            Interlocked.Increment(ref _fps);
        }

        private void OpenTkControl_OnExceptionOccurred(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.ExceptionObject);
        }
    }
}