using System;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public class EmptyWindowInfo : IWindowInfo
    {
        public void Dispose()
        {
        }

        public IntPtr Handle { get; } = IntPtr.Zero;
    }
}