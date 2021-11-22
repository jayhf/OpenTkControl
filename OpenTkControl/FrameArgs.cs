using System.Windows;

namespace OpenTkWPFHost
{
    public class FrameArgs : PipelineArgs
    {
    }

    public class BitmapFrameArgs : FrameArgs
    {
        public BufferInfo BufferInfo { get; set; }
    }
}