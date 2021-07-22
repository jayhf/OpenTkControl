using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform.Windows;

namespace OpenTkControl
{
    /// <summary>
    /// A WPF control that performs all OpenGL rendering on a thread separate from the UI thread to improve performance
    /// </summary>
    public class ThreadOpenTkControl : OpenTkControlBase, IDisposable
    {
        public static readonly DependencyProperty ThreadNameProperty = DependencyProperty.Register(
            nameof(ThreadName), typeof(string), typeof(ThreadOpenTkControl),
            new PropertyMetadata("OpenTk Render Thread"));

        /// <summary>
        /// The name of the background thread that does the OpenGL rendering
        /// </summary>
        public string ThreadName
        {
            get => (string) GetValue(ThreadNameProperty);
            set => SetValue(ThreadNameProperty, value);
        }

        /// <summary>
        /// The Thread object for the rendering thread
        /// </summary>
        private Thread _renderThread;

        /// <summary>
        /// The CTS used to stop the thread when this control is unloaded
        /// </summary>
        private CancellationTokenSource _endThreadCts;

        public ThreadOpenTkControl()
        {
            IsVisibleChanged += (_, args) =>
            {
                if ((bool) args.NewValue)
                {
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                }
                else
                {
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                }
            };
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
            timer = new Timer((state =>
            {
                averageFrame = currentFrame;
                currentFrame = 0;
            }), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        }

        private volatile int currentFrame, averageFrame;

        private Timer timer;

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null && RenderProcedure.IsInitialized)
            {
                CurrentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
            }
        }

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var canvasFrontSource = canvas?.GetFrontSource();
            if (canvasFrontSource != null)
            {
                drawingContext.DrawImage(canvasFrontSource,
                    new Rect(new Size(canvasFrontSource.Width, canvasFrontSource.Height)));
                drawingContext.DrawText(
                    new FormattedText($"fps:{averageFrame}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        mFpsTypeface, 16, brush, 1),
                    new Point(10, 10));
            }

            if (_isWaitingCompletedEvent)
            {
                _renderCompletedResetEvent.Set();
            }
        }

        private async void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                return;
            }

            _lastRenderTime = currentRenderTime.Value;
            if (!_renderThreadStart)
            {
                return;
            }
            InvalidateVisual();
            /*if (await PushRender() != null)
            {
                
            }*/
            /*if (drawingDirective?.ImageSource != null)
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var imageSource = drawingDirective.ImageSource;
                    if (imageSource != null && drawingDirective.CanDraw)
                    {
                        if (drawingDirective.IsNeedTransform)
                        {
                            // Transforms are applied in reverse order
                            drawingContext.PushTransform(drawingDirective
                                .TranslateTransform); // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                            drawingContext.PushTransform(drawingDirective
                                .ScaleTransform); // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg
                            // dpi scaled rectangle from the image
                            var rect = new Rect(0, 0, imageSource.Width, imageSource.Width);
                            drawingContext.DrawImage(imageSource, rect); // Draw the image source 
                            drawingContext.Pop(); // Remove the scale transform
                            drawingContext.Pop(); // Remove the translation transform
                        }
                        else
                        {
                            var rect = new Rect(0, 0, imageSource.Width, imageSource.Height);
                            drawingContext.DrawImage(imageSource, rect); // Draw the image source 
                        }
                    }
                }
            }*/

            /*if (drawingDirective != null && !_renderCompletedResetEvent.WaitOne(0))
            {
                _renderCompletedResetEvent.Set();
            }*/
        }


        protected CanvasInfo CurrentCanvasInfo;

        protected override void OnRenderProcedureChanged()
        {
            if (Dispatcher.Invoke(IsDesignMode))
                return;
            if (this.RenderProcedure != null && IsLoaded)
            {
                StartThread();
            }
        }

        protected override void OnRenderProcedureChanging()
        {
            if (_renderThreadStart)
            {
                CloseThread();
            }
        }

        public Task OnUITask(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        private Typeface mFpsTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold,
            FontStretches.Normal);

        private SolidColorBrush brush = new SolidColorBrush(Colors.DarkOrange);


        /*protected override int VisualChildrenCount { get; } = 1;

        private DrawingVisual _drawingVisual = new DrawingVisual();

        protected override Visual GetVisualChild(int index)
        {
            return _drawingVisual;
        }*/

        private volatile bool _isWaitingRenderEvent;

        private volatile bool _isWaitingCompletedEvent;

        private readonly ManualResetEvent _renderingResetEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private TaskCompletionSource<DrawingDirective> _imageSourceCompletionSource = null;

        public Task<DrawingDirective> PushRender()
        {
            if (_isWaitingRenderEvent)
            {
                _imageSourceCompletionSource = new TaskCompletionSource<DrawingDirective>();
                _renderingResetEvent.Set();
                return _imageSourceCompletionSource.Task;
            }
            else
            {
                return Task.FromResult(default(DrawingDirective));
            }
        }

        private volatile bool _renderThreadStart = false;

        private IRenderCanvas canvas;

        public void StartThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

            RenderProcedure.Buffer.Create(new CanvasInfo(0, 0, 1, 1));
            canvas = RenderProcedure.Buffer;
            _renderThreadStart = true;
            _endThreadCts = new CancellationTokenSource();
            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = ThreadName
            };
            _renderThread.Start(_endThreadCts.Token);
        }

        private void CloseThread()
        {
            _renderThreadStart = false;
            _renderingResetEvent.Set();
            _endThreadCts.Cancel();
            _renderThread.Join();
            _endThreadCts.Dispose();
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
            if (!_renderThreadStart && RenderProcedure != null)
            {
                StartThread();
            }
        }


        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);
            //unload don't close thread
        }

        private DebugProc _debugProc;


        /// <summary>
        /// The function that the thread runs to render the control
        /// </summary>
        /// <param name="boxedToken"></param>
        private void RenderThread(object boxedToken)
        {
            var token = (CancellationToken) boxedToken;
            _debugProc = Callback;
            RenderProcedure.Initialize(WindowInfo);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            OnUITask(() => { CurrentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this); }).Wait(token);
            RenderProcedure.SetSize(CurrentCanvasInfo);
            var canvasInfo = CurrentCanvasInfo;
            using (RenderProcedure)
            {
                WaitHandle[] renderHandles = {token.WaitHandle, _renderingResetEvent};
                WaitHandle[] drawHandles = {token.WaitHandle, _renderCompletedResetEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(CurrentCanvasInfo))
                    {
                        canvasInfo = CurrentCanvasInfo;
                        OnUITask(() => { canvas.Create(CurrentCanvasInfo); }).Wait(token);
                        RenderProcedure.SetSize(CurrentCanvasInfo);
                    }
                    if (RenderProcedure.CanRender)
                    {
                        DrawingDirective drawingDirective = null;
                        Exception exception = null;
                        try
                        {
                            OnUITask(() => RenderProcedure.Begin()).Wait(token);
                            Interlocked.Increment(ref currentFrame);
                            drawingDirective = RenderProcedure.Render();
                            OnUITask(() => RenderProcedure.End()).Wait(token);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                        finally
                        {
                            /*_isWaitingRenderEvent = true;
                            WaitHandle.WaitAny(renderHandles);
                            _isWaitingRenderEvent = false;
                            _renderingResetEvent.Reset();*/
                        }

                        /*if (exception != null)
                        {
                            _imageSourceCompletionSource.SetException(exception);
                        }
                        else
                        {
                            _imageSourceCompletionSource.SetResult(drawingDirective);
                        }*/


                        if (drawingDirective != null)
                        {
                            //不允许异步填充
                            _isWaitingCompletedEvent = true;
                            WaitHandle.WaitAny(drawHandles);
                            _isWaitingCompletedEvent = false;
                            _renderCompletedResetEvent.Reset();
                        }
                        RenderProcedure.SwapBuffer();
                    }
                }
            }
        }

        public void Dispose()
        {
            CloseThread();
            _renderingResetEvent?.Dispose();
        }
    }
}