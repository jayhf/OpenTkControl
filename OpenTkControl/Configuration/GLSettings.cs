using System;
using JetBrains.Annotations;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.Configuration
{
    public sealed class GLSettings : ICloneable
    {
        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public IGraphicsContext ContextToUse { get; set; }

        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Offscreen;

        public ContextProfileMask GraphicsProfile { get; set; } = ContextProfileMask.ContextCoreProfileBit;

        public SyncMode SyncMode { get; set; } = SyncMode.On;

        public GraphicsMode GraphicsMode { get; set; }
            = new GraphicsMode(new ColorFormat(32), 24, 0, 0);

// =GraphicsMode.Default;
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
                    { SwapInterval = (int)this.SyncMode };
            }

            return new GraphicsContext(this.GraphicsMode, windowInfo, sharedContext, this.MajorVersion,
                this.MinorVersion, this.GraphicsContextFlags)
            {
                SwapInterval = (int)this.SyncMode,
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