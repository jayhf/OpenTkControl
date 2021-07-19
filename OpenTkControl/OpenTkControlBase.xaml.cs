using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

namespace OpenTkControl
{
    /// <summary>
    /// Interaction logic for OpenTkControlBase.xaml. OpenTkControlBase is a base class for OpenTK WPF controls
    /// </summary>
    public abstract partial class OpenTkControlBase
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

        public static readonly DependencyProperty RendererProperty = DependencyProperty.Register(
            "Renderer", typeof(IRenderProcedure), typeof(OpenTkControlBase),
            new PropertyMetadata(default(IRenderProcedure)));

        public IRenderProcedure Renderer
        {
            get { return (IRenderProcedure) GetValue(RendererProperty); }
            set { SetValue(RendererProperty, value); }
        }

        /// <summary>
        /// 依赖属性的性能较差，使用变量
        /// </summary>
        protected IRenderProcedure RenderProcedure { get; set; }

        private volatile float _frameRateLimit = (float) FrameRateLimitProperty.DefaultMetadata.DefaultValue;

        public static readonly DependencyProperty FrameRateLimitProperty = DependencyProperty.Register(
            nameof(FrameRateLimit), typeof(float), typeof(OpenTkControlBase),
            new PropertyMetadata(float.PositiveInfinity));

        /// <summary>
        /// The maximum frame rate to render at. Anything over 1000 is treated as unlimited.
        /// </summary>
        public float FrameRateLimit
        {
            get => (float) GetValue(FrameRateLimitProperty);
            set => SetValue(FrameRateLimitProperty, value);
        }

        /// <summary>
        /// True if OnLoaded has already been called
        /// </summary>
        private bool _alreadyLoaded;

        /// <summary>
        /// Creates the <see cref="OpenTkControlBase"/>/>
        /// </summary>
        protected OpenTkControlBase()
        {
            InitializeComponent();

            // Update all of the volatile copies the variables
            // This is a workaround for the WPF threading restric_rendererResetEventtions on DependencyProperties
            // that allows other threads to read the values.
            DependencyPropertyDescriptor.FromProperty(FrameRateLimitProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => _frameRateLimit = FrameRateLimit);
            DependencyPropertyDescriptor.FromProperty(RendererProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) =>
                {
                    RenderProcedure = Renderer;
                    OnRenderProcedureChanged();
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
        }


        /// <summary>
        /// after renderprocedure changed
        /// </summary>
        protected abstract void OnRenderProcedureChanged();

        /// <summary>
        /// request change renderprocedure
        /// </summary>
        protected abstract void OnRenderProcedureChanging();

        protected IWindowInfo WindowInfo;

        /// <summary>
        /// Check if it is run in designer mode.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsDesignMode() => DesignerProperties.GetIsInDesignMode(this);

        /// <summary>
        /// Executes an action on the UI thread
        /// </summary>
        /// <param name="action">The action to run</param>
        /// <returns>a Task that will complete when the action finishes running or null if already complete</returns>
        // public abstract Task RunOnUiThread(Action action);


        /// <summary>
        /// Called when this control is loaded
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">Information about the event</param>
        protected virtual void OnLoaded(object sender, RoutedEventArgs args)
        {
            WindowInfo = Utilities.CreateWindowsWindowInfo(
                new WindowInteropHelper(Window.GetWindow(this)).Handle);
        }

        /// <summary>
        /// Called when this control is unloaded
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">Information about the event</param>
        protected virtual void OnUnloaded(object sender, RoutedEventArgs args)
        {
        }
    }
}