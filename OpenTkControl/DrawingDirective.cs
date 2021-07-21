using System.Windows.Media;

namespace OpenTkControl
{
    public class DrawingDirective
    {
        public TranslateTransform TranslateTransform { get; }

        public ScaleTransform ScaleTransform { get; }

        public ImageSource ImageSource { get; set; }

        public bool IsNeedTransform => TranslateTransform != null || ScaleTransform != null;

        /// <summary>
        /// 
        /// </summary>
        public bool CanDraw { get; set; }

        /// <summary>
        /// async drawing
        /// </summary>
        public bool IsDrawingAsync { get; }

        public DrawingDirective(TranslateTransform translate, ScaleTransform scale, ImageSource imageSource,
            bool isDrawAsync = false)
        {
            this.TranslateTransform = translate;
            this.ScaleTransform = scale;
            ImageSource = imageSource;
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