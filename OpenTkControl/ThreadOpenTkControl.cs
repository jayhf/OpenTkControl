using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using Point = System.Windows.Point;

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

        private readonly FpsCounter _glFps = new FpsCounter() { Title = "GLFps" };

        private readonly FpsCounter _controlFps = new FpsCounter() { Title = "ControlFps" };

        protected volatile CanvasInfo RecentCanvasInfo = new CanvasInfo(0, 0, 96, 96);

        private readonly EventWaiter _userVisibleResetEvent = new EventWaiter();

        public ThreadOpenTkControl() : base()
        {
            _debugProc = Callback;
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecentCanvasInfo = this.GlSettings.CreateCanvasInfo(this);
            if (!RecentCanvasInfo.IsEmpty)
            {
                _sizeNotEmptyEvent.TrySet();
            }

            CallValidRenderOnce();
        }

        private readonly DrawingGroup _drawingGroup = new DrawingGroup();

        private readonly TaskCompletionEvent _renderSyncWaiter = new TaskCompletionEvent();

        private readonly TaskCompletionEvent _sizeNotEmptyEvent = new TaskCompletionEvent();

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

        private void BuildPipeline(GLContextBinding contextBinding, IRenderCanvas canvas, IFrameBuffer frameBuffer,
            out ITargetBlock<RenderArgs> targetBlock, out IDataflowBlock dataflowBlock)
        {
            // contextBinding.BindNull();
            var renderBlock = new TransformBlock<RenderArgs, FrameArgs>(
                args => { return frameBuffer.ReadFrames(args); },
                new ExecutionDataflowBlockOptions()
                {
                    SingleProducerConstrained = true,
                    TaskScheduler = new GLContextTaskScheduler(contextBinding, _debugProc),
                    MaxDegreeOfParallelism = 1,
                });
            var frameBlock = new TransformBlock<FrameArgs, CanvasArgs>(args => { return canvas.Flush(args); },
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            renderBlock.LinkTo(frameBlock);
            var canvasBlock = new ActionBlock<CanvasArgs>((args =>
            {
                if (args == null)
                {
                    return;
                }

                bool commit;
                using (var drawingContext = _drawingGroup.Open())
                {
                    commit = canvas.Commit(drawingContext, args);
                }

                if (commit)
                {
                    this.InvalidateVisual();
                }
            }), new ExecutionDataflowBlockOptions()
            {
                SingleProducerConstrained = true,
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            });
            frameBlock.LinkTo(canvasBlock);
            targetBlock = renderBlock;
            dataflowBlock = canvasBlock;
        }


        protected override void StartRenderProcedure(IWindowInfo windowInfo)
        {
            _endThreadCts = new CancellationTokenSource();
            var procedure = this.RenderProcedure;
            var renderer = this.Renderer;
            var glSettings = this.GlSettings;
            IRenderCanvas uiRenderCanvas = null;
            IFrameBuffer frameBuffer = null;
            IGraphicsContext sharedContext = null;
            GLContextBinding glContextBinding;
            try
            {
                RecentCanvasInfo = glSettings.CreateCanvasInfo(this);
                var mainContext = procedure.Initialize(windowInfo, glSettings);
                glContextBinding = new GLContextBinding(mainContext, windowInfo);
                uiRenderCanvas = procedure.CreateCanvas();
                frameBuffer = procedure.CreateFrameBuffer();
                sharedContext = glSettings.CreateContext(windowInfo, mainContext);
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
                renderer.Initialize(mainContext);
                this.SizeChanged += ThreadOpenTkControl_SizeChanged;
            }
            catch (Exception e)
            {
                OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Inbuilt, e));
                return;
            }


            BuildPipeline(new GLContextBinding(sharedContext, windowInfo), uiRenderCanvas, frameBuffer,
                out var targetBlock, out var dataflowBlock);
            glContextBinding.BindNull();
            _renderTask = Task.Run(async () =>
            {
                using (_endThreadCts)
                {
                    await RenderThread(_endThreadCts.Token, glContextBinding, procedure, renderer, uiRenderCanvas,
                        frameBuffer, targetBlock, dataflowBlock);
                }
            });
        }

        private async Task RenderThread(CancellationToken token, GLContextBinding binding, IRenderProcedure procedure,
            IRenderer renderer, IRenderCanvas uiThreadCanvas, IFrameBuffer frameBuffer,
            ITargetBlock<RenderArgs> targetBlock, IDataflowBlock endBlock)
        {
            #region initialize

            binding.BindCurrentThread();
            try
            {
                if (!renderer.IsInitialized)
                {
                    renderer.Initialize(binding.Context);
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            using (procedure)
            {
                while (!token.IsCancellationRequested)
                {
                    var renderContinuously = IsRenderContinuouslyValue;
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
                        var pixelSize = canvasInfo.GetPixelSize();
                        procedure.SizeFrame(pixelSize);
                        frameBuffer.Release();
                        frameBuffer.Allocate(pixelSize);
                        renderer.Resize(pixelSize);
                    }

                    if (RecentCanvasInfo.IsEmpty)
                    {
                        await _sizeNotEmptyEvent.Wait(binding);
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
                        await binding.Delay(30);
                        continue;
                    }

                    try
                    {
                        OnBeforeRender();
                        procedure.PreRender();
                        renderer.Render(renderEventArgs);
                        // graphicsContext.SwapBuffers(); //swap?
                        if (ShowFps)
                        {
                            _glFps.Increment();
                        }

                        var postRender = procedure.PostRender();
                        OnAfterRender();
                        targetBlock.SendAsync(postRender);
                        // targetBlock.SendAsync(postRender);
                    }
                    catch (Exception exception)
                    {
                        OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Render, exception));
                    }
                    finally
                    {
                    }

                    if (EnableFrameRateLimit)
                    {
                        var renderMinus = FrameGenerateSpan - stopwatch.ElapsedMilliseconds;
                        if (renderMinus > 5)
                        {
                            await binding.Delay((int)renderMinus);
                        }
                        else if (renderMinus > 0)
                        {
                            Thread.Sleep((int)renderMinus);
                        }

                        stopwatch.Restart();
                    }

                    /*if (uiThreadCanvas.IsDirty)
                    {
                        OnUITaskAsync(() =>
                        {
                            using (var drawingContext = _drawingGroup.Open())
                            {
                                uiThreadCanvas.FlushFrame(drawingContext);
                            }

                            this.InvalidateVisual();
                        });
                        if (!uiThreadCanvas.CanAsyncFlush)
                        {
                            //由于wpf的默认刷新率为60，意味着等待延迟最高达16ms，此时上下文切换的开销小于wait
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
                    }*/

                    if (renderContinuously)
                    {
                        //read previous turn before swap buffer
                        frameBuffer.SwapBuffer();
                    }
                }

                targetBlock.Complete();
                frameBuffer.Release();
                renderer.Uninitialize();
                stopwatch.Stop();
            }

            await endBlock.Completion;
        }

        private void CloseRenderThread()
        {
            try
            {
                _endThreadCts.Cancel();
            }
            finally
            {
                _sizeNotEmptyEvent.ForceSet();
                _renderSyncWaiter.ForceSet();
                if (!UserVisible)
                {
                    _userVisibleResetEvent.ForceSet();
                }

                _renderContinuousWaiter.ForceSet();
                _renderTask.Wait();
            }
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

        protected override void Dispose(bool dispose)
        {
            this._controlFps.Dispose();
            this._glFps.Dispose();
            this._userVisibleResetEvent.Dispose();
            this._renderContinuousWaiter.Dispose();
        }
    }
}