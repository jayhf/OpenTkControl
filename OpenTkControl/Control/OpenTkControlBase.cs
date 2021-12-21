using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Bitmap;
using OpenTkWPFHost.Configuration;
using OpenTkWPFHost.Core;
using WindowState = System.Windows.WindowState;

namespace OpenTkWPFHost.Control
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class OpenTkControlBase : FrameworkElement, IDisposable
    {
        /// <summary>
        /// Initialize the OpenTk Toolkit
        /// </summary>
        static OpenTkControlBase()
        {
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative
            });
        }

        public event EventHandler<OpenGlErrorArgs> OpenGlErrorReceived;

        public event EventHandler<RenderErrorArgs> RenderErrorReceived;

        /// <summary>
        /// renderer is ready
        /// </summary>
        public event EventHandler<GlRenderEventArgs> BeforeRender;

        /// <summary>
        /// after successfully render
        /// </summary>
        public event EventHandler<GlRenderEventArgs> AfterRender;

        /// <summary>
        /// Called whenever an exception occurs during initialization, rendering or deinitialization
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> ExceptionOccurred;

        public static readonly DependencyProperty GlSettingsProperty = DependencyProperty.Register(
            "GlSettings", typeof(GLSettings), typeof(OpenTkControlBase), new PropertyMetadata(new GLSettings()));

        public GLSettings GlSettings
        {
            get { return (GLSettings) GetValue(GlSettingsProperty); }
            set { SetValue(GlSettingsProperty, value); }
        }

        public static readonly DependencyProperty RenderSettingProperty = DependencyProperty.Register(
            "RenderSetting", typeof(RenderSetting), typeof(OpenTkControlBase),
            new PropertyMetadata(new RenderSetting()));

        public RenderSetting RenderSetting
        {
            get { return (RenderSetting) GetValue(RenderSettingProperty); }
            set { SetValue(RenderSettingProperty, value); }
        }

        public static readonly DependencyProperty RenderProcedureProperty = DependencyProperty.Register(
            "RenderProcedure", typeof(IRenderProcedure), typeof(OpenTkControlBase),
            new PropertyMetadata(new BitmapProcedure()));

        /// <summary>
        /// must be set before render start
        /// </summary>
        public IRenderProcedure RenderProcedure
        {
            get { return (IRenderProcedure) GetValue(RenderProcedureProperty); }
            set { SetValue(RenderProcedureProperty, value); }
        }

        /// <summary>
        /// renderer 
        /// </summary>
        public static readonly DependencyProperty RendererProperty = DependencyProperty.Register(
            "Renderer", typeof(IRenderer), typeof(OpenTkControlBase), new PropertyMetadata(default(IRenderer)));

        /// <summary>
        /// must be set before render start
        /// </summary>
        public IRenderer Renderer
        {
            get { return (IRenderer) GetValue(RendererProperty); }
            set { SetValue(RendererProperty, value); }
        }

        public static readonly DependencyProperty IsShowFpsProperty =
            DependencyProperty.Register("IsShowFps", typeof(bool), typeof(OpenTkControlBase),
                new PropertyMetadata(false));

        public bool IsShowFps
        {
            get { return (bool) GetValue(IsShowFpsProperty); }
            set { SetValue(IsShowFpsProperty, value); }
        }

        public static readonly DependencyProperty MaxFrameRateProperty = DependencyProperty.Register(
            "MaxFrameRate", typeof(int), typeof(OpenTkControlBase), new PropertyMetadata(-1));

        /// <summary>
        /// if lower than 0, infinity
        /// </summary>
        public int MaxFrameRate
        {
            get { return (int) GetValue(MaxFrameRateProperty); }
            set { SetValue(MaxFrameRateProperty, value); }
        }

        public static readonly DependencyProperty IsRendererOpenedProperty = DependencyProperty.Register(
            "IsRendererOpened", typeof(bool), typeof(OpenTkControlBase), new PropertyMetadata(default(bool)));


        /// <summary>
        /// 渲染过程是否已被打开
        /// </summary>
        public bool IsRendererOpened
        {
            get { return (bool) GetValue(IsRendererOpenedProperty); }
            protected set { SetValue(IsRendererOpenedProperty, value); }
        }

        /// <summary>
        /// control renderer lifecycle
        /// </summary>
        public RendererProcedureLifeCycle RendererProcedureLifeCycle
        {
            get { return (RendererProcedureLifeCycle) GetValue(RendererProcedureLifeCycleProperty); }
            set { SetValue(RendererProcedureLifeCycleProperty, value); }
        }

        /// <summary>
        /// default is bound to window as wpf window cannot reuse after close
        /// </summary>
        public static readonly DependencyProperty RendererProcedureLifeCycleProperty = DependencyProperty.Register(
            "RendererProcedureLifeCycle", typeof(RendererProcedureLifeCycle), typeof(OpenTkControlBase),
            new PropertyMetadata(RendererProcedureLifeCycle.BoundToWindow));

        public static readonly DependencyProperty IsAutoAttachProperty = DependencyProperty.Register(
            "IsAutoAttach", typeof(bool), typeof(OpenTkControlBase), new PropertyMetadata(false));

        /// <summary>
        /// if set to true, will start rendering when this element is loaded.
        /// </summary>
        public bool IsAutoAttach
        {
            get { return (bool) GetValue(IsAutoAttachProperty); }
            set { SetValue(IsAutoAttachProperty, value); }
        }

        public static readonly DependencyProperty IsRenderContinuouslyProperty = DependencyProperty.Register(
            "IsRenderContinuously", typeof(bool), typeof(OpenTkControlBase), new PropertyMetadata(true));

        /// <summary>
        /// whether render continuous, if not need to manually call update 
        /// </summary>
        public bool IsRenderContinuously
        {
            get { return (bool) GetValue(IsRenderContinuouslyProperty); }
            set { SetValue(IsRenderContinuouslyProperty, value); }
        }

        public static readonly DependencyProperty IsUserVisibleProperty = DependencyProperty.Register(
            "IsUserVisible", typeof(bool), typeof(OpenTkControlBase), new PropertyMetadata(default(bool)));

        /// <summary>
        /// a combination of window closed/minimized, control unloaded/visibility status
        /// indicate whether user can see the control,
        /// </summary>
        public bool IsUserVisible
        {
            get { return (bool) GetValue(IsUserVisibleProperty); }
            protected set { SetValue(IsUserVisibleProperty, value); }
        }


        protected volatile bool UserVisible;

        private WindowState _windowState;

        /// <summary>
        /// window visibility
        /// </summary>
        private bool _isWindowVisible;

        private bool _isWindowLoaded;

        private bool _isWindowClosed;

        private bool _isControlLoaded;

        private bool _isControlVisible;

        private IWindowInfo _windowInfo;

        /// <summary>
        /// True if OnLoaded has already been called
        /// </summary>
        private bool _alreadyLoaded;

        protected volatile bool IsRenderContinuouslyValue;

        protected volatile int FrameGenerateSpan;

        protected volatile bool EnableFrameRateLimit;

        protected bool ShowFps = (bool) IsShowFpsProperty.DefaultMetadata.DefaultValue;

        /// <summary>
        /// Creates the <see cref="OpenTkControlBase"/>/>
        /// </summary>
        protected OpenTkControlBase()
        {
            //used for fast read and thread safe
            DependencyPropertyDescriptor.FromProperty(IsShowFpsProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => ShowFps = IsShowFps);
            DependencyPropertyDescriptor.FromProperty(IsRenderContinuouslyProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) =>
                {
                    this.IsRenderContinuouslyValue = IsRenderContinuously;
                    if (this.IsRenderContinuouslyValue)
                    {
                        ResumeRender();
                    }
                });
            DependencyPropertyDescriptor.FromProperty(MaxFrameRateProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this,
                    (sender, args) => { ApplyMaxFrameRate(this.MaxFrameRate); });
            DependencyPropertyDescriptor.FromProperty(IsUserVisibleProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this,
                    (sender, args) => { this.UserVisible = this.IsUserVisible; });
            ApplyMaxFrameRate((int) MaxFrameRateProperty.DefaultMetadata.DefaultValue);
            this.IsRenderContinuouslyValue = (bool) IsRenderContinuouslyProperty.DefaultMetadata.DefaultValue;
            Loaded += (sender, args) =>
            {
                if (_alreadyLoaded)
                    return;
                _alreadyLoaded = true;
                OnLoaded(sender, args);
            };
            Unloaded += (sender, args) =>
            {
                if (!_alreadyLoaded)
                    return;

                _alreadyLoaded = false;
                OnUnloaded(sender, args);
            };
            Application.Current.Exit += (sender, args) =>
            {
                if (RendererProcedureLifeCycle == RendererProcedureLifeCycle.BoundToApplication
                    || this.IsRendererOpened)
                {
                    Close();
                }
                this.Dispose();
            };
            this.IsVisibleChanged += OpenTkControlBase_IsVisibleChanged;
        }

        private void ApplyMaxFrameRate(int maxFrameRate)
        {
            if (maxFrameRate < 1)
            {
                EnableFrameRateLimit = false;
                return;
            }

            EnableFrameRateLimit = true;
            FrameGenerateSpan = (int) (1000d / maxFrameRate);
        }

        /// <summary>
        /// resume render procedure
        /// </summary>
        protected abstract void ResumeRender();

        /// <summary>
        /// manually call render loop regardless of double buffer mechanism
        /// </summary>
        public void CallValidRenderOnce()
        {
            if (!IsRenderContinuouslyValue && IsRendererOpened && UserVisible)
            {
                ResumeRender();
            }
        }

        private void CheckUserVisible()
        {
            this.IsUserVisible = _windowState != WindowState.Minimized && !_isWindowClosed && _isControlVisible &&
                                 _isWindowVisible && _isWindowLoaded && _isControlLoaded;
        }

        private void OpenTkControlBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _isControlVisible = (bool) e.NewValue;
            CheckUserVisible();
        }

        private void HostWindow_StateChanged(object sender, EventArgs e)
        {
            _windowState = ((Window) sender).WindowState;
            CheckUserVisible();
        }

        private void HostWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _isWindowVisible = (bool) e.NewValue;
            CheckUserVisible();
        }

        private void HostWindow_Closed(object sender, EventArgs e)
        {
            _isWindowClosed = true;
            CheckUserVisible();
            if (RendererProcedureLifeCycle == RendererProcedureLifeCycle.BoundToWindow)
            {
                Close();
            }
        }

        /// <summary>
        /// explicitly start render procedure
        /// </summary>
        /// <param name="hostWindow"></param>
        public void Start(Window hostWindow)
        {
            if (RenderProcedure == null)
            {
                throw new NotSupportedException($"Can't start render procedure as {nameof(RenderProcedure)} is null!");
            }

            if (Renderer == null)
            {
                throw new NotSupportedException($"Can't start render procedure as {nameof(Renderer)} is null!");
            }

            if (GlSettings == null)
            {
                throw new NotSupportedException($"Can't start render procedure as {nameof(GlSettings)} is null!");
            }


            if (hostWindow == null)
            {
                throw new ArgumentNullException(nameof(hostWindow));
            }

            if (IsRendererOpened)
            {
                return;
            }

            _isWindowClosed = false;
            _windowState = hostWindow.WindowState;
            _isWindowVisible = hostWindow.IsVisible;
            _isWindowLoaded = hostWindow.IsLoaded;
            _isControlVisible = this.IsVisible;
            _isControlLoaded = this.IsLoaded;
            CheckUserVisible();
            var baseHandle = new WindowInteropHelper(hostWindow).Handle;
            _hwndSource = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);
            this._windowInfo = Utilities.CreateWindowsWindowInfo(_hwndSource.Handle);
            hostWindow.Closed += HostWindow_Closed;
            hostWindow.IsVisibleChanged += HostWindow_IsVisibleChanged;
            hostWindow.StateChanged += HostWindow_StateChanged;
            this.StartRenderProcedure(_windowInfo);
            this.IsRendererOpened = true;
        }

        /// <summary>
        /// explicitly close render procedure, can reopen
        /// <para>will be called in dispose</para>
        /// </summary>
        public void Close()
        {
            if (!IsRendererOpened)
            {
                return;
            }

            CloseRenderer();
            this.IsRendererOpened = false;
        }

        /// <summary>
        /// close render procedure
        /// only dispose render procedure! 
        /// </summary>
        protected virtual void CloseRenderer()
        {
            //only dispose window handle
            _windowInfo?.Dispose();
            _hwndSource?.Dispose();
        }

        /// <summary>
        /// open render procedure
        /// </summary>
        protected abstract void StartRenderProcedure(IWindowInfo windowInfo);

        /// <summary>
        /// after <see cref="IsUserVisible"/> changed
        /// </summary>
        protected abstract void OnUserVisibleChanged(PropertyChangedArgs<bool> args);

        /// <summary>
        /// Check if it is run in designer mode.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsDesignMode() => DesignerProperties.GetIsInDesignMode(this);

        protected override void OnRender(DrawingContext drawingContext)
        {
#if DEBUG
            if (IsDesignMode())
            {
                DesignTimeHelper.DrawDesignTimeHelper(this, drawingContext);
                return;
            }
#endif
            base.OnRender(drawingContext);

            /*if (!IsRendererOpened)
            {
                UnstartedControlHelper.DrawUnstartedControlHelper(this, drawingContext);
            }*/
        }

        private HwndSource _hwndSource;

        /// <summary>
        /// Get window handle, if null, call <see cref="Start"/>
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">Information about the event</param>
        protected virtual void OnLoaded(object sender, RoutedEventArgs args)
        {
            _isControlLoaded = true;
            CheckUserVisible();
            if (IsDesignMode())
            {
                return;
            }

            if (!IsRendererOpened && IsAutoAttach)
            {
                var window = Window.GetWindow(this);
                if (window == null)
                {
                    return;
                }

                Start(window);
            }
        }

        /// <summary>
        /// Called when this control is unloaded
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">Information about the event</param>
        protected virtual void OnUnloaded(object sender, RoutedEventArgs args)
        {
            _isControlLoaded = false;
            CheckUserVisible();
            if (IsDesignMode())
            {
                return;
            }

            if (RendererProcedureLifeCycle == RendererProcedureLifeCycle.Self)
            {
                this.Close();
            }
        }

        protected void Callback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
            IntPtr message, IntPtr userParam)
        {
            OnOpenGlErrorReceived(
                new OpenGlErrorArgs(source, type, id, severity, length, message, userParam));
        }

        protected virtual void OnExceptionOccurred(UnhandledExceptionEventArgs e)
        {
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnOpenGlErrorReceived(OpenGlErrorArgs e)
        {
            OpenGlErrorReceived?.Invoke(this, e);
        }

        private bool _isDisposed;

        /// <summary>
        /// can't reopen render procedure
        /// </summary>
        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Close();
            Dispose(true);
            _isDisposed = true;
        }

        protected abstract void Dispose(bool dispose);

        protected virtual void OnAfterRender(GlRenderEventArgs renderEventArgs)
        {
            AfterRender?.Invoke(this, renderEventArgs);
        }

        protected virtual void OnBeforeRender(GlRenderEventArgs renderEventArgs)
        {
            BeforeRender?.Invoke(this, renderEventArgs);
        }

        protected virtual void OnRenderErrorReceived(RenderErrorArgs e)
        {
            RenderErrorReceived?.Invoke(this, e);
        }
    }
}