using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Platform.Windows;
using Buffer = System.Buffer;
using BufferTarget = OpenTK.Graphics.OpenGL4.BufferTarget;
using BufferUsageHint = OpenTK.Graphics.OpenGL4.BufferUsageHint;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;
using FramebufferAttachment = OpenTK.Graphics.OpenGL4.FramebufferAttachment;
using FramebufferErrorCode = OpenTK.Graphics.OpenGL4.FramebufferErrorCode;
using FramebufferTarget = OpenTK.Graphics.OpenGL4.FramebufferTarget;
using GL = OpenTK.Graphics.OpenGL4.GL;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL4.PixelType;
using ReadBufferMode = OpenTK.Graphics.OpenGL4.ReadBufferMode;
using RenderbufferTarget = OpenTK.Graphics.OpenGL4.RenderbufferTarget;
using WindowState = System.Windows.WindowState;

namespace OpenTkControl
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

        /// <summary>
        /// Called whenever an exception occurs during initialization, rendering or deinitialization
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> ExceptionOccurred;

        public static readonly DependencyProperty RendererProperty = DependencyProperty.Register(
            "Renderer", typeof(IRenderProcedure), typeof(OpenTkControlBase),
            new PropertyMetadata(default(IRenderProcedure)));

        public IRenderProcedure Renderer
        {
            get { return (IRenderProcedure) GetValue(RendererProperty); }
            set { SetValue(RendererProperty, value); }
        }

        public bool IsShowFps
        {
            get { return (bool) GetValue(IsShowFpsProperty); }
            set { SetValue(IsShowFpsProperty, value); }
        }

        public static readonly DependencyProperty IsShowFpsProperty =
            DependencyProperty.Register("IsShowFps", typeof(bool), typeof(OpenTkControlBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty RenderTriggerProperty = DependencyProperty.Register(
            "RenderTrigger", typeof(bool), typeof(OpenTkControlBase),
            new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        protected bool RenderTrigger
        {
            get { return (bool) GetValue(RenderTriggerProperty); }
            set { SetValue(RenderTriggerProperty, value); }
        }

        /// <summary>
        /// default is bound to window as wpf window cannot reuse after close
        /// </summary>
        public static readonly DependencyProperty RendererLifeCycleProperty = DependencyProperty.Register(
            "RendererLifeCycle", typeof(RendererProcedureLifeCycle), typeof(OpenTkControlBase),
            new PropertyMetadata(RendererProcedureLifeCycle.BoundToWindow));

        public static readonly DependencyProperty IsRendererOpenedProperty = DependencyProperty.Register(
            "IsRendererOpened", typeof(bool), typeof(OpenTkControlBase), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 渲染过程是否已被打开
        /// </summary>
        public bool IsRendererOpened
        {
            get { return (bool) GetValue(IsRendererOpenedProperty); }
            set { SetValue(IsRendererOpenedProperty, value); }
        }

        /// <summary>
        /// control renderer lifecycle
        /// </summary>
        public RendererProcedureLifeCycle RendererLifeCycle
        {
            get { return (RendererProcedureLifeCycle) GetValue(RendererLifeCycleProperty); }
            set { SetValue(RendererLifeCycleProperty, value); }
        }

        public static readonly DependencyProperty IsAutoStartProperty = DependencyProperty.Register(
            "IsAutoStart", typeof(bool), typeof(OpenTkControlBase), new PropertyMetadata(default(bool)));

        /// <summary>
        /// if set to true, will start rendering when this element is loaded.
        /// </summary>
        public bool IsAutoStart
        {
            get { return (bool) GetValue(IsAutoStartProperty); }
            set { SetValue(IsAutoStartProperty, value); }
        }

        /// <summary>
        /// indicate whether user can see the control,not dependency property
        /// a combination of window closed/minimized, control unloaded/visibility status
        /// </summary>
        protected bool IsUserVisible
        {
            set
            {
                if (UserVisible == value)
                {
                    return;
                }

                var oldValue = UserVisible;
                UserVisible = value;
                OnUserVisibleChanged(new PropertyChangedArgs<bool>(oldValue, value));
            }
        }

        protected bool UserVisible = false;

        private WindowState _windowState;

        /// <summary>
        /// window visibility
        /// </summary>
        private bool _isWindowVisible;

        private bool _isWindowLoaded;

        private bool _isWindowClosed;

        private bool _isControlLoaded;

        private bool _isControlVisible;

        /// <summary>
        /// 依赖属性的性能较差，使用变量
        /// </summary>
        protected IRenderProcedure RenderProcedure;

        private IWindowInfo _windowInfo;

        /// <summary>
        /// True if OnLoaded has already been called
        /// </summary>
        private bool _alreadyLoaded;

        protected bool ShowFps = (bool) IsShowFpsProperty.DefaultMetadata.DefaultValue;

        /// <summary>
        /// Creates the <see cref="OpenTkControlBase"/>/>
        /// </summary>
        protected OpenTkControlBase()
        {
            //used for fast read and thread safe
            DependencyPropertyDescriptor.FromProperty(IsShowFpsProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => ShowFps = IsShowFps);
            DependencyPropertyDescriptor.FromProperty(RendererProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) =>
                {
                    var oldValue = RenderProcedure;
                    RenderProcedure = Renderer;
                    OnRenderProcedureChanged(new PropertyChangedArgs<IRenderProcedure>(oldValue, Renderer));
                });
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
            Application.Current.Exit += ((sender, args) =>
            {
                if (RendererLifeCycle == RendererProcedureLifeCycle.BoundToApplication)
                {
                    Close();
                }
            });
            this.IsVisibleChanged += OpenTkControlBase_IsVisibleChanged;
        }

        private void ApplyUserVisible()
        {
            this.IsUserVisible = _windowState != WindowState.Minimized && !_isWindowClosed && _isControlVisible &&
                                 _isWindowVisible && _isWindowLoaded && _isControlLoaded;
        }

        private void OpenTkControlBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _isControlVisible = (bool) e.NewValue;
            ApplyUserVisible();
        }

        private void HostWindow_StateChanged(object sender, EventArgs e)
        {
            _windowState = ((Window) sender).WindowState;
            ApplyUserVisible();
        }

        private void HostWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _isWindowVisible = (bool) e.NewValue;
            ApplyUserVisible();
        }

        private void HostWindow_Closed(object sender, EventArgs e)
        {
            _isWindowClosed = true;
            ApplyUserVisible();
            if (RendererLifeCycle == RendererProcedureLifeCycle.BoundToWindow)
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
            if (IsRendererOpened)
            {
                return;
            }

            if (hostWindow == null)
            {
                throw new ArgumentNullException();
            }

            _isWindowClosed = false;
            _windowState = hostWindow.WindowState;
            _isWindowVisible = hostWindow.IsVisible;
            _isWindowLoaded = hostWindow.IsLoaded;
            _isControlVisible = this.IsVisible;
            _isControlLoaded = this.IsLoaded;
            ApplyUserVisible();
            if (UserVisible)
            {
                Debug.WriteLine("Warning! uservisible is false, rendering may not be enable.");
            }

            var baseHandle = new WindowInteropHelper(hostWindow).Handle;
            _hwndSource = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);
            this._windowInfo = Utilities.CreateWindowsWindowInfo(_hwndSource.Handle);
            hostWindow.Closed += HostWindow_Closed;
            hostWindow.IsVisibleChanged += HostWindow_IsVisibleChanged;
            hostWindow.StateChanged += HostWindow_StateChanged;
            this.IsRendererOpened = true;
            this.OpenRenderer(_windowInfo);
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
            this.IsRendererOpened = false;
            CloseRenderer();
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
        protected abstract void OpenRenderer(IWindowInfo windowInfo);

        /// <summary>
        /// after render procedure changed
        /// </summary>
        /// <param name="args"></param>
        protected abstract void OnRenderProcedureChanged(PropertyChangedArgs<IRenderProcedure> args);

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
            if (IsDesignMode())
            {
                DesignTimeHelper.DrawDesignTimeHelper(this, drawingContext);
            }

            base.OnRender(drawingContext);
            if (RenderProcedure == null)
            {
                UnstartedControlHelper.DrawUnstartedControlHelper(this, drawingContext);
            }
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
            ApplyUserVisible();
            if (IsDesignMode())
            {
                return;
            }

            if (!IsRendererOpened && IsAutoStart)
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
            ApplyUserVisible();
            if (IsDesignMode())
            {
                return;
            }

            if (RendererLifeCycle == RendererProcedureLifeCycle.Self)
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

            _isDisposed = true;
            Close();
            Dispose(true);
        }

        protected abstract void Dispose(bool dispose);
    }
}