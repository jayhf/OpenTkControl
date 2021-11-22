using System;

namespace OpenTkWPFHost
{
    public class PipelineArgs
    {
        public PixelSize PixelSize { get; set; }
    }

    public class RenderArgs:PipelineArgs
    {
        
    }

    public class BitmapRenderArgs : RenderArgs
    {
        public BufferInfo BufferInfo { get; set; }
    }

    
}