using System.Windows.Media;

namespace OpenTkControl
{
    public struct DrawingDirective
    {
        public static  DrawingDirective None => new DrawingDirective();

        public TranslateTransform TranslateTransform { get; }

        public ScaleTransform ScaleTransform { get; }

        /// <summary>
        /// 是否支持异步输出UI
        /// </summary>
        public bool IsOutputAsync;

        public DrawingDirective(TranslateTransform translate, ScaleTransform scale, bool isOutputAsync = false)
        {
            this.TranslateTransform = translate;
            this.ScaleTransform = scale;
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