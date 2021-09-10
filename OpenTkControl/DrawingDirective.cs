using System.Windows.Media;

namespace OpenTkWPFHost
{
    public class DrawingDirective
    {
        public TransformGroup TransformGroup { get; }

        public bool IsNeedTransform => TransformGroup != null;

        /// <summary>
        /// async drawing
        /// </summary>
        public bool IsDrawingAsync { get; } = false;

        public DrawingDirective(TransformGroup transformGroup,
            bool isDrawAsync = false)
        {
            TransformGroup = transformGroup;
            this.IsDrawingAsync = isDrawAsync;
        }
    }
}