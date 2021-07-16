using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenTkControl
{
    /// <summary>
    /// A WPF control that performs all OpenGL rendering on a thread separate from the UI thread to improve performance
    /// </summary>
    public class ThreadOpenTkControl : OpenTkControlBase
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
        /// This event is set to notify the thread to wake up when the control becomes visible
        /// </summary>
        private readonly ManualResetEvent _becameVisibleEvent = new ManualResetEvent(false);

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
            IsVisibleChanged += OnIsVisibleChanged;
        }

        protected override void OnRenderProcedureChanged()
        {
            throw new NotImplementedException();
        }

        protected override void OnContinuousChanged()
        {
            throw new NotImplementedException();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        public Task RunOnUiThread(Action action)
        {
            return Dispatcher.InvokeAsync(action).Task;
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
            
            _endThreadCts = new CancellationTokenSource();
            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = ThreadName
            };
            _renderThread.Start(_endThreadCts.Token);
        }

        private Task _previousUpdateImageTask;

        protected override async void OnUnloaded(object sender, RoutedEventArgs args)
        {
            try
            {
                
                var previousUpdateImageTask = _previousUpdateImageTask;
                if (_previousUpdateImageTask != null)
                {
                    await previousUpdateImageTask;
                }

                _previousUpdateImageTask = null;
            }
            catch (TaskCanceledException)
            {
            }

            base.OnUnloaded(sender, args);
            _endThreadCts.Cancel();
            _renderThread.Join();
        }

        /// <summary>
        /// Wakes up the thread when the control becomes visible
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">The event arguments about this event</param>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            bool visible = (bool) args.NewValue;

            if (visible)
                _becameVisibleEvent.Set();
        }

        /* if (_frameRateLimit > 0 && _frameRateLimit < 1000)
                         {
                             var now = DateTime.Now;
                             var delayTime = TimeSpan.FromSeconds(1 / _frameRateLimit) - (now - _lastFrameTime);
                             if (delayTime.CompareTo(TimeSpan.Zero) > 0)
                                 return delayTime;
                             _lastFrameTime = now;
                         }
                         else
                         {
                             _lastFrameTime = DateTime.MinValue;
                         }*/

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

            RenderProcedure.Initialize();
            using (RenderProcedure)
            {
                WaitHandle[] notContinuousHandles = {token.WaitHandle};
                WaitHandle[] notVisibleHandles = {token.WaitHandle, _becameVisibleEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!_continuous)
                    {
                        WaitHandle.WaitAny(notContinuousHandles);
                    }
                    else if (!IsVisible)
                    {
                        WaitHandle.WaitAny(notVisibleHandles);
                        _becameVisibleEvent.Reset();

                        if (!_continuous)
                            continue;
                    }

                    if (token.IsCancellationRequested)
                        break;

                    var imageSource = RenderProcedure.Render(out var directive);
                    if (imageSource != null)
                    {
                        RunOnUiThread(() =>
                        {
                            // Transforms are applied in reverse order
                            drawingContext.PushTransform(directive
                                .TranslateTransform); // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                            drawingContext.PushTransform(_framebuffer
                                .FlipYTransform); // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg
                            // dpi scaled rectangle from the image
                            var rect = new Rect(0, 0, _framebuffer.D3dImage.Width, _framebuffer.D3dImage.Height);
                            drawingContext.DrawImage(_framebuffer.D3dImage, rect); // Draw the image source 

                            drawingContext.Pop(); // Remove the scale transform
                            drawingContext.Pop(); // Remove the translation transform
                        })
                    }

                    if (sleepTime.CompareTo(TimeSpan.Zero) > 0)
                        Thread.Sleep(sleepTime);
                }
            }
            //_lastFrameTime = DateTime.MinValue;
        }

        
    }
}