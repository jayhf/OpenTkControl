namespace OpenTkWPFHost
{
    public class DoubleDxCanvas : IDoubleBuffer, IRenderCanvas
    {
        private readonly DxCanvas[] _dxCanvasArray = {new DxCanvas(), new DxCanvas()};

        private DxCanvas _backBuffer, _frontBuffer;

        public DoubleDxCanvas()
        {
            _backBuffer = _dxCanvasArray[0];
            _frontBuffer = _dxCanvasArray[1];
        }

        public DxCanvas GetWriteBuffer()
        {
            return _backBuffer;
        }

        public IImageBuffer GetFrontBuffer()
        {
            return _frontBuffer;
        }

        public IImageBuffer GetBackBuffer()
        {
            return _backBuffer;
        }

        public void SwapBuffer()
        {
            var mid = _backBuffer;
            _backBuffer = _frontBuffer;
            _frontBuffer = mid;
        }

        public void Create(CanvasInfo info)
        {
            foreach (var dxCanvas in _dxCanvasArray)
            {
                dxCanvas.Create(info);
            }
        }
    }
}