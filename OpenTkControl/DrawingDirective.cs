using System.Windows.Media;

namespace OpenTkControl
{
    public class DrawingDirective
    {
        public TranslateTransform TranslateTransform { get; }

        public ScaleTransform ScaleTransform { get; }

        public bool IsNeedTransform => TranslateTransform != null || ScaleTransform != null;

        /// <summary>
        /// async drawing
        /// </summary>
        public bool IsDrawingAsync { get; } = false;

        public DrawingDirective(TranslateTransform translate, ScaleTransform scale,
            bool isDrawAsync = false)
        {
            this.TranslateTransform = translate;
            this.ScaleTransform = scale;
            this.IsDrawingAsync = isDrawAsync;
        }

        public bool Equals(DrawingDirective other)
        {
            return Equals(TranslateTransform, other.TranslateTransform) && Equals(ScaleTransform, other.ScaleTransform);
        }

        public override bool Equals(object obj)
        {
            return obj is DrawingDirective other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TranslateTransform != null ? TranslateTransform.GetHashCode() : 0) * 397) ^
                       (ScaleTransform != null ? ScaleTransform.GetHashCode() : 0);
            }
        }
    }
}