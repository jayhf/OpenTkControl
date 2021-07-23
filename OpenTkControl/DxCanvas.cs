using System;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenTkControl
{
    public class DxCanvas : IRenderCanvas
    {
        private D3DImage _image;

        public D3DImage Image => _image;

        public Guid Id { get; } = Guid.NewGuid();

        public ImageSource GetSource()
        {
            return _image;
        }

        public void Create(CanvasInfo info)
        {
            _image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
        }

        public bool CanRender => _image != null && _image.Width > 0 && _image.Height > 0;
    }
}