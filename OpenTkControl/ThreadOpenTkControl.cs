using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
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

        private readonly Fraps _openglFps = new Fraps() {Name = "GLFps"};

        private readonly Fraps _controlFps = new Fraps() {Name = "ControlFps"};

        protected volatile CanvasInfo RecentCanvasInfo;

        private TimeSpan _lastWindowsRenderTime = TimeSpan.FromSeconds(-1);

        private volatile bool _renderThreadStart = false;

        private readonly StatefulManualResetEvent _userVisibleResetEvent = new StatefulManualResetEvent();

        public ThreadOpenTkControl() : base()
        {
            _debugProc = Callback;
            DependencyPropertyDescriptor.FromProperty(IsUserVisibleProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this,
                    (sender, args) =>
                    {
                        if (this.IsUserVisible)
                        {
                            CompositionTarget.Rendering += OnCompTargetRender;
                        }
                        else
                        {
                            CompositionTarget.Rendering -= OnCompTargetRender;
                        }
                    });
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }


        private volatile bool _newFrameBufferReady = false;

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastWindowsRenderTime)
            {
                return;
            }

            if (currentRenderTime.HasValue)
            {
                _lastWindowsRenderTime = currentRenderTime.Value;
            }

            if (_newFrameBufferReady)
            {
                InvalidateVisual();
                _newFrameBufferReady = false;
            }
        }

        private volatile IRenderProcedure _renderProcedureValue;

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_renderProcedureValue != null)
            {
                RecentCanvasInfo = _renderProcedureValue.GlSettings.CreateCanvasInfo(this);
                if (IsRefreshWhenRenderIncontinuous && !IsRenderContinuouslyValue)
                {
                    CallValidRenderOnce();
                }
            }
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
                _controlFps.Increment();
                _openglFps.DrawFps(drawingContext, new Point(10, 10));
                _controlFps.DrawFps(drawingContext, new Point(10, 50));
            }

            if (_isWaitingForSync)
            {
                _completion.TrySetResult(true);
            }
        }

        private IWindowInfo _windowInfo;

        protected override void StartRenderProcedure(IWindowInfo windowInfo)
        {
            this._windowInfo = windowInfo;
            if (_renderThreadStart)
            {
                return;
            }

            _renderThreadStart = true;
            _openglFps.Start();
            _controlFps.Start();
            _endThreadCts = new CancellationTokenSource();
            this._renderProcedureValue = this.RenderProcedure;
            var renderer = this.Renderer;
            _renderTask = Task.Run(async () => { await RenderThread(_endThreadCts.Token, renderer); });
        }

        private readonly StatefulManualResetEvent _manualRenderResetEvent = new StatefulManualResetEvent();

        protected override void ResumeRender()
        {
            _manualRenderResetEvent.TrySet();
        }

        protected override void CloseRenderer()
        {
            CloseRenderThread();
            base.CloseRenderer();
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

        public BitmapSource Snapshot()
        {
            var renderTargetBitmap = new RenderTargetBitmap(RecentCanvasInfo.ActualWidth, RecentCanvasInfo.ActualHeight,
                RecentCanvasInfo.DpiScaleX * 96, RecentCanvasInfo.DpiScaleY * 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(_drawingVisual);
            return renderTargetBitmap;
        }

        private async Task RenderThread(CancellationToken token, IRenderer renderer)
        {
            #region initialize

            var graphicsContext = _renderProcedureValue.Initialize(_windowInfo);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            IRenderCanvas uiThreadCanvas = null;
            OnUITaskAsync(() =>
            {
                RecentCanvasInfo = _renderProcedureValue.GlSettings.CreateCanvasInfo(this);
                uiThreadCanvas = _renderProcedureValue.CreateCanvas();
                if (!RecentCanvasInfo.IsEmpty)
                {
                    uiThreadCanvas.Allocate(RecentCanvasInfo);
                }
            }).Wait(token);
            if (!RecentCanvasInfo.IsEmpty)
            {
                _renderProcedureValue.SizeFrame(RecentCanvasInfo);
                renderer.Initialize(graphicsContext);
                renderer.Resize(RecentCanvasInfo.GetPixelSize());
            }

            #endregion

            var canvasInfo = RecentCanvasInfo;
            var lastRenderTime = DateTime.MinValue;
            using (_renderProcedureValue)
            {
                while (!token.IsCancellationRequested)
                {
                    if (!renderer.IsInitialized)
                    {
                        renderer.Initialize(graphicsContext);
                    }

                    if (!canvasInfo.Equals(RecentCanvasInfo) && !RecentCanvasInfo.IsEmpty)
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITaskAsync(() => { uiThreadCanvas.Allocate(RecentCanvasInfo); }).Wait(token);
                        _renderProcedureValue.SizeFrame(canvasInfo);
                        renderer.Resize(canvasInfo.GetPixelSize());
                    }

                    if (!RecentCanvasInfo.IsEmpty)
                    {
                        if (uiThreadCanvas.Ready)
                        {
                            try
                            {
                                OnBeforeRender();
                                OnUITaskAsync(() => { uiThreadCanvas.Begin(); }).Wait(token);
                                _renderProcedureValue.Render(uiThreadCanvas, renderer);
                                if (ShowFps)
                                {
                                    _openglFps.Increment();
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
                                    if (!IsRenderContinuouslyValue)
                                    {
                                        _renderProcedureValue.SwapBuffer();
                                    }
                                    using (var drawingContext = _drawingVisual.RenderOpen())
                                    {
                                        uiThreadCanvas.FlushFrame(drawingContext);
                                    }

                                    _newFrameBufferReady = true;
                                });

                                if (!uiThreadCanvas.CanAsyncRender)
                                {
                                    //由于wpf的最高帧率为60，意味着等待延迟最高达16ms，上下文切换的开销小于wait
                                    _completion = new TaskCompletionSource<bool>();
                                    _isWaitingForSync = true;
                                    graphicsContext.MakeCurrent(new EmptyWindowInfo());
                                    await _completion.Task;
                                    if (!graphicsContext.IsCurrent)
                                    {
                                        graphicsContext.MakeCurrent(_windowInfo);
                                    }

                                    _isWaitingForSync = false;
                                }
                            }

                            if (IsRenderContinuouslyValue)
                            {
                                //read previous turn before swap buffer
                                _renderProcedureValue.SwapBuffer();
                            }
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }
                    else
                    {
                        graphicsContext.MakeCurrent(new EmptyWindowInfo());
                        await Task.Delay(200, token);
                        if (!graphicsContext.IsCurrent)
                        {
                            graphicsContext.MakeCurrent(_windowInfo);
                        }
                    }

                    if (!UserVisible)
                    {
                        _userVisibleResetEvent.WaitInfinity();
                    }

                    if (!IsRenderContinuouslyValue)
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
            this._controlFps.Dispose();
            this._openglFps.Dispose();
            this._userVisibleResetEvent.Dispose();
            this._manualRenderResetEvent.Dispose();
        }
    }
}