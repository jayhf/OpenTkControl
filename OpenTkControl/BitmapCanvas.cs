using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenTkControl
{
    public class BitmapCanvas : IRenderCanvas
    {
        public ImageSource Canvas => Bitmap;

        public WriteableBitmap Bitmap { get; private set; }

        public void Create(CanvasInfo info)
        {
            if (info.ActualHeight == 0 || info.ActualWidth==0)
            {
                return;
            }

            Bitmap = new WriteableBitmap(info.ActualWidth, info.ActualHeight, 96 * info.DpiScaleX,
                96 * info.DpiScaleY, PixelFormats.Pbgra32, null);
        }
    }
}