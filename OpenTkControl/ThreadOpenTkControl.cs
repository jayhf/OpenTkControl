using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace OpenTkWPFHost
{
    /// <summary>
    /// A WPF control that performs all OpenGL rendering on a thread separate from the UI thread to improve performance
    /// </summary>
    public class ThreadOpenTkControl : OpenTkControlBase
    {
        /// <summary>
        /// The Thread object for the rendering thread， use origin thread but not task lest context switch
        /// </summary>
        // private Thread _renderThread;
        private Task renderTask;

        /// <summary>
        /// The CTS used to stop the thread when this control is unloaded
        /// </summary>
        private CancellationTokenSource _endThreadCts;

        private DebugProc _debugProc;

        private readonly Fraps _openglFraps = new Fraps() {Name = "GLFps"};

        private readonly Fraps _controlFraps = new Fraps() {Name = "WindowFps"};

        protected volatile CanvasInfo RecentCanvasInfo;

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private volatile bool _renderThreadStart = false;

        private readonly ManualResetEvent _renderLoopResetEvent = new ManualResetEvent(false);

        public ThreadOpenTkControl() : base()
        {
            IsVisibleChanged += (_, args) =>
            {
                var newValue = (bool) args.NewValue;
                this.IsUserVisible = newValue;
                if (newValue)
                {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else
                {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                return;
            }

            _lastRenderTime = currentRenderTime.Value;
            RenderTrigger = !RenderTrigger;
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null)
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
            }
        }

        // private readonly DrawingVisual _drawingCopy = new DrawingVisual();

        private readonly DrawingVisual _drawingVisual = new DrawingVisual();

        /// <summary>
        /// d3d image maybe flicker when 
        /// </summary>
        private volatile bool _isWaitingForSync = false;

        private TaskCompletionSource<bool> _completion;

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawDrawing(_drawingVisual.Drawing);
            if (ShowFps)
            {
                _controlFraps.Increment();
                _openglFraps.DrawFps(drawingContext, new Point(10, 10));
                _controlFraps.DrawFps(drawingContext, new Point(10, 50));
            }

            if (_isWaitingForSync)
            {
                _completion.SetResult(true);
                // _renderCompletedResetEvent.Set();
            }
        }

        private IWindowInfo _windowInfo;

        protected override void OpenRenderer(IWindowInfo windowInfo)
        {
            if (RenderProcedure != null)
            {
                this._windowInfo = windowInfo;
                StartRenderThread();
            }
        }

        protected override void CloseRenderer()
        {
            CloseRenderThread();
            base.CloseRenderer();
        }

        /// <summary>
        /// switch render procedure
        /// </summary>
        /// <param name="args"></param>
        protected override void OnRenderProcedureChanged(PropertyChangedArgs<IRenderProcedure> args)
        {
            if (Dispatcher.Invoke(IsDesignMode))
                return;
            if (args.OldValue != null && _renderThreadStart)
            {
                CloseRenderThread();
            }

            if (this.RenderProcedure != null && IsRendererOpened)
            {
                StartRenderThread();
            }
        }

        protected override void OnUserVisibleChanged(PropertyChangedArgs<bool> args)
        {
            if (args.NewValue)
            {
                _renderLoopResetEvent.Set();
            }
        }

        private Task OnUITask(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);
        }

        public BitmapSource GetCurrentImage()
        {
            var renderTargetBitmap = new RenderTargetBitmap(RecentCanvasInfo.ActualWidth, RecentCanvasInfo.ActualHeight,
                RecentCanvasInfo.DpiScaleX * 96, RecentCanvasInfo.DpiScaleY * 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(_drawingVisual);
            return renderTargetBitmap;
        }

        private readonly ConcurrentQueue<RenderProcedureTask> _renderActionQueue =
            new ConcurrentQueue<RenderProcedureTask>();

        /// <summary>
        /// allow user to run a  invisible render procedure which run under a condition between frames
        /// </summary>
        /// <param name="beforeAction">render operation before render</param>
        /// <param name="afterAction">render operation after render</param>
        /// <returns></returns>
        public Task<BitmapSource> PushRenderTask(Action<IRenderProcedure> beforeAction,
            Action<IRenderProcedure> afterAction)
        {
            var renderProcedureTask = new RenderProcedureTask(beforeAction, afterAction);
            _renderActionQueue.Enqueue(renderProcedureTask);
            return renderProcedureTask.BitmapCompletionSource.Task;
        }


        /// <summary>
        /// The function that the thread runs to render the control
        /// </summary>
        /// <param name="boxedToken"></param>
        private async Task RenderThread(object boxedToken)
        {
            var token = (CancellationToken) boxedToken;
            _debugProc = Callback;
            RenderProcedure.Initialize(_windowInfo);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            IRenderCanvas uiThreadCanvas = null;
            OnUITask(() =>
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                uiThreadCanvas = RenderProcedure.CreateCanvas(RecentCanvasInfo);
            }).Wait(token);
            var taskRenderCanvas = RenderProcedure.CreateCanvas(RecentCanvasInfo);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
            using (RenderProcedure)
            {
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(RecentCanvasInfo))
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITask(() => { uiThreadCanvas.Allocate(canvasInfo); }).Wait(token);
                        RenderProcedure.SizeFrame(canvasInfo);
                    }

                    var renderProcedureContext = RenderProcedure.Context;
                    if (RenderProcedure.Renderer != null && !RecentCanvasInfo.IsEmpty)
                    {
                        if (_renderActionQueue.TryDequeue(out var renderProcedureTask))
                        {
                            taskRenderCanvas.Allocate(canvasInfo);
                            while (!taskRenderCanvas.Ready)
                            {
                                Thread.Sleep(5);
                            }

                            renderProcedureTask.Enter(RenderProcedure);
                            taskRenderCanvas.Begin();
                            RenderProcedure.Render(taskRenderCanvas);
                            taskRenderCanvas.End();
                            renderProcedureTask.Exit(RenderProcedure);
                            //sequence differ from uithreadcanvas
                            RenderProcedure.SwapBuffer();
                            if (ShowFps)
                            {
                                _openglFraps.Increment();
                            }

                            var drawingVisual = new DrawingVisual();
                            using (var drawingContext = drawingVisual.RenderOpen())
                            {
                                taskRenderCanvas.FlushFrame(drawingContext);
                            }

                            var renderTargetBitmap = RecentCanvasInfo.CreateRenderTargetBitmap();
                            renderTargetBitmap.Render(drawingVisual);
                            renderTargetBitmap.Freeze();
                            renderProcedureTask.BitmapCompletionSource.SetResult(renderTargetBitmap);
                        }
                        else
                        {
                            if (uiThreadCanvas.Ready)
                            {
                                try
                                {
                                    OnUITask(() => uiThreadCanvas.Begin()).Wait(token);
                                    RenderProcedure.Render(uiThreadCanvas);
                                    if (ShowFps)
                                    {
                                        _openglFraps.Increment();
                                    }

                                    OnUITask(() => uiThreadCanvas.End()).Wait(token);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                                finally
                                {
                                }

                                if (uiThreadCanvas.IsDirty)
                                {
                                    OnUITask(() =>
                                    {
                                        using (var drawingContext = _drawingVisual.RenderOpen())
                                        {
                                            uiThreadCanvas.FlushFrame(drawingContext);
                                        }
                                    });
                                    if (!uiThreadCanvas.CanAsyncRender)
                                    {
                                        //由于wpf的帧率为60，意味着等待延迟最高达16ms,使用
                                        _completion = new TaskCompletionSource<bool>();
                                        _isWaitingForSync = true;
                                        renderProcedureContext.MakeCurrent(new EmptyWindowInfo());
                                        await _completion.Task;
                                        if (!renderProcedureContext.IsCurrent)
                                        {
                                            renderProcedureContext.MakeCurrent(_windowInfo);
                                        }

                                        _isWaitingForSync = false;
                                    }
                                }

                                //read previous turn before swap buffer
                                RenderProcedure.SwapBuffer();
                            }
                            else
                            {
                                Thread.Sleep(5);
                            }
                        }
                    }
                    else
                    {
                        renderProcedureContext.MakeCurrent(new EmptyWindowInfo());
                        await Task.Delay(1000, token);
                        if (!renderProcedureContext.IsCurrent)
                        {
                            renderProcedureContext.MakeCurrent(_windowInfo);
                        }
                    }

                    if (!UserVisible)
                    {
                        _renderLoopResetEvent.WaitOne();
                        _renderLoopResetEvent.Reset();
                    }
                }
            }
        }

        private void StartRenderThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

            _renderThreadStart = true;
            _openglFraps.Start();
            _controlFraps.Start();
            _endThreadCts = new CancellationTokenSource();
            renderTask = Task.Run(async () => await RenderThread((object) _endThreadCts.Token));
            /*_renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            };
            _renderThread.Start(_endThreadCts.Token);*/
        }

        private void CloseRenderThread()
        {
            if (!_renderThreadStart)
            {
                return;
            }

            _renderThreadStart = false;
            try
            {
                _endThreadCts.Cancel();
            }
            catch (Exception)
            {
            }
            finally
            {
                if (_isWaitingForSync)
                {
                    _renderCompletedResetEvent.Set();
                }

                if (!UserVisible)
                {
                    _renderLoopResetEvent.Set();
                }

                renderTask.Wait();
                // _renderThread.Join();
                _endThreadCts.Dispose();
            }
        }


        protected override void Dispose(bool dispose)
        {
            CloseRenderThread();
            this._controlFraps.Dispose();
            this._openglFraps.Dispose();
            this._renderCompletedResetEvent.Dispose();
            this._renderLoopResetEvent.Dispose();
        }
    }
}