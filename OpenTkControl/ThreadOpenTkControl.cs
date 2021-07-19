using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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

        private Task _lastRenderTask;

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
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null)
            {
                CurrentCanvasInfo = RenderProcedure.Settings.CreateCanvasInfo(this);
            }
        }

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                return;
            }

            _lastRenderTime = currentRenderTime.Value;
            InvalidateVisual();
        }


        protected CanvasInfo CurrentCanvasInfo;


        protected override void OnRenderProcedureChanged()
        {
            if (this.RenderProcedure != null)
            {
                CurrentCanvasInfo = this.RenderProcedure.Settings.CreateCanvasInfo(this);
                StartThread();
            }
        }

        protected override void OnRenderProcedureChanging()
        {
            if (this.RenderProcedure != null)
            {
                CloseThread();
            }
        }

        private DrawingContext _currentDrawingContext;

        public Task OnRenderTask(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        private readonly ManualResetEvent _renderResetEvent = new ManualResetEvent(false);

        /*private TaskCompletionSource<Tuple<ImageSource, DrawingDirective>> renderCompletionSource =
            new TaskCompletionSource<Tuple<ImageSource, DrawingDirective>>();*/

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            _currentDrawingContext = drawingContext;
            if (!_renderResetEvent.WaitOne(0))
            {
                _renderResetEvent.Set();
            }
        }


        private volatile bool renderThreadStart = false;

        public void StartThread()
        {
            if (renderThreadStart)
            {
                return;
            }

            renderThreadStart = true;
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
            renderThreadStart = false;
            _renderResetEvent.Set();
            _endThreadCts.Cancel();
            _renderThread.Join();
            _endThreadCts.Dispose();
        }


        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);
            CloseThread();
        }

        /// <summary>
        /// The function that the thread runs to render the control
        /// </summary>
        /// <param name="boxedToken"></param>
        private void RenderThread(object boxedToken)
        {
#if DEBUG
            // Don't render in design mode to prevent errors from calling OpenGL API methods.
            if (Dispatcher.Invoke(IsDesignMode))
                return;
#endif

            var token = (CancellationToken) boxedToken;
            RenderProcedure.Initialize(WindowInfo);
            var canvasInfo = CurrentCanvasInfo;
            RenderProcedure.SizeCanvas(CurrentCanvasInfo);
            using (RenderProcedure)
            {
                WaitHandle[] renderHandles = {token.WaitHandle, _renderResetEvent};
                while (!token.IsCancellationRequested)
                {
                    WaitHandle.WaitAny(renderHandles);
                    _renderResetEvent.Reset();
                    if (token.IsCancellationRequested)
                        break;
                    if (!canvasInfo.Equals(CurrentCanvasInfo))
                    {
                        RenderProcedure.SizeCanvas(canvasInfo);
                    }

                    var imageSource = RenderProcedure.Render(out var directive);
                    if (imageSource != null)
                    {
                        _lastRenderTask = OnRenderTask(() =>
                        {
                            if (directive.Equals(DrawingDirective.None))
                            {
                                var rect = new Rect(0, 0, imageSource.Width, imageSource.Height);
                                _currentDrawingContext.DrawImage(imageSource, rect); // Draw the image source 
                            }
                            else
                            {
                                // Transforms are applied in reverse order
                                _currentDrawingContext.PushTransform(directive
                                    .TranslateTransform); // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                                _currentDrawingContext.PushTransform(directive
                                    .ScaleTransform); // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg
                                // dpi scaled rectangle from the image
                                var rect = new Rect(0, 0, imageSource.Width, imageSource.Height);
                                _currentDrawingContext.DrawImage(imageSource, rect); // Draw the image source 
                                _currentDrawingContext.Pop(); // Remove the scale transform
                                _currentDrawingContext.Pop(); // Remove the translation transform
                            }
                        });
                    }

                    if (!directive.IsOutputAsync)
                    {
                        _lastRenderTask.Wait(token);
                    }
                }
            }
        }

        public void Dispose()
        {
            CloseThread();
            _renderResetEvent?.Dispose();
        }
    }
}