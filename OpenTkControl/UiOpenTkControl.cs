using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    /// <summary>
    /// A WPF control that performs OpenGL rendering on the UI thread
    /// </summary>
    public class UiOpenTkControl : OpenTkControlBase
    {
        private DateTime _nextRenderTime = DateTime.MinValue;

        /// <summary>
        /// Creates a UiOpenTkControl
        /// </summary>
        public UiOpenTkControl() : base()
        {
            IsVisibleChanged += OnIsVisibleChanged;
            this.SizeChanged += UiOpenTkControl_SizeChanged;
        }

        private void UiOpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderProcedure != null && RenderProcedure.IsInitialized)
            {
                var canvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SizeFrame(canvasInfo);
            }
        }

        private IWindowInfo _windowInfo;

        protected override void ResumeRender()
        {
            throw new NotImplementedException();
        }

        protected override void OpenRenderer(IWindowInfo windowInfo)
        {
            this._windowInfo = windowInfo;
        }

        protected override void OnRenderProcedureChanged(PropertyChangedArgs<IRenderProcedure> args)
        {
            if (RenderProcedure != null && this.IsLoaded)
            {
                RenderProcedure.Initialize(this._windowInfo);
                var canvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SizeFrame(canvasInfo);
            }
        }


        protected override void OnUserVisibleChanged(PropertyChangedArgs<bool> args)
        {
            throw new NotImplementedException();
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
            if (RenderProcedure != null && !RenderProcedure.IsInitialized)
            {
                RenderProcedure.Initialize(this._windowInfo);
                var canvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SizeFrame(canvasInfo);
            }
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            base.OnUnloaded(sender, routedEventArgs);
        }

        protected override void Dispose(bool dispose)
        {
            throw new NotImplementedException();
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (RenderProcedure != null && RenderProcedure.IsInitialized)
            {
                RenderProcedure.Render(null);
                /*var imageSource = new BitmapImage(); //drawingDirective?.;
                if (imageSource != null)
                {
                    if (drawingDirective.IsNeedTransform)
                    {
                        // Transforms are applied in reverse order
                        drawingContext.PushTransform(drawingDirective
                            .TransformGroup); // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                        var rect = new Rect(0, 0, imageSource.Width, imageSource.Height);
                        drawingContext.DrawImage(imageSource, rect); // Draw the image source 
                        drawingContext.Pop(); // Remove the scale transform
                    }
                    else
                    {
                        var rect = new Rect(0, 0, imageSource.Width, imageSource.Height);
                        drawingContext.DrawImage(imageSource, rect); // Draw the image source 
                    }
                }*/
            }
        }

        /// <summary>
        /// Performs the OpenGl rendering when this control is visible
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">The event arguments about this event</param>
        private void CompositionTargetOnRendering(object sender, EventArgs args)
        {
#if DEBUG
            //We needn't call render() for avoiding crash by calling OpenGL API methods.
            if (IsDesignMode())
                return;
#endif

            DateTime now = DateTime.Now;
            InvalidateVisual();
            /*if ((_continuous && now > _nextRenderTime) || ManualRepaintEvent.WaitOne(0))
            {
                ManualRepaintEvent.Reset();
                _nextRenderTime = now + Renderer();
            }*/
        }

        /// <summary>
        /// Handles subscribing and unsubcribing <see cref="CompositionTargetOnRendering"/> when this component's visibility has changed
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">The event arguments about this event</param>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            bool visible = (bool) args.NewValue;

            if (visible)
                CompositionTarget.Rendering += CompositionTargetOnRendering;
            else
                CompositionTarget.Rendering -= CompositionTargetOnRendering;
        }
    }
}