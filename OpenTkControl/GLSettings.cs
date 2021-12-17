using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public enum RenderTrigger
    {
        /// <summary>
        /// listen to <see cref="CompositionTarget.Rendering"/>
        /// </summary>
        CompositionTarget = 0,

        /// <summary>
        /// use a discrete loop to drive render
        /// <para>warning: event rate of <see cref="CompositionTarget "/> will surge </para> 
        /// </summary>
        Internal = 1,
    }

    public enum RenderTactic
    {
        // Balance = 2,

        /// <summary>
        /// max Throughput£¬
        /// </summary>
        ThroughputPriority = 0,

        /// <summary>
        /// lowest latency
        /// </summary>
        LatencyPriority = 1,
    }

    public class RenderSetting
    {
        public RenderTrigger RenderTrigger { get; set; } = RenderTrigger.CompositionTarget;

        public RenderTactic RenderTactic { get; set; } = RenderTactic.LatencyPriority;

        /// If this is set to false, the control will render without any DPI scaling.
        /// This will result in higher performance and a worse image quality on systems with >100% DPI settings, such as 'Retina' laptop screens with 4K UHD at small sizes.
        /// This setting may be useful to get extra performance on mobile platforms.
        public bool UseDeviceDpi { get; set; } = false;

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
                Debug.Assert(presentationSource.CompositionTarget != null,
                    "presentationSource.CompositionTarget != null");
                var transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
                dpiScaleX = transformToDevice.M11;
                dpiScaleY = transformToDevice.M22;
            }

            return new CanvasInfo((int) element.ActualWidth, (int) element.ActualHeight, dpiScaleX, dpiScaleY);
        }
    }

    public sealed class GLSettings : ICloneable
    {
        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public IGraphicsContext ContextToUse { get; set; }

        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Offscreen;

        public ContextProfileMask GraphicsProfile { get; set; } = ContextProfileMask.ContextCoreProfileBit;

        public SyncMode SyncMode { get; set; } = SyncMode.On;

        public GraphicsMode GraphicsMode { get; set; } = GraphicsMode.Default;

        public int MajorVersion { get; set; } = 4;

        public int MinorVersion { get; set; } = 3;

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;


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


        public GLContextBinding NewBinding(GLContextBinding binding)
        {
            return new GLContextBinding(CreateContext(binding.Info, binding.Context), binding.Info);
        }

        public IGraphicsContext CreateContext(IWindowInfo windowInfo, IGraphicsContext sharedContext = null)
        {
            if (sharedContext == null)
            {
                return new GraphicsContext(this.GraphicsMode, windowInfo, this.MajorVersion,
                        this.MinorVersion,
                        this.GraphicsContextFlags)
                    {SwapInterval = (int) this.SyncMode};
            }

            return new GraphicsContext(this.GraphicsMode, windowInfo, sharedContext, this.MajorVersion,
                this.MinorVersion, this.GraphicsContextFlags)
            {
                SwapInterval = (int) this.SyncMode,
            };
        }

        public object Clone()
        {
            return new GLSettings
            {
                ContextToUse = ContextToUse,
                GraphicsContextFlags = GraphicsContextFlags,
                GraphicsProfile = GraphicsProfile,
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
            };
        }
    }
}