using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace OpenTkControl
{
    public sealed class GLSettings
    {
        /// If the render event is fired continuously whenever required.
        /// Disable this if you want manual control over when the rendered surface is updated.
        public bool RenderContinuously { get; set; } = true;

        /// If this is set to false, the control will render without any DPI scaling.
        /// This will result in higher performance and a worse image quality on systems with >100% DPI settings, such as 'Retina' laptop screens with 4K UHD at small sizes.
        /// This setting may be useful to get extra performance on mobile platforms.
        public bool UseDeviceDpi { get; set; } = true;

        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public IGraphicsContext ContextToUse { get; set; }

        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Default;
        public ContextProfileMask GraphicsProfile { get; set; }

        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;

        /// Creates a copy of the settings.
        internal GLSettings Copy()
        {
            var c = new GLSettings
            {
                ContextToUse = ContextToUse,
                GraphicsContextFlags = GraphicsContextFlags,
                GraphicsProfile = GraphicsProfile,
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
                RenderContinuously = RenderContinuously,
                UseDeviceDpi = UseDeviceDpi
            };
            return c;
        }

        /// Determines if two settings would result in the same context being created.
        [Pure]
        internal static bool WouldResultInSameContext([NotNull] GLSettings a, [NotNull] GLSettings b)
        {
            if (a.MajorVersion != b.MajorVersion)
            {
                return false;
            }

            if (a.MinorVersion != b.MinorVersion)
            {
                return false;
            }

            if (a.GraphicsProfile != b.GraphicsProfile)
            {
                return false;
            }

            if (a.GraphicsContextFlags != b.GraphicsContextFlags)
            {
                return false;
            }

            return true;
        }

        public CanvasInfo CreateCanvasInfo(FrameworkElement element)
        {
            if (!UseDeviceDpi)
            {
                return new CanvasInfo((int) element.ActualWidth, (int) element.ActualHeight, 1, 1);
            }

            var dpiScaleX = 1.0;
            var dpiScaleY = 1.0;
            var presentationSource = PresentationSource.FromVisual(element);
            // this can be null in the case of not having any visual on screen, such as a tabbed view.
            if (presentationSource != null)
            {
                Debug.Assert(presentationSource.CompositionTarget != null, "presentationSource.CompositionTarget != null");
                var transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
                dpiScaleX = transformToDevice.M11;
                dpiScaleY = transformToDevice.M22;
            }

            return new CanvasInfo((int) element.ActualWidth, (int) element.ActualHeight, dpiScaleX, dpiScaleY);
        }
    }
}