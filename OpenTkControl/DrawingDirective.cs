using System.Windows.Media;

namespace OpenTkControl
{
    public class DrawingDirective
    {
        public TranslateTransform TranslateTransform { get; }

        public ScaleTransform ScaleTransform { get; }

        public ImageSource ImageSource { get; }
        
        public bool IsNeedTransform => TranslateTransform != null || ScaleTransform != null;

        /// <summary>
        /// 是否支持异步输出UI
        /// </summary>
        public bool IsOutputAsync;

        public DrawingDirective(TranslateTransform translate, ScaleTransform scale, ImageSource imageSource,
            bool isOutputAsync = false)
        {
            this.TranslateTransform = translate;
            this.ScaleTransform = scale;
            ImageSource = imageSource;
            this.IsOutputAsync = isOutputAsync;
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