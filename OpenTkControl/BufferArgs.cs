using System;

namespace OpenTkWPFHost
{
    public class BufferArgs
    {
        public IntPtr HostBufferIntPtr { get; set; }

        public CanvasInfo CanvasInfo { get; set; }

        public BufferInfo BufferInfo { get; set; }
    }
}