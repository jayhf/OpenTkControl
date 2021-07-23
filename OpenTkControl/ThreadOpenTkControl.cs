using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform.Windows;

namespace OpenTkControl
{
    /// <summary>
    /// A WPF control that performs all OpenGL rendering on a thread separate from the UI thread to improve performance
    /// </summary>
    public class ThreadOpenTkControl : OpenTkControlBase, IDisposable
    {
        public static readonly DependencyProperty ThreadNameProperty = DependencyProperty.Register(
            nameof(ThreadName), typeof(string), typeof(ThreadOpenTkControl),
            new PropertyMetadata("OpenTk Render Thread"));


        /// <summary>
        /// The name of the background thread that does the OpenGL rendering
        /// </summary>
        public string ThreadName
        {
            get => (string) GetValue(ThreadNameProperty);
            set => SetValue(ThreadNameProperty, value);
        }

        /// <summary>
        /// The Thread object for the rendering thread
        /// </summary>
        private Thread _renderThread;

        /// <summary>
        /// The CTS used to stop the thread when this control is unloaded
        /// </summary>
        private CancellationTokenSource _endThreadCts;

        private DebugProc _debugProc;

        private bool _visible;

        public ThreadOpenTkControl()
        {
            IsVisibleChanged += (_, args) => { _visible = (bool) args.NewValue; };
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null && RenderProcedure.IsInitialized)
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
            }
        }

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        protected override void OnRender(DrawingContext drawingContext)
        {
            Debug.WriteLine("draw in");
            base.OnRender(drawingContext);
            /*drawingContext.DrawRectangle(Brushes.LightSeaGreen, new Pen(Brushes.DarkBlue, 2),
                new Rect(new Point(10, 10), new Size(100, 100)));*/

            if (!_renderThreadStart)
            {
                Debug.WriteLine("draw interrupt");
                return;
            }

            if (RenderCommand == Idle)
            {
                Debug.WriteLine("draw prevent");
                return;
            }

            RenderCommand = Idle;
            var source = RenderProcedure.GetFrontBuffer().GetSource();
            if (source.Width > 0 && source.Height > 0)
            {
                Debug.WriteLine("draw success");
                var drawingVisual = new DrawingVisual();
                using (var renderOpen = drawingVisual.RenderOpen())
                {
                    renderOpen.DrawImage(source,
                        new Rect(new Size(source.Width, source.Height)));
                }

                if (HistorySources.Count < 200)
                {
                    HistorySources.Add(drawingVisual);
                }

                drawingContext.DrawImage(source,
                    new Rect(new Size(source.Width, source.Height)));
            }
            else
            {
                Debug.WriteLine("draw empty");
            }

            _renderCompletedResetEvent.Set();
            Debug.WriteLine("draw out");
        }

        protected CanvasInfo RecentCanvasInfo;

        protected override void OnRenderProcedureChanged()
        {
            if (Dispatcher.Invoke(IsDesignMode))
                return;
            if (this.RenderProcedure != null && IsLoaded)
            {
                StartThread();
            }
        }

        protected override void OnRenderProcedureChanging()
        {
            if (_renderThreadStart)
            {
                CloseThread();
            }
        }

        public Task OnUITask(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private volatile bool _renderThreadStart = false;

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
            if (!_renderThreadStart && RenderProcedure != null)
            {
                StartThread();
            }
        }

        public IList<DrawingVisual> HistorySources { get; set; } = new List<DrawingVisual>();

        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);
            //unload don't close thread
        }

        /// <summary>
        /// The function that the thread runs to render the control
        /// </summary>
        /// <param name="boxedToken"></param>
        private void RenderThread(object boxedToken)
        {
            var token = (CancellationToken) boxedToken;
            _debugProc = Callback;
            RenderProcedure.Initialize(WindowInfo);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            OnUITask(() =>
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SizeCanvas(RecentCanvasInfo);
            }).Wait(token);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
            using (RenderProcedure)
            {
                WaitHandle[] drawHandles = {token.WaitHandle, _renderCompletedResetEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(RecentCanvasInfo))
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITask(() => { RenderProcedure.SizeCanvas(RecentCanvasInfo); }).Wait(token);
                        RenderProcedure.SizeFrame(RecentCanvasInfo);
                    }

                    if (RenderProcedure.ReadyToRender)
                    {
                        try
                        {
                            Debug.WriteLine("render in");
                            OnUITask(() => RenderProcedure.Begin()).Wait(token);
                            RenderProcedure.Render();
                            OnUITask(() => RenderProcedure.End()).Wait(token);
                            Debug.WriteLine("render out");
                        }
                        finally
                        {
                            RenderCommand = Run;
                            OnUITask(InvalidateVisual);
                            WaitHandle.WaitAny(drawHandles);
                            _renderCompletedResetEvent.Reset();
                            RenderProcedure.SwapBuffer();
                        }
                    }
                    else
                    {
                        Thread.Sleep(30);
                    }
                }
            }
        }

        private volatile int RenderCommand = Idle;

        private const int Run = 1;
        private const int Idle = 0;

        public void StartThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

            _endThreadCts = new CancellationTokenSource();
            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = ThreadName
            };
            _renderThread.Start(_endThreadCts.Token);
            _renderThreadStart = true;
        }

        private void CloseThread()
        {
            _renderThreadStart = false;
            _endThreadCts.Cancel();
            _renderThread.Join();
            _endThreadCts.Dispose();
        }

        public void Dispose()
        {
            CloseThread();
        }
    }
}