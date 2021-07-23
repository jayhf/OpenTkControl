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

        private DebugProc _debugProc;

        private bool visible;

        public ThreadOpenTkControl()
        {
            IsVisibleChanged += (_, args) => { visible = (bool) args.NewValue; };
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

            if (!_renderThreadStart)
            {
                drawingContext.DrawText(new FormattedText($"loading", CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        mFpsTypeface, 26, brush, 1),
                    new Point(100, 100));
                return;
            }

            Debug.WriteLine("render");

            if (RenderCommand == Idle)
            {
                return;
            }

            RenderCommand = Idle;
            var renderCanvas = _doubleBuffer.GetFrontBuffer();
            var canvasFrontSource = renderCanvas.GetSource();
            if (canvasFrontSource != null)
            {
                drawingContext.DrawImage(canvasFrontSource,
                    new Rect(new Size(canvasFrontSource.Width, canvasFrontSource.Height)));
                drawingContext.DrawImage(canvasFrontSource,
                    new Rect(new Size(canvasFrontSource.Width, canvasFrontSource.Height)));
                drawingContext.DrawText(
                    new FormattedText($"fps:{averageFrame}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        mFpsTypeface, 16, brush, 1),
                    new Point(10, 10));
            }

            if (isWaiting)
            {
                _renderCompletedResetEvent.Set();
            }
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

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private volatile bool _renderThreadStart = false;

        private IDoubleBuffer _doubleBuffer;

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
                WaitHandle[] drawHandles = {token.WaitHandle, _renderCompletedResetEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(CurrentCanvasInfo))
                    {
                        canvasInfo = CurrentCanvasInfo;
                        OnUITask(() => { _doubleBuffer.Create(CurrentCanvasInfo); }).Wait(token);
                        RenderProcedure.SetSize(CurrentCanvasInfo);
                    }

                    if (RenderProcedure.CanRender)
                    {
                        try
                        {
                            OnUITask(() => RenderProcedure.Begin()).Wait(token);
                            RenderProcedure.Render();
                            OnUITask(() => RenderProcedure.End()).Wait(token);
                            Interlocked.Increment(ref currentFrame);
                            if (RenderCommand == Run)
                            {
                                isWaiting = true;
                                WaitHandle.WaitAny(drawHandles);
                                _renderCompletedResetEvent.Reset();
                                isWaiting = false;
                            }

                            RenderProcedure.SwapBuffer();
                        }
                        finally
                        {
                        }

                        if (_doubleBuffer?.GetFrontBuffer().GetSource() != null)
                        {
                            RenderCommand = Run;
                            OnUITask(() => { InvalidateVisual(); });
                        }
                    }
                }
            }
        }

        private volatile int RenderCommand = Idle;

        private const int Run = 1;
        private const int Idle = 0;

        private volatile bool isWaiting = false;

        public void StartThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

            RenderProcedure.Buffer.Create(new CanvasInfo(0, 0, 1, 1));
            _doubleBuffer = RenderProcedure.Buffer;
            _endThreadCts = new CancellationTokenSource();
            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = ThreadName
            };
            _renderThread.Start(_endThreadCts.Token);
            _renderThreadStart = true;
        }

        private void CloseThread()
        {
            _renderThreadStart = false;
            _endThreadCts.Cancel();
            _renderThread.Join();
            _endThreadCts.Dispose();
        }

        public void Dispose()
        {
            CloseThread();
        }
    }
}