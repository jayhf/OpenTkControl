using OpenTK;

namespace TestRenderer
{
    public static class MatrixExtension
    {
        /// <summary>
        /// 2d translation
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Matrix3 CreateTranslation(this Matrix3 identity, float x, float y)
        {
            identity.Row2.X = x;
            identity.Row2.Y = y;
            return identity;
        }
    }
}