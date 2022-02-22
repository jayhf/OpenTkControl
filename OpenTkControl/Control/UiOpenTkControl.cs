using System;
using System.Windows;
using System.Windows.Media;
using OpenTK.Platform;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Control
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

        }

        private IWindowInfo _windowInfo;

        protected override void ResumeRender()
        {
            throw new NotImplementedException();
        }

        protected override void StartRenderProcedure(IWindowInfo windowInfo)
        {
            this._windowInfo = windowInfo;
        }


        protected override void OnUserVisibleChanged(PropertyChangedArgs<bool> args)
        {
        }

        protected override void OnLoaded(object sender, RoutedEventArgs args)
        {
            base.OnLoaded(sender, args);
        }

        protected override void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            base.OnUnloaded(sender, routedEventArgs);
        }

        protected override void Dispose(bool dispose)
        {
            
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
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