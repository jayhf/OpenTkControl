using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace WpfLibrary1
{
    public class Class1
    {
        [Fact]
        public void FactMethodName()
        {
            var task = Task.Run(() =>
            {
                var writeableBitmap = new WriteableBitmap(100,100,1,1,PixelFormats.Bgra32, null);
                writeableBitmap.Lock();
                writeableBitmap.AddDirtyRect(new Int32Rect(0,0,40,50));
                writeableBitmap.Unlock();
                Debugger.Break();
            }); 
            task.GetAwaiter().GetResult();
        }
    }
}
