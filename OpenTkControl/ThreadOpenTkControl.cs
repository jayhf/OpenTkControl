﻿using System;
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

            if (await PushRender() != null)
            {
                InvalidateVisual();
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

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_imageSource != null)
            {
                drawingContext.DrawImage(_imageSource, new Rect(new Size(_imageSource.Width, _imageSource.Height)));
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

        private volatile bool _isWaitingRenderEvent;

        private volatile bool _isWaitingCompletedEvent;

        private readonly ManualResetEvent _renderingResetEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent _renderCompletedResetEvent = new ManualResetEvent(false);

        private TaskCompletionSource<DrawingDirective> _imageSourceCompletionSource = null;

        public void CallRender()
        {
            if (!_renderingResetEvent.WaitOne(0))
            {
                _renderingResetEvent.Set();
            }
        }

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

        private ImageSource _imageSource;

        public void StartThread()
        {
            if (_renderThreadStart)
            {
                return;
            }

            RenderProcedure.Canvas.Create(new CanvasInfo(0, 0, 1, 1));
            _imageSource = RenderProcedure.Canvas.Canvas;
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
            var canvas = RenderProcedure.Canvas;
            OnUITask(() => { CurrentCanvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this); }).Wait(token);
            RenderProcedure.SizeCanvas(CurrentCanvasInfo);
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
                        // OnUITask(() => { canvas.Create(CurrentCanvasInfo); }).Wait(token);
                        RenderProcedure.SizeCanvas(CurrentCanvasInfo);
                    }

                    if (RenderProcedure.CanRender)
                    {
                        DrawingDirective drawingDirective = null;
                        Exception exception = null;
                        try
                        {
                            OnUITask(() => canvas.Begin()).Wait(token);
                            Interlocked.Increment(ref currentFrame);
                            drawingDirective = RenderProcedure.Render();
                            OnUITask(() => canvas.End()).Wait(token);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                        finally
                        {
                            _isWaitingRenderEvent = true;
                            WaitHandle.WaitAny(renderHandles);
                            _isWaitingRenderEvent = false;
                            _renderingResetEvent.Reset();
                        }

                        if (exception != null)
                        {
                            _imageSourceCompletionSource.SetException(exception);
                        }
                        else
                        {
                            _imageSourceCompletionSource.SetResult(drawingDirective);
                        }

                        if (token.IsCancellationRequested)
                            break;

                        if (drawingDirective != null)
                        {
                            //不允许异步渲染
                            _isWaitingCompletedEvent = true;
                            WaitHandle.WaitAny(drawHandles);
                            _isWaitingCompletedEvent = false;
                            _renderCompletedResetEvent.Reset();
                        }
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