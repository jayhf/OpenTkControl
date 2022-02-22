using System.Windows.Media;

namespace OpenTkWPFHost.Core
{
    public class DrawingDirective
    {
        public TransformGroup TransformGroup { get; }

        public bool IsNeedTransform { get; }

        /// <summary>
        /// async drawing
        /// </summary>
        public bool IsDrawingAsync { get; } = false;

        public DrawingDirective(TransformGroup transformGroup,
            bool isDrawAsync = false)
        {
            TransformGroup = transformGroup;
            IsNeedTransform = transformGroup != null;
            this.IsDrawingAsync = isDrawAsync;
        }
    }
}