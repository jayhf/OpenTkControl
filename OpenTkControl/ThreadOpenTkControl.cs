using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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
        }

        private void ThreadOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null)
            {
                CurrentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
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
                CurrentCanvasInfo = this.RenderProcedure.GlSettings.CreateCanvasInfo(this);
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

        public Task OnRenderTask(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        private readonly ManualResetEvent _renderingResetEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        /*private TaskCompletionSource<Tuple<ImageSource, DrawingDirective>> renderCompletionSource =
            new TaskCompletionSource<Tuple<ImageSource, DrawingDirective>>();*/

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var directive = PushRender().GetAwaiter().GetResult();
            var imageSource = directive?.ImageSource;
            if (imageSource!= null)
            {
                if (directive.IsNeedTransform)
                {
                    // Transforms are applied in reverse order
                    drawingContext.PushTransform(directive
                        .TranslateTransform); // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                    drawingContext.PushTransform(directive
                        .ScaleTransform); // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg
                    // dpi scaled rectangle from the image
                    var rect = new Rect(0, 0, imageSource.Width, imageSource.Height);
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
            
            if (directive != null && _renderCompletedResetEvent.WaitOne(0))
            {
                _renderCompletedResetEvent.Set();
            }
        }

        private TaskCompletionSource<DrawingDirective> _imageSourceCompletionSource = null;

        public Task<DrawingDirective> PushRender()
        {
            _imageSourceCompletionSource = new TaskCompletionSource<DrawingDirective>();
            if (!_renderingResetEvent.WaitOne(0))
            {
                _renderingResetEvent.Set();
            }

            return _imageSourceCompletionSource.Task;
        }

        private volatile bool _renderThreadStart = false;

        public void StartThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

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
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs args)
        {
            base.OnUnloaded(sender, args);
            // CloseThread();
        }

        private DebugProc _debugProc;

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
            _debugProc = Callback;
            RenderProcedure.Initialize(WindowInfo);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            var canvasInfo = CurrentCanvasInfo;
            RenderProcedure.SizeCanvas(CurrentCanvasInfo);
            using (RenderProcedure)
            {
                WaitHandle[] renderHandles = {token.WaitHandle, _renderingResetEvent};
                WaitHandle[] drawHandles = {token.WaitHandle, _renderCompletedResetEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(CurrentCanvasInfo))
                    {
                        canvasInfo = CurrentCanvasInfo;
                        RenderProcedure.SizeCanvas(CurrentCanvasInfo);
                    }

                    DrawingDirective directive = null;
                    Exception exception = null;
                    try
                    {
                        directive = RenderProcedure.Render();
                        var directiveImageSource = directive?.ImageSource;
                        if (directiveImageSource != null)
                        {
                            directive.ImageSource = (ImageSource)directiveImageSource.GetCurrentValueAsFrozen();
                        }

                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    finally
                    {
                        WaitHandle.WaitAny(renderHandles);
                        _renderingResetEvent.Reset();
                    }

                    if (exception != null)
                    {
                        _imageSourceCompletionSource.SetException(exception);
                    }
                    else
                    {
                        
                        _imageSourceCompletionSource.SetResult(directive);
                    }

                    if (token.IsCancellationRequested)
                        break;

                    if (directive != null && !directive.IsOutputAsync)
                    {
                        //不允许异步渲染
                        WaitHandle.WaitAny(drawHandles);
                        _renderCompletedResetEvent.Reset();
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