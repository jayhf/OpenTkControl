using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace OpenTkWPFHost
{
    public class TestFrameworkElement : FrameworkElement
    {
        public TestFrameworkElement()
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private readonly FpsCounter _fpsCounter = new FpsCounter();

        private void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            _fpsCounter.Increment();
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            _fpsCounter.DrawFps(drawingContext, new Point(0, 0));
        }
    }

}