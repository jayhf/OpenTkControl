using System;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenTkWPFHost
{
    /// <summary>
    /// ����d3dimage�Ļ��������ñ��������ӦDispatcher���ύ��
    /// ���������δ�ύ��������������������cpuռ�ã���ʹ��˫����
    /// </summary>
    [Obsolete("˫���岢��������d3dimage")]
    public class DoubleDxCanvas : IRenderCanvas
    {
        private readonly DxCanvas[] _dxCanvasArray;// = {new DxCanvas(), new DxCanvas(), new DxCanvas()};

        private DxCanvas _backBuffer, _frontBuffer;

        public DoubleDxCanvas()
        {
            _backBuffer = _dxCanvasArray[0];
            _frontBuffer = _dxCanvasArray[1];
        }

        public DxCanvas BackBuffer
        {
            get
            {
                var i = pointer % 2 + 1;
                return _dxCanvasArray[i];
            }
        }

        public DxCanvas FrontBuffer
        {
            get
            {
                var i = pointer % 2;
                return _dxCanvasArray[i];
            }
        }

        private int pointer = 0;

        public void SwapBuffer()
        {
            pointer++;
            /*var mid = BackBuffer;
            _backBuffer = _frontBuffer;
            _frontBuffer = mid;*/
        }

        public bool CanAsyncFlush { get; }
        public bool IsDirty { get; }
        public bool Ready { get; }

        public void Allocate(CanvasInfo info)
        {
            foreach (var dxCanvas in _dxCanvasArray)
            {
                dxCanvas.Allocate(info);
            }
        }

        public void Begin()
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }

        public void FlushFrame(DrawingContext context)
        {
            throw new NotImplementedException();
        }
    }
}