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

        private readonly Fraps fraps = new Fraps();

        protected CanvasInfo RecentCanvasInfo;

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        TimeSpan stanSpan = TimeSpan.FromMilliseconds(300);

        DateTime _date = DateTime.Now;

        private volatile int Status = Idle;

        private const int Fire = 2;
        public const int Ready = 1;
        private const int Idle = 0;

        private Fraps realFraps = new Fraps();

        public ThreadOpenTkControl() : base()
        {
            IsVisibleChanged += (_, args) =>
            {
                if ((bool) args.NewValue)
                {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else
                {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };
            // IsVisibleChanged += (_, args) => { _visible = (bool) args.NewValue; };
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }


        private async void OnCompTargetRender(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                return;
            }

            Debug.WriteLine($"fire {DateTime.Now}");
            _lastRenderTime = currentRenderTime.Value;
            RenderTrigger = !RenderTrigger;
            // InvalidateVisual();
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null)
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
            }
        }

        /*private RenderTargetBitmap renderTargetBitmap
            = new RenderTargetBitmap(1000, 1000, 1, 1, PixelFormats.Pbgra32);*/


        protected override void OnRender(DrawingContext drawingContext)
        {
            Debug.WriteLine($"render {DateTime.Now}");
            /*var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                this.Source = frontBuffer.ImageSource;
                /*var imageSource = frontBuffer.ImageSource;
                drawingContext.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));#1#
            }*/

            // base.OnRender(drawingContext);
            var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                drawingContext.DrawImage(frontBuffer.ImageSource, new Rect(new Point(), this.RenderSize));
            }

            /*var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                var imageSource = frontBuffer.ImageSource;
                drawingContext.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));
            }*/

            /*drawingContext.DrawImage(renderTargetBitmap,
                new Rect(new Size(renderTargetBitmap.Width, renderTargetBitmap.Width)));*/
            // drawingContext.DrawImage(bitmap, new Rect(new Size(bitmap.Width, bitmap.Height)));
            /*var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                var imageSource = frontBuffer.ImageSource;
                drawingContext.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));
            }*/

            realFraps.Increment();
            fraps.DrawFps(drawingContext, new Point(10, 10));
            realFraps.DrawFps(drawingContext, new Point(10, 50));
            if (waiting)
            {
                _renderCompletedResetEvent.Set();
            }
        }


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
                // RecentCanvasInfo = new CanvasInfo(1000, 1000, 1, 1);
                RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SizeCanvas(RecentCanvasInfo);
            }).Wait(token);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
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

                    DrawingDirective drawingDirective = null;
                    if (RenderProcedure.ReadyToRender)
                    {
                        try
                        {
                            OnUITask(() => RenderProcedure?.Begin()).Wait(token);
                            drawingDirective = RenderProcedure.Render();
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

                        /*OnUITask(() =>
                        {
                            Debug.WriteLine("render in");

                            var frontBuffer = RenderProcedure.GetFrontBuffer();
                            if (frontBuffer.IsAvailable)
                            {
                                /*using (var drawingContext = drawingVisual.RenderOpen())
                                {
                                    var imageSource = frontBuffer.ImageSource;
                                    drawingContext.DrawImage(imageSource,
                                        new Rect(new Size(imageSource.Width, imageSource.Height)));
                                }#1#
                                InvalidateVisual();
                            }

                            else
                            {
                                _renderCompletedResetEvent.Set();
                            }
                            Debug.WriteLine("render out");
                        })*/
                        // renderTask?.Wait(token);
                        waiting = true;
                        WaitHandle.WaitAny(drawHandles);
                        _renderCompletedResetEvent.Reset();
                        waiting = false;
                        // RenderProcedure.SwapBuffer();
                    }
                    else
                    {
                        Thread.Sleep(30);
                    }
                }
            }
        }

        private volatile bool waiting = true;

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