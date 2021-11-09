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
     3. 独立的两个渲染线程，线程同步的复杂度大幅提升*/

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
        
        private readonly EventWaiter _userVisibleResetEvent = new EventWaiter();

        public ThreadOpenTkControl() : base()
        {
            _debugProc = Callback;
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
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

        private readonly DrawingVisual _drawingVisual = new DrawingVisual();

        private readonly ContextWaiter _renderSyncWaiter = new ContextWaiter();

        private readonly ContextWaiter _sizeNotEmptyWaiter = new ContextWaiter();

        private readonly EventWaiter _renderContinuousWaiter = new EventWaiter();

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
            _endThreadCts = new CancellationTokenSource();
            var renderProcedureValue = this.RenderProcedure;
            var renderer = this.Renderer;
            var glSettings = this.GlSettings;
            _renderTask = Task.Run(async () =>
            {
                using (_endThreadCts)
                {
                    await RenderThread(_endThreadCts.Token, renderProcedureValue, renderer, glSettings);
                }
            });
        }

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

        private async Task RenderThread(CancellationToken token, IRenderProcedure renderProcedureValue, IRenderer renderer,
            GLSettings settings)
        {
            #region initialize

            var graphicsContext = renderProcedureValue.Initialize(_windowInfo, settings);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            IRenderCanvas uiThreadCanvas = null;
            OnUITaskAsync(() =>
            {
                RecentCanvasInfo = this.GlSettings.CreateCanvasInfo(this);
                uiThreadCanvas = renderProcedureValue.CreateCanvas();
                if (!RecentCanvasInfo.IsEmpty)
                {
                    uiThreadCanvas.Allocate(RecentCanvasInfo);
                }
            }).Wait(token);
            if (!RecentCanvasInfo.IsEmpty)
            {
                renderProcedureValue.SizeFrame(RecentCanvasInfo);
                renderer.Initialize(graphicsContext);
                renderer.Resize(RecentCanvasInfo.GetPixelSize());
            }

            _renderSyncWaiter.Context = graphicsContext;
            _renderSyncWaiter.WindowInfo = _windowInfo;

            #endregion

            var canvasInfo = RecentCanvasInfo;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var milliseconds = FrameGenerateSpan.Milliseconds;
            using (renderProcedureValue)
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
                        renderProcedureValue.SizeFrame(canvasInfo);
                        renderer.Resize(canvasInfo.GetPixelSize());
                    }

                    if (RecentCanvasInfo.IsEmpty)
                    {
                        await _sizeNotEmptyWaiter;
                        continue;
                    }

                    if (!uiThreadCanvas.Ready)
                    {
                        var spinWait = new SpinWait();
                        spinWait.SpinOnce();
                        spinWait.SpinOnce();
                        // await graphicsContext.Delay(, _windowInfo);
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
                        renderProcedureValue.Render(uiThreadCanvas, renderer);
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
                        if (ShowFps)
                        {
                            _openglFps.Increment();
                        }

                        OnUITaskAsync(() =>
                        {
                            if (!IsRenderContinuouslyValue)
                            {
                                renderProcedureValue.SwapBuffer();
                            }

                            using (var drawingContext = _drawingVisual.RenderOpen())
                            {
                                uiThreadCanvas.FlushFrame(drawingContext);
                            }

                            InvalidateVisual();
                        });
                        if (EnableFrameRateLimit)
                        {
                            var renderMinus = milliseconds - stopwatch.ElapsedMilliseconds;
                            if (renderMinus > 5)
                            {
                                await graphicsContext.Delay((int) renderMinus, _windowInfo);
                            }
                            else if (renderMinus > 0)
                            {
                                Thread.Sleep((int) renderMinus);
                            }

                            stopwatch.Restart();
                        }
                        /*if (!uiThreadCanvas.CanAsyncFlush)
                        {
                            //由于wpf的默认刷新率为60，意味着等待延迟最高达16ms，上下文切换的开销小于wait
                            await _renderSyncWaiter;
                        }*/
                    }

                    if (IsRenderContinuouslyValue)
                    {
                        //read previous turn before swap buffer
                        renderProcedureValue.SwapBuffer();
                    }
                }

                stopwatch.Stop();
            }
        }

        private void CloseRenderThread()
        {
            try
            {
                _endThreadCts.Cancel();
            }
            finally
            {
                _sizeNotEmptyWaiter.ForceSet();
                _renderSyncWaiter.ForceSet();
                if (!UserVisible)
                {
                    _userVisibleResetEvent.ForceSet();
                }

                _renderContinuousWaiter.ForceSet();
                _renderTask.Wait();
            }
        }

        protected override void Dispose(bool dispose)
        {
            this._controlFps.Dispose();
            this._openglFps.Dispose();
            this._userVisibleResetEvent.Dispose();
            this._renderContinuousWaiter.Dispose();
        }
    }
}