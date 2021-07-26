using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform.Windows;

namespace OpenTkControl
{
/*candidate approach: use rendertargetbitmap*/

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

        private readonly Fraps _openglFraps = new Fraps();

        protected CanvasInfo RecentCanvasInfo;

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        private Fraps _controlFraps = new Fraps();

        public ThreadOpenTkControl() : base()
        {
            IsVisibleChanged += (_, args) =>
            {
                if ((bool) args.NewValue)
                {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else
                {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };
            // IsVisibleChanged += (_, args) => { _visible = (bool) args.NewValue; };
            this.SizeChanged += ThreadOpenTkControl_SizeChanged;
        }


        private void OnCompTargetRender(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if (currentRenderTime == _lastRenderTime)
            {
                return;
            }

            Debug.WriteLine($"fire {DateTime.Now}");
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


        private readonly DrawingVisual _drawingCopy = new DrawingVisual();

        /// <summary>
        /// d3d image maybe flicker when 
        /// </summary>
        private volatile bool _isWaitingForSync = true;

        protected override void OnRender(DrawingContext drawingContext)
        {
            Debug.WriteLine($"render {DateTime.Now}");
            drawingContext.DrawDrawing(_drawingCopy.Drawing);
            /*var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                drawingContext.DrawImage(frontBuffer.ImageSource, new Rect(new Point(), this.RenderSize));
            }*/

            /*var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                var imageSource = frontBuffer.ImageSource;
                drawingContext.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));
            }*/

            /*drawingContext.DrawImage(renderTargetBitmap,
                new Rect(new Size(renderTargetBitmap.Width, renderTargetBitmap.Width)));*/
            // drawingContext.DrawImage(bitmap, new Rect(new Size(bitmap.Width, bitmap.Height)));
            /*var frontBuffer = RenderProcedure.GetFrontBuffer();
            if (frontBuffer.IsAvailable)
            {
                var imageSource = frontBuffer.ImageSource;
                drawingContext.DrawImage(imageSource, new Rect(new Size(imageSource.Width, imageSource.Height)));
            }*/

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


        protected override void OnRenderProcedureChanged()
        {
            if (Dispatcher.Invoke(IsDesignMode))
                return;
            if (this.RenderProcedure == null)
            {
                CloseThread();
            }

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

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private volatile bool _renderThreadStart = false;

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
            if (!_renderThreadStart && RenderProcedure != null)
            {
                StartThread();
            }
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
            OnUITask(() =>
            {
                RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SizeCanvas(RecentCanvasInfo);
            }).Wait(token);
            RenderProcedure.SizeFrame(RecentCanvasInfo);
            var canvasInfo = RecentCanvasInfo;
            using (RenderProcedure)
            {
                WaitHandle[] drawHandles = {token.WaitHandle, _renderCompletedResetEvent};
                while (!token.IsCancellationRequested)
                {
                    if (!canvasInfo.Equals(RecentCanvasInfo))
                    {
                        canvasInfo = RecentCanvasInfo;
                        OnUITask(() => { RenderProcedure.SizeCanvas(RecentCanvasInfo); }).Wait(token);
                        RenderProcedure.SizeFrame(RecentCanvasInfo);
                    }

                    if (RenderProcedure.ReadyToRender)
                    {
                        try
                        {
                            OnUITask(() => RenderProcedure?.Begin()).Wait(token);
                            RenderProcedure.Render();
                            if (ShowFps)
                            {
                                _openglFraps.Increment();
                            }

                            OnUITask(() => RenderProcedure?.End()).Wait(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        finally
                        {
                        }

                        OnUITask(() =>
                        {
                            var frontBuffer = RenderProcedure.GetFrontBuffer();
                            if (frontBuffer.IsAvailable)
                            {
                                using (var drawingContext = _drawingCopy.RenderOpen())
                                {
                                    var imageSource = frontBuffer.ImageSource;
                                    drawingContext.DrawImage(imageSource, new Rect(this.RenderSize));
                                }
                            }
                        });
                        
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
                }
            }
        }

        public void StartThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

            _openglFraps.Start();
            _controlFraps.Start();
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
            try
            {
                _endThreadCts.Cancel();
            }
            catch (Exception e)
            {
            }

            _renderThread.Join();
            _endThreadCts.Dispose();
        }

        public void Dispose()
        {
            this._controlFraps.Dispose();
            this._openglFraps.Dispose();
            CloseThread();
        }
    }
}