using System.Diagnostics;
using System.Windows;

namespace OpenTkWPFHost
{
    public class RenderSetting
    {
        public RenderTrigger RenderTrigger { get; set; } = RenderTrigger.Internal;

        public RenderTactic RenderTactic { get; set; } = RenderTactic.ThroughputPriority;

        /// If this is set to false, the control will render without any DPI scaling.
        /// This will result in higher performance and a worse image quality on systems with >100% DPI settings, such as 'Retina' laptop screens with 4K UHD at small sizes.
        /// This setting may be useful to get extra performance on mobile platforms.
        public bool UseDeviceDpi { get; set; } = false;

        public RenderTargetInfo CreateCanvasInfo(FrameworkElement element)
        {
            if (!UseDeviceDpi)
            {
                return new RenderTargetInfo((int) element.ActualWidth, (int) element.ActualHeight, 1, 1);
            }

            var dpiScaleX = 1.0;
            var dpiScaleY = 1.0;
            var presentationSource = PresentationSource.FromVisual(element);
            // this can be null in the case of not having any visual on screen, such as a tabbed view.
            if (presentationSource != null)
            {
                Debug.Assert(presentationSource.CompositionTarget != null,
                    "presentationSource.CompositionTarget != null");
                var transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
                dpiScaleX = transformToDevice.M11;
                dpiScaleY = transformToDevice.M22;
            }

            return new RenderTargetInfo((int) element.ActualWidth, (int) element.ActualHeight, dpiScaleX, dpiScaleY);
        }
    }
}