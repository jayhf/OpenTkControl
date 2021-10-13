using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

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
        private Task _renderTask;

        /// <summary>
        /// The CTS used to stop the thread when this control is unloaded
        /// </summary>
        private CancellationTokenSource _endThreadCts;

        private DebugProc _debugProc;

        private readonly Fraps _openglFraps = new Fraps() {Name = "GLFps"};

        private readonly Fraps _controlFraps = new Fraps() {Name = "WindowFps"};

        protected volatile CanvasInfo RecentCanvasInfo;

        private TimeSpan _lastWindowsRenderTime = TimeSpan.FromSeconds(-1);

        private volatile bool _renderThreadStart = false;

        private readonly StatefulManualResetEvent _userVisibleResetEvent = new StatefulManualResetEvent();

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
            if (currentRenderTime == _lastWindowsRenderTime)
            {
                return;
            }

            _lastWindowsRenderTime = currentRenderTime.Value;
            InvalidateVisual();
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null)
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
            }

            /*if (IsRendererOpened && !RenderContinuously)
            {
                CallRenderLoop();
            }*/
        }

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
                _completion.TrySetResult(true);
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

        private readonly StatefulManualResetEvent _manualRenderResetEvent = new StatefulManualResetEvent();

        protected override void ResumeRender()
        {
            _manualRenderResetEvent.TrySet();
        }

        /*public override void CallRenderLoop()
        {
            BeforeFrameFlush += RenderLoop_BeforeFrameFlush;
            IsRenderContinuously = true;
        }

        private void RenderLoop_BeforeFrameFlush()
        {
            IsRenderContinuously = false;
            BeforeFrameFlush -= RenderLoop_BeforeFrameFlush;
        }*/

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
                _userVisibleResetEvent.TrySet();
            }
        }

        private Task OnUITaskAsync(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        private void OnUITask(Action action)
        {
            Dispatcher.Invoke(action);
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);
        }

        public BitmapSource Shotcut()
        {
            var renderTargetBitmap = new RenderTargetBitmap(RecentCanvasInfo.ActualWidth, RecentCanvasInfo.ActualHeight,
                RecentCanvasInfo.DpiScaleX * 96, RecentCanvasInfo.DpiScaleY * 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(_drawingVisual);
            return renderTargetBitmap;
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
            OnUITaskAsync(() =>
            {
                RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                uiThreadCanvas = RenderProcedure.CreateCanvas(RecentCanvasInfo);
            }).Wait(token);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
            var lastRenderTime = DateTime.MinValue;
            using (RenderProcedure)
            {
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(RecentCanvasInfo))
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITaskAsync(() => { uiThreadCanvas.Allocate(canvasInfo); }).Wait(token);
                        RenderProcedure.SizeFrame(canvasInfo);
                    }

                    var renderProcedureContext = RenderProcedure.Context;
                    if (RenderProcedure.Renderer != null && !RecentCanvasInfo.IsEmpty)
                    {
                        if (uiThreadCanvas.Ready)
                        {
                            try
                            {
                                OnBeforeRender();
                                OnUITaskAsync(() => uiThreadCanvas.Begin()).Wait(token);
                                RenderProcedure.Render(uiThreadCanvas);
                                if (ShowFps)
                                {
                                    _openglFraps.Increment();
                                }

                                OnUITaskAsync(() => uiThreadCanvas.End()).Wait(token);
                                OnAfterRender();
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
                                    OnBeforeFrameFlush();
                                    using (var drawingContext = _drawingVisual.RenderOpen())
                                    {
                                        uiThreadCanvas.FlushFrame(drawingContext);
                                    }
                                });

                                if (!uiThreadCanvas.CanAsyncRender)
                                {
                                    //由于wpf的帧率为60，意味着等待延迟最高达16ms
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
                        _userVisibleResetEvent.WaitInfinity();
                    }

                    if (!RenderContinuously)
                    {
                        _manualRenderResetEvent.WaitInfinity();
                    }

                    if (EnableFrameRateLimit)
                    {
                        var now = DateTime.Now;
                        var renderTime = now - lastRenderTime;
                        if (renderTime < FrameGenerateSpan)
                        {
                            Thread.Sleep(FrameGenerateSpan - renderTime);
                        }

                        lastRenderTime = now;
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
            _renderTask = Task.Run(async () => { await RenderThread((object) _endThreadCts.Token); });
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
                    _completion.TrySetResult(true);
                }

                if (!UserVisible)
                {
                    _userVisibleResetEvent.ForceSet();
                }

                _manualRenderResetEvent.ForceSet();
                _renderTask.Wait();
                // _renderThread.Join();
                _endThreadCts.Dispose();
            }
        }


        protected override void Dispose(bool dispose)
        {
            CloseRenderThread();
            this._controlFraps.Dispose();
            this._openglFraps.Dispose();
            this._userVisibleResetEvent.Dispose();
            this._manualRenderResetEvent.Dispose();
        }
    }
}