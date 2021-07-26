using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;
using Xunit.Abstractions;

namespace WpfLibrary1
{
    public class Class1
    {
        private ITestOutputHelper output;

        public Class1(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void FactMethodName()
        {
            var task = Task.Run(() =>
            {
                var writeableBitmap = new WriteableBitmap(100, 100, 1, 1, PixelFormats.Bgra32, null);
                writeableBitmap.Lock();
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, 40, 50));
                writeableBitmap.Unlock();
                Debugger.Break();
            });
            task.GetAwaiter().GetResult();
        }

        [Fact]
        public async void TestWaitOne()
        {
            using (var manualResetEvent = new ManualResetEvent(false))
            {
                output.WriteLine(manualResetEvent.WaitOne(0).ToString());
                Task.Run((() =>
                {
                    manualResetEvent.WaitOne();
                    manualResetEvent.Reset();
                }));
                await Task.Delay(300);
                output.WriteLine(manualResetEvent.WaitOne(0).ToString());
                manualResetEvent.Set();
                output.WriteLine(manualResetEvent.WaitOne(0).ToString());
                await Task.Delay(300);
                output.WriteLine(manualResetEvent.WaitOne(0).ToString());
                manualResetEvent.Set();
                output.WriteLine(manualResetEvent.WaitOne(0).ToString());
                await Task.Delay(int.MaxValue);
            }
        }
    }
}