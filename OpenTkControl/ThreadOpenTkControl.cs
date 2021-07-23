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
using System.Windows.Threading;
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

        private Fraps fraps = new Fraps();

        public ThreadOpenTkControl() : base()
        {
            /*IsVisibleChanged += (_, args) =>
            {
                if ((bool) args.NewValue)
                {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else
                {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };*/
            // IsVisibleChanged += (_, args) => { _visible = (bool) args.NewValue; };
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }

        private int x;

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                // It's possible for Rendering to call back twice in the same frame
                // so only render when we haven't already rendered in this frame.
                // Reference: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-direct3d9-content-in-wpf?view=netframeworkdesktop-4.8#to-import-direct3d9-content
                return;
            }

            _lastRenderTime = currentRenderTime.Value;
            // InvalidateVisual();
            RenderTrigger = x;
            x++;
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null && RenderProcedure.IsInitialized)
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                ;
            }
        }

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        TimeSpan stanSpan = TimeSpan.FromMilliseconds(300);

        DateTime _date = DateTime.Now;

        private Fraps realFraps = new Fraps();

        private DoubleBufferDrawingVisual drawingVisual = new DoubleBufferDrawingVisual();

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!_renderThreadStart)
            {
                return;
            }

            base.OnRender(drawingContext);
            if (RenderCommand != Run)
            {
                return;
            }

            RenderCommand = Idle;
            // var imageSource = RenderProcedure.GetFrontBuffer().GetSource();
            // drawingContext.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));
            drawingContext.DrawDrawing(drawingVisual.FrontBuffer.Drawing);
            realFraps.Increment();
            fraps.DrawFps(drawingContext, new Point(10, 10));
            realFraps.DrawFps(drawingContext, new Point(10, 50));
            _renderCompletedResetEvent.Set();
        }

        protected CanvasInfo RecentCanvasInfo;

        protected override void OnRenderProcedureChanged()
        {
            if (Dispatcher.Invoke(IsDesignMode))
                return;
            if (this.RenderProcedure == null)
            {
                CloseThread();
            }

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

        public IList<ImageSource> HistorySources { get; set; } = new List<ImageSource>();

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
            int x = 0;
            using (RenderProcedure)
            {
                Task renderTask = null;
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
                            OnUITask(() => RenderProcedure?.Begin()).Wait(token);
                            RenderProcedure.Render();
                            fraps.Increment();
                            OnUITask(() => RenderProcedure?.End()).Wait(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        finally
                        {
                        }

                        OnUITask(() =>
                        {
                            var imageSource = RenderProcedure.GetFrontBuffer().GetSource();
                            if (imageSource != null && imageSource.Width > 0 && imageSource.Height > 0)
                            {
                                using (var drawingContext = drawingVisual.BackBuffer.RenderOpen())
                                {
                                    drawingContext.DrawImage(imageSource,
                                        new Rect(new Size(imageSource.Width, imageSource.Height)));
                                    fraps.DrawFps(drawingContext, new Point(10, 10));
                                }
                                drawingVisual.Swap();
                                RenderCommand = Run;
                                x++;
                                RenderTrigger = x;
                            }
                            else
                            {
                                _renderCompletedResetEvent.Set();
                            }

                        });

                        WaitHandle.WaitAny(drawHandles);
                        _renderCompletedResetEvent.Reset();
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

            fraps.Start();
            realFraps.Start();
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
            try
            {
                _endThreadCts.Cancel();
            }
            catch (Exception e)
            {
            }

            _renderThread.Join();
            _endThreadCts.Dispose();
        }

        public void Dispose()
        {
            CloseThread();
        }
    }
}