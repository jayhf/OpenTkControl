using System;

namespace OpenTkWPFHost
{
    public class DoubleBuffer<T>
    {
        private readonly Func<T> _createFunc;

        private readonly T[] _drawingVisual = new T[2];

        public T FrontVisual { get; private set; }
        public T BackVisual { get; private set; }

        public DoubleBuffer(Func<T> createFunc)
        {
            _createFunc = createFunc;
        }

        public void Create()
        {
            _drawingVisual[0] = _createFunc();
            _drawingVisual[1] = _createFunc();
            FrontVisual = _drawingVisual[0];
            BackVisual = _drawingVisual[1];
        }

        public void Swap()
        {
            var visual = FrontVisual;
            FrontVisual = BackVisual;
            BackVisual = visual;
        }
        
    }
}