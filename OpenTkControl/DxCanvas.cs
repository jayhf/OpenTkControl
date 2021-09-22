using System;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    public class DxCanvas : IRenderCanvas, IRenderBuffer
    {
        public D3DImage Image { get; private set; }

        public Guid Id { get; } = Guid.NewGuid();

        public void Create(CanvasInfo info)
        {
            Image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
            // Image.IsFrontBufferAvailableChanged += Image_IsFrontBufferAvailableChanged;
        }

        public ImageSource ImageSource => Image;

        public bool IsAvailable => Image != null && Image.IsFrontBufferAvailable && Image.Width > 0 && Image.Height > 0;
    }
}