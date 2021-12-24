using System;
using OpenTK.Platform;

namespace OpenTkWPFHost.Core
{
    public class EmptyWindowInfo : IWindowInfo
    {
        public void Dispose()
        {
        }

        public IntPtr Handle { get; } = IntPtr.Zero;
    }
}