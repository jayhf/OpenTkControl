using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
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

        private readonly FpsCounter _glFps = new FpsCounter() {Title = "GLFps"};

        private readonly FpsCounter _controlFps = new FpsCounter() {Title = "ControlFps"};

        protected volatile RenderTargetInfo RecentTargetInfo = new RenderTargetInfo(0, 0, 96, 96);

        private readonly EventWaiter _userVisibleResetEvent = new EventWaiter();

        public ThreadOpenTkControl() : base()
        {
            _debugProc = Callback;
        }

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            var renderingTime = ((RenderingEventArgs) e)?.RenderingTime;
            if (renderingTime == _lastRenderTime)
            {
                return;
            }

            _lastRenderTime = renderingTime.Value;
            if (_useSemaphore && _semaphoreSlim.CurrentCount > 2)
            {
                return;
            }

            this.InvalidateVisual();
        }


        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecentTargetInfo = this.RenderSetting.CreateCanvasInfo(this);
            if (!RecentTargetInfo.IsEmpty)
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
            try
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
            finally
            {
                if (_useSemaphore)
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        private bool _useSemaphore = false;

        private bool _internalTrigger = false;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 3);

        private Pipeline<RenderArgs> BuildPipeline(TaskScheduler glContextTaskScheduler, IRenderCanvas canvas,
            IRenderBuffer renderBuffer, TaskScheduler uiScheduler)
        {
            // run on gl thread, read buffer from pbo.
            var renderBlock = new TransformBlock<RenderArgs, FrameArgs>(
                args =>
                {
                    if (ShowFps)
                    {
                        _glFps.Increment();
                    }

                    return renderBuffer.ReadFrames(args);
                },
                new ExecutionDataflowBlockOptions()
                {
                    SingleProducerConstrained = true,
                    TaskScheduler = glContextTaskScheduler,
                    MaxDegreeOfParallelism = 1,
                    // BoundedCapacity = 10,
                });
            //copy buffer to image source
            var frameBlock = new TransformBlock<FrameArgs, CanvasArgs>(args =>
                {
                    if (args == null)
                    {
                        return null;
                    }

                    canvas.Swap();

                    return canvas.Flush(args);
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1,
                    SingleProducerConstrained = true,
                    // BoundedCapacity = 10,
                });
            renderBlock.LinkTo(frameBlock);
            //call render 
            var canvasBlock = new ActionBlock<CanvasArgs>((args =>
            {
                if (args == null)
                {
                    return;
                }

                bool commit;
                using (var drawingContext = _drawingGroup.Open())
                {
                    commit = args.Commit(drawingContext);
                }

                if (commit)
                {
                    if (_internalTrigger)
                    {
                        this.InvalidateVisual();
                    }

                    // _controlFps.Increment();//when set here, rate will differ from onrender method.
                }
            }), new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = 1,
                SingleProducerConstrained = true,
                TaskScheduler = uiScheduler,
                // BoundedCapacity = 10
            });
            frameBlock.LinkTo(canvasBlock);
            renderBlock.Completion.ContinueWith((task =>
            {
                if (task.IsFaulted)
                {
                    ((IDataflowBlock) frameBlock).Fault(task.Exception);
                }
                else
                {
                    frameBlock.Complete();
                }
            }));
            frameBlock.Completion.ContinueWith((task =>
            {
                if (task.IsFaulted)
                {
                    ((IDataflowBlock) canvasBlock).Fault(task.Exception);
                }
                else
                {
                    canvasBlock.Complete();
                }
            }));
            return new Pipeline<RenderArgs>(renderBlock, canvasBlock);
        }

        private RenderSetting _workingRenderSetting;

        protected override void StartRenderProcedure(IWindowInfo windowInfo)
        {
            _endThreadCts = new CancellationTokenSource();
            var procedure = this.RenderProcedure;
            var renderer = this.Renderer;
            var glSettings = this.GlSettings;
            _workingRenderSetting = this.RenderSetting;
            _useSemaphore = _workingRenderSetting.RenderTactic == RenderTactic.LatencyPriority;
            _internalTrigger = _workingRenderSetting.RenderTrigger == RenderTrigger.Internal;
            RecentTargetInfo = _workingRenderSetting.CreateCanvasInfo(this);
            switch (_workingRenderSetting.RenderTrigger)
            {
                case RenderTrigger.CompositionTarget:
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                    break;
                case RenderTrigger.Internal:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
            var renderCanvas = procedure.CreateCanvas();
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _renderTask = Task.Run(async () =>
            {
                using (_endThreadCts)
                {
                    await RenderThread(_endThreadCts.Token, glSettings, scheduler, procedure, renderer, windowInfo,
                        renderCanvas, _workingRenderSetting.RenderTactic == RenderTactic.LatencyPriority);
                }
            });
        }

        private async Task RenderThread(CancellationToken token, GLSettings glSettings, TaskScheduler taskScheduler,
            IRenderProcedure procedure, IRenderer renderer, IWindowInfo windowInfo, IRenderCanvas uiRenderCanvas,
            bool syncPipeline)
        {
            using (procedure)
            {
                #region initialize

                IRenderBuffer renderBuffer = null;
                GLContextBinding mainContextBinding = null;
                GLContextBinding pboContextBinding = null;
                try
                {
                    mainContextBinding = procedure.Initialize(windowInfo, glSettings);
                    GL.Enable(EnableCap.DebugOutput);
                    GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
                    renderBuffer = procedure.CreateRenderBuffer();
                    pboContextBinding = glSettings.NewBinding(mainContextBinding);
                    pboContextBinding.BindNull();
                }
                catch (Exception e)
                {
                    OnRenderErrorReceived(new RenderErrorArgs(RenderPhase.Inbuilt, e));
                    return;
                }

                #endregion

                using (var glContextTaskScheduler = new GLContextTaskScheduler(pboContextBinding, _debugProc))
                {
                    var pipeline = BuildPipeline(glContextTaskScheduler, uiRenderCanvas, renderBuffer, taskScheduler);
                    mainContextBinding.BindCurrentThread();
                    RenderTargetInfo targetInfo = null;
                    GlRenderEventArgs renderEventArgs = null;
                    if (!renderer.IsInitialized)
                    {
                        renderer.Initialize(mainContextBinding.Context);
                    }

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

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

                        if (!Equals(targetInfo, RecentTargetInfo) && !RecentTargetInfo.IsEmpty)
                        {
                            targetInfo = RecentTargetInfo;
                            renderEventArgs = RecentTargetInfo.GetRenderEventArgs();
                            var pixelSize = RecentTargetInfo.PixelSize;
                            procedure.Apply(RecentTargetInfo);
                            renderer.Resize(pixelSize);
                        }

                        // ReSharper disable once PossibleNullReferenceException
                        if (RecentTargetInfo.IsEmpty)
                        {
                            await _sizeNotEmptyEvent.Wait(mainContextBinding);
                            continue;
                        }

                        if (!renderer.PreviewRender())
                        {
                            await mainContextBinding.Delay(30);
                            continue;
                        }

                        try
                        {
                            OnBeforeRender(renderEventArgs);
                            procedure.PreRender();
                            renderer.Render(renderEventArgs);
                            // graphicsContext.SwapBuffers(); //swap?
                            var postRender = procedure.PostRender();
                            OnAfterRender(renderEventArgs);
                            pipeline.Post(postRender);
                            procedure.Swap();
                            // pipeline.SendAsync(postRender, token).Wait(token);
                            if (syncPipeline)
                            {
                                _semaphoreSlim.Wait(token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
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
                                await mainContextBinding.Delay((int) renderMinus);
                            }
                            else if (renderMinus > 0)
                            {
                                Thread.Sleep((int) renderMinus);
                            }

                            stopwatch.Restart();
                        }
                    }

                    stopwatch.Stop();
                    pipeline.Finish().Wait(CancellationToken.None);
                    renderer.Uninitialize();
                }
            }
        }

        private async Task CloseRenderThread()
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
                await _renderTask;
                switch (_workingRenderSetting.RenderTrigger)
                {
                    case RenderTrigger.CompositionTarget:
                        CompositionTarget.Rendering -= CompositionTarget_Rendering;
                        break;
                    case RenderTrigger.Internal:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override void ResumeRender()
        {
            _renderContinuousWaiter.TrySet();
        }

        protected override async void CloseRenderer()
        {
            await CloseRenderThread();
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
            var targetBitmap = RecentTargetInfo.CreateRenderTargetBitmap();
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
            this._semaphoreSlim.Dispose();
            this._controlFps.Dispose();
            this._glFps.Dispose();
            this._userVisibleResetEvent.Dispose();
            this._renderContinuousWaiter.Dispose();
        }
    }
}