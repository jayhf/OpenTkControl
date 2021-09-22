using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private Thread _renderThread;

        /// <summary>
        /// The CTS used to stop the thread when this control is unloaded
        /// </summary>
        private CancellationTokenSource _endThreadCts;

        private DebugProc _debugProc;

        private readonly Fraps _openglFraps = new Fraps() { Name = "GLFps" };

        private readonly Fraps _controlFraps = new Fraps() { Name = "WindowFps" };

        protected CanvasInfo RecentCanvasInfo;

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private volatile bool _renderThreadStart = false;

        private readonly ManualResetEvent _renderLoopResetEvent = new ManualResetEvent(false);

        public ThreadOpenTkControl() : base()
        {
            IsVisibleChanged += (_, args) =>
            {
                var newValue = (bool)args.NewValue;
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
        private volatile bool _isWaitingForSync = true;

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
                _renderCompletedResetEvent.Set();
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
        private void RenderThread(object boxedToken)
        {
            var token = (CancellationToken)boxedToken;
            _debugProc = Callback;
            RenderProcedure.Initialize(_windowInfo);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            OnUITask((() => { RecentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this); })).Wait(token);

            RenderProcedure.SizeCanvas(RecentCanvasInfo);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
            using (RenderProcedure)
            {
                DrawingVisual cacheVisual = new DrawingVisual();
                WaitHandle[] drawHandles = { token.WaitHandle, _renderCompletedResetEvent };
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(RecentCanvasInfo))
                    {
                        canvasInfo = RecentCanvasInfo;
                        RenderProcedure.SizeCanvas(RecentCanvasInfo);
                        RenderProcedure.SizeFrame(RecentCanvasInfo);
                    }

                    if (RenderProcedure.ReadyToRender)
                    {
                        try
                        {
                            RenderProcedure?.Begin();
                            RenderProcedure.Render();
                            if (ShowFps)
                            {
                                _openglFraps.Increment();
                            }

                            RenderProcedure?.End();
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        finally
                        {
                        }

                        var frontBuffer = RenderProcedure.GetFrontBuffer();
                        if (frontBuffer.IsAvailable)
                        {
                            var imageSource = frontBuffer.ImageSource;
                            if (imageSource.CanFreeze)
                            {
                                var valueAsFrozen = imageSource.GetCurrentValueAsFrozen() as ImageSource;
                                OnUITask(() =>
                                {
                                    using (var drawingContext = drawingGroup.Open())
                                    {
                                        drawingContext.DrawImage(valueAsFrozen,RecentCanvasInfo.Rect);
                                    }
                                });
                            }
                            else
                            {
                                using (var drawingContext = cacheVisual.RenderOpen())
                                {
                                    var rectangle = RecentCanvasInfo.Rect;
                                    if (drawingDirective.IsNeedTransform)
                                    {
                                        drawingContext.PushTransform(transformGroup);
                                        drawingContext.DrawImage(imageSource, rectangle);
                                        drawingContext.Pop();
                                    }
                                    else
                                    {
                                        drawingContext.DrawImage(imageSource, rectangle);
                                    }
                                }
                                var renderTargetBitmap = new RenderTargetBitmap(RecentCanvasInfo.ActualWidth,
                                    RecentCanvasInfo.ActualHeight, RecentCanvasInfo.DpiScaleX * 96,
                                    RecentCanvasInfo.DpiScaleY * 96, PixelFormats.Pbgra32);
                                renderTargetBitmap.Render(cacheVisual);
                                var currentValueAsFrozen = (ImageSource)renderTargetBitmap.GetCurrentValueAsFrozen();
                                OnUITask(() =>
                                {
                                    drawingGroup.Children.Clear();
                                    drawingGroup.Children.Add(new ImageDrawing(currentValueAsFrozen,
                                        RecentCanvasInfo.Rect));
                                });

                            }
                        }

                        _isWaitingForSync = true;
                        WaitHandle.WaitAny(drawHandles);
                        _renderCompletedResetEvent.Reset();
                        _isWaitingForSync = false;
                        RenderProcedure.SwapBuffer();
                    }
                    else
                    {
                        Thread.Sleep(30);
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
            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            };
            _renderThread.Start(_endThreadCts.Token);
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
            catch (Exception e)
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

                _renderThread.Join();
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