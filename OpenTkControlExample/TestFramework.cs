using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTkWPFHost;
using OpenTkWPFHost.Core;
using Point = System.Windows.Point;

namespace OpenTkControlExample
{
    public class TestFrameworkElement : FrameworkElement
    {
        public static readonly DependencyProperty BProperty = DependencyProperty.Register(
            "B", typeof(bool), typeof(TestFrameworkElement), new FrameworkPropertyMetadata(default(bool),FrameworkPropertyMetadataOptions.AffectsRender));

        public bool B
        {
            get { return (bool) GetValue(BProperty); }
            set { SetValue(BProperty, value); }
        }

        /*private DrawingVisual drawingVisual = new DrawingVisual();

        protected override Visual GetVisualChild(int index)
        {
            return drawingVisual;
        }

        protected override int VisualChildrenCount { get; } = 1;*/

        public TestFrameworkElement()
        {
            /*wpf 驱动速率和渲染速率是匹配的，但是在compositiontarget的监听上会叠加（和系统驱动一起），
             比如两个90会驱动出90x2+60=240，因为invalidate只会标记当前控件视图失效
             */
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            /*Task.Run((async () =>
            {
                await Task.Delay(1000);
                bool x = false;
                while (true)
                {

                    x = !x;
                    // Dispatcher.Invoke((() => B = x)); 

                    Thread.Sleep(10);
                }
            }));*/
        }

        private readonly FpsCounter _fpsCounter = new FpsCounter();
        
        private void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            _fpsCounter.Increment();
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // _fpsCounter.Increment();
            base.OnRender(drawingContext);
            _fpsCounter.DrawFps(drawingContext, new Point(0, 0));
        }
    }

}