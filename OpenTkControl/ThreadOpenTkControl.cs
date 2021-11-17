﻿using System;
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
using OpenTK.Graphics;
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

        private readonly DebugProc _debugProc;

        private readonly FpsCounter _glFps = new FpsCounter() {Name = "GLFps"};

        private readonly FpsCounter _controlFps = new FpsCounter() {Name = "ControlFps"};

        protected volatile CanvasInfo RecentCanvasInfo = new CanvasInfo(0, 0, 96, 96);

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

            CallValidRenderOnce();
        }


        private readonly DrawingGroup _drawingGroup = new DrawingGroup();

        private readonly ContextWaiter _renderSyncWaiter = new ContextWaiter();

        private readonly ContextWaiter _sizeNotEmptyWaiter = new ContextWaiter();

        private readonly EventWaiter _renderContinuousWaiter = new EventWaiter();

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawDrawing(_drawingGroup);
            _renderSyncWaiter.TrySet();
            if (ShowFps)
            {
                _controlFps.Increment();
                _glFps.DrawFps(drawingContext, new Point(10, 10));
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
            IRenderCanvas uiRenderCanvas = null;
            try
            {
                RecentCanvasInfo = this.GlSettings.CreateCanvasInfo(this);
                uiRenderCanvas = renderProcedureValue.CreateCanvas();
            }
            catch (Exception e)
            {
                OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Inbuilt, e));
                return;
            }

            _renderTask = Task.Run(async () =>
            {
                using (_endThreadCts)
                {
                    await RenderThread(_endThreadCts.Token, renderProcedureValue, renderer, uiRenderCanvas, glSettings);
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
            var targetBitmap = RecentCanvasInfo.CreateRenderTargetBitmap();
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawDrawing(_drawingGroup);
            }

            targetBitmap.Render(drawingVisual);
            return targetBitmap;
        }

        private async Task RenderThread(CancellationToken token, IRenderProcedure renderProcedureValue,
            IRenderer renderer, IRenderCanvas uiThreadCanvas, GLSettings settings)
        {
            #region initialize

            IGraphicsContext graphicsContext;
            try
            {
                graphicsContext = renderProcedureValue.Initialize(_windowInfo, settings);
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
                renderer.Initialize(graphicsContext);
                _renderSyncWaiter.Context = graphicsContext;
                _renderSyncWaiter.WindowInfo = _windowInfo;
                _sizeNotEmptyWaiter.Context = graphicsContext;
                _sizeNotEmptyWaiter.WindowInfo = _windowInfo;
            }
            catch (Exception e)
            {
                OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Inbuilt, e));
                return;
            }

            try
            {
                if (!renderer.IsInitialized)
                {
                    renderer.Initialize(graphicsContext);
                }
            }
            catch (Exception e)
            {
                OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Initialize, e));
                return;
            }

            #endregion

            CanvasInfo canvasInfo = null;
            GlRenderEventArgs renderEventArgs = null;
            bool renderContinuously;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            using (renderProcedureValue)
            {
                while (!token.IsCancellationRequested)
                {
                    renderContinuously = IsRenderContinuouslyValue;
                    if (!UserVisible)
                    {
                        _userVisibleResetEvent.WaitInfinity();
                    }

                    if (!renderContinuously)
                    {
                        _renderContinuousWaiter.WaitInfinity();
                    }

                    if (!Equals(canvasInfo, RecentCanvasInfo) && !RecentCanvasInfo.IsEmpty)
                    {
                        canvasInfo = RecentCanvasInfo;
                        renderEventArgs = RecentCanvasInfo.GetRenderEventArgs();
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
                        uiThreadCanvas.Prepare();
                        renderProcedureValue.PreRender();
                        renderer.Render(renderEventArgs);
                        if (!renderContinuously)
                        {
                            renderProcedureValue.SwapBuffer();
                        }

                        renderProcedureValue.PostRender();
                        renderProcedureValue.BindCanvas(uiThreadCanvas);
                        OnUITaskAsync(() => uiThreadCanvas.Flush()).Wait(token);
                        OnAfterRender();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Render, exception));
                    }
                    finally
                    {
                    }

                    if (uiThreadCanvas.IsDirty)
                    {
                        if (ShowFps)
                        {
                            _glFps.Increment();
                        }

                        OnUITaskAsync(() =>
                        {
                            using (var drawingContext = _drawingGroup.Open())
                            {
                                uiThreadCanvas.FlushFrame(drawingContext);
                            }

                            InvalidateVisual();
                        });
                        if (!uiThreadCanvas.CanAsyncFlush)
                        {
                            //由于wpf的默认刷新率为60，意味着等待延迟最高达16ms，上下文切换的开销小于wait
                            await _renderSyncWaiter;
                        }

                        if (EnableFrameRateLimit)
                        {
                            var renderMinus = FrameGenerateSpan - stopwatch.ElapsedMilliseconds;
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
                    }

                    if (renderContinuously)
                    {
                        //read previous turn before swap buffer
                        renderProcedureValue.SwapBuffer();
                    }
                    
                }

                renderer.Uninitialize();
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
            this._glFps.Dispose();
            this._userVisibleResetEvent.Dispose();
            this._renderContinuousWaiter.Dispose();
        }
    }
}