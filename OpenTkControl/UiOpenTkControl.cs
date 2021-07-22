using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkControl
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
                RenderProcedure.SetSize(canvasInfo);
            }
        }

        protected override void OnRenderProcedureChanged()
        {
            if (RenderProcedure != null && this.IsLoaded)
            {
                RenderProcedure.Initialize(this.WindowInfo);
                var canvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SetSize(canvasInfo);
            }
        }

        protected override void OnRenderProcedureChanging()
        {
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
            if (RenderProcedure != null && !RenderProcedure.IsInitialized)
            {
                RenderProcedure.Initialize(this.WindowInfo);
                var canvasInfo = RenderProcedure.GlSettings.CreateCanvasInfo(this);
                RenderProcedure.SetSize(canvasInfo);
            }
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            base.OnUnloaded(sender, routedEventArgs);
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (RenderProcedure != null && RenderProcedure.IsInitialized)
            {
                var drawingDirective = RenderProcedure.Render();
                var imageSource = new BitmapImage();//drawingDirective?.ImageSource;
                if (imageSource != null)
                {
                    if (drawingDirective.IsNeedTransform)
                    {
                        // Transforms are applied in reverse order
                        drawingContext.PushTransform(drawingDirective
                            .TranslateTransform); // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                        drawingContext.PushTransform(drawingDirective
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