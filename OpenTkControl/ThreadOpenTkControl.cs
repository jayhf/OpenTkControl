using System;
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

        private DrawingGroup drawingGroup = new DrawingGroup();

        /// <summary>
        /// d3d image maybe flicker when 
        /// </summary>
        private volatile bool _isWaitingForSync = false;

        private TaskCompletionSource<bool> completion;

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawDrawing(drawingGroup);
            if (ShowFps)
            {
                _controlFraps.Increment();
                _openglFraps.DrawFps(drawingContext, new Point(10, 10));
                _controlFraps.DrawFps(drawingContext, new Point(10, 50));
            }

            if (_isWaitingForSync)
            {
                completion.SetResult(true);
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
            OnUITask(() =>
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.Canvas.Create(RecentCanvasInfo);
            }).Wait(token);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
            using (RenderProcedure)
            {
                // WaitHandle[] drawHandles = {token.WaitHandle, _renderCompletedResetEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(RecentCanvasInfo))
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITask(() => { RenderProcedure.Canvas.Create(canvasInfo); }).Wait(token);
                        RenderProcedure.SizeFrame(canvasInfo);
                    }

                    if (RenderProcedure.ReadyToRender)
                    {
                        bool renderSuccess;
                        try
                        {
                            OnUITask((() => RenderProcedure.Begin())).Wait(token);
                            renderSuccess = RenderProcedure.Render();
                            if (ShowFps && renderSuccess)
                            {
                                _openglFraps.Increment();
                            }

                            OnUITask((() => RenderProcedure.End())).Wait(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        finally
                        {
                        }

                        if (renderSuccess)
                        {
                            OnUITask((() =>
                            {
                                if (RenderProcedure.Canvas.IsAvailable)
                                {
                                    using (var drawingContext = drawingGroup.Open())
                                    {
                                        RenderProcedure.FlushFrame(drawingContext);
                                    }
                                }
                            }));
                        }

                        if (!RenderProcedure.CanAsyncRender)
                        {
                            completion = new TaskCompletionSource<bool>();
                            _isWaitingForSync = true;
                            RenderProcedure.Context.MakeCurrent(new EmptyWindowInfo());
                            await completion.Task;
                            if (!RenderProcedure.Context.IsCurrent)
                            {
                                RenderProcedure.Context.MakeCurrent(_windowInfo);
                            }

                            /*WaitHandle.WaitAny(drawHandles);
                            _renderCompletedResetEvent.Reset();*/
                            _isWaitingForSync = false;
                        }

                        //read previous turn before swap buffer
                        RenderProcedure.SwapBuffer();
                    }
                    else
                    {
                        // Thread.Sleep(5);
                        RenderProcedure.Context.MakeCurrent(new EmptyWindowInfo());
                        await Task.Delay(1, token);
                        if (!RenderProcedure.Context.IsCurrent)
                        {
                            RenderProcedure.Context.MakeCurrent(_windowInfo);
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