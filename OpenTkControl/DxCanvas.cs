using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    public class DxCanvas : IRenderCanvas
    {
        public D3DImage Image { get; private set; }

        private double _dpiScaleX, _dpiScaleY;

        private FieldInfo fieldInfo;

        public void Create(CanvasInfo info)
        {
            if (info.DpiScaleX.Equals(_dpiScaleX)
                && info.DpiScaleY.Equals(_dpiScaleY))
            {
                return;
            }

            this._dpiScaleX = info.DpiScaleX;
            this._dpiScaleY = info.DpiScaleY;
            Image = new D3DImage(96.0 * info.DpiScaleX, 96.0 * info.DpiScaleY);
            var bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            | BindingFlags.Static;
            fieldInfo = typeof(D3DImage).GetField("_isDirty", bindFlags);
        }

        public bool D3DImageDirty
        {
            get { return (bool) fieldInfo.GetValue(this.Image); }
        }

        public ImageSource ImageSource => Image;

        public bool IsAvailable => Image != null && Image.Width > 0 && Image.Height > 0;
    }
}