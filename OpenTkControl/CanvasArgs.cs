using System;
using System.Windows;

namespace OpenTkWPFHost
{
    public abstract class CanvasArgs : PipelineArgs
    {
    }

    public class BitmapCanvasArgs:CanvasArgs
    {
        public Int32Rect Int32Rect { get; set; }
    }

    public class DXCanvasArgs:CanvasArgs
    {
        public IntPtr FrameBuffer { get; set; }
    }
}