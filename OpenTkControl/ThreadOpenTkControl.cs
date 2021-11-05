using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    /*UI线程和opengl线程的同步方式有三种：
     1. UI线程驱动，每次CompositionTarget发出渲染请求时会释放opengl，此方法性能较好，但是opengl的帧率无法超过wpf
     2. opengl驱动，每次产生新的帧就发出渲染请求，当然请求速率不超过ui，缺点是当opengl的帧率较低时，ui的帧数也较低（这个实际并非缺点）
     并且线程模型简单
     3. 独立的渲染过程，线程同步的复杂度大幅提升，灵活性好*/

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

        private readonly EventWaiter _userVisibleResetEvent = new EventWaiter();

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
                RecentCanvasInfo = this.GlSettings.CreateCanvasInfo(this);
                if (!RecentCanvasInfo.IsEmpty)
                {
                    _sizeNotEmptyWaiter.TrySet();
                }

                if (IsRefreshWhenRenderIncontinuous && !IsRenderContinuouslyValue)
                {
                    CallValidRenderOnce();
                }
            }
        }

        private readonly DrawingVisual _drawingVisual = new DrawingVisual();

        private readonly ContextWaiter _renderSyncWaiter = new ContextWaiter();

        private readonly ContextWaiter _sizeNotEmptyWaiter = new ContextWaiter();

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawDrawing(_drawingVisual.Drawing);
            _renderSyncWaiter.TrySet();
            if (ShowFps)
            {
                _controlFps.Increment();
                _openglFps.DrawFps(drawingContext, new Point(10, 10));
                _controlFps.DrawFps(drawingContext, new Point(10, 50));
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
            var glSettings = this.GlSettings;
            _renderTask = Task.Run(async () => { await RenderThread(_endThreadCts.Token, renderer, glSettings); });
        }

        private readonly EventWaiter _renderContinuousWaiter = new EventWaiter();

        protected override void ResumeRender()
        {
            _renderContinuousWaiter.TrySet();
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

        private async Task RenderThread(CancellationToken token, IRenderer renderer, GLSettings settings)
        {
            #region initialize

            var graphicsContext = _renderProcedureValue.Initialize(_windowInfo, settings);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            IRenderCanvas uiThreadCanvas = null;
            OnUITaskAsync(() =>
            {
                RecentCanvasInfo = this.GlSettings.CreateCanvasInfo(this);
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

            _renderSyncWaiter.Context = graphicsContext;
            _renderSyncWaiter.WindowInfo = _windowInfo;

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

                    if (!UserVisible)
                    {
                        _userVisibleResetEvent.WaitInfinity();
                    }

                    if (!IsRenderContinuouslyValue)
                    {
                        _renderContinuousWaiter.WaitInfinity();
                    }

                    if (!canvasInfo.Equals(RecentCanvasInfo) && !RecentCanvasInfo.IsEmpty)
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITaskAsync(() => { uiThreadCanvas.Allocate(RecentCanvasInfo); }).Wait(token);
                        _renderProcedureValue.SizeFrame(canvasInfo);
                        renderer.Resize(canvasInfo.GetPixelSize());
                    }

                    if (RecentCanvasInfo.IsEmpty)
                    {
                        await _sizeNotEmptyWaiter;
                        continue;
                    }

                    if (!uiThreadCanvas.Ready)
                    {
                        await graphicsContext.Delay(30, _windowInfo);
                        continue;
                    }

                    if (!renderer.PreviewRender())
                    {
                        await graphicsContext.Delay(30, _windowInfo);
                        continue;
                    }

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
                        if (EnableFrameRateLimit)
                        {
                            var now = DateTime.Now;
                            var renderInterval = now - lastRenderTime;
                            if (renderInterval < FrameGenerateSpan)
                            {
                                var interval = FrameGenerateSpan - renderInterval;
                                if (interval.Milliseconds > 5)
                                {
                                    await graphicsContext.Delay(interval.Milliseconds, _windowInfo);
                                }

                                Thread.Sleep(interval);
                            }

                            lastRenderTime = now;
                        }

                        OnUITask(() =>
                        {
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
                            await _renderSyncWaiter;
                        }
                    }

                    if (IsRenderContinuouslyValue)
                    {
                        //read previous turn before swap buffer
                        _renderProcedureValue.SwapBuffer();
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
                _sizeNotEmptyWaiter.ForceSet();
                _renderSyncWaiter.ForceSet();
                /*if (_isWaitingForSync)
                {
                    _completion.TrySetResult(true);
                }*/

                if (!UserVisible)
                {
                    _userVisibleResetEvent.ForceSet();
                }

                _renderContinuousWaiter.ForceSet();
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
            this._renderContinuousWaiter.Dispose();
        }
    }
}