using System;
using System.Windows;

namespace OpenTkWPFHost
{
    public class CanvasArgs : PipelineArgs
    {
    }

    public class BitmapCanvasArgs:CanvasArgs
    {
        public Int32Rect Int32Rect { get; set; }
    }
}