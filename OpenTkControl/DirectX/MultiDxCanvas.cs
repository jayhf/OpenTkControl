using System;
using OpenTkWPFHost.Abstraction;

namespace OpenTkWPFHost.DirectX
{
    /// <summary>
    /// ����d3dimage�Ļ��������ñ��������ӦDispatcher���ύ��
    /// ���������δ�ύ��������������������cpuռ�ã���ʹ��˫����
    /// </summary>
    public class MultiDxCanvas : IRenderCanvas
    {
        private readonly uint _bufferCount;
        private readonly DxCanvas[] _dxCanvasArray;

        private DxCanvas _backBuffer;

        public MultiDxCanvas():this(3)
        {
            
        }

        public MultiDxCanvas(uint bufferCount)
        {
            if (bufferCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount));
            }

            _bufferCount = bufferCount;
            _dxCanvasArray = new DxCanvas[bufferCount];
            for (int i = 0; i < bufferCount; i++)
            {
                _dxCanvasArray[i] = new DxCanvas();
            }
            // this.Swap();
        }


        private int _pointer = 0;

        public bool CanAsyncFlush { get; } = true;

        public bool Ready => _backBuffer.Ready;

        public CanvasArgs Flush(FrameArgs frame)
        {
            var dxFrameArgs = (DXFrameArgs) frame;
            return new DxCanvasArgs(dxFrameArgs.RenderTargetIntPtr, _backBuffer, dxFrameArgs.TargetInfo);
        }

        public void Swap()
        {
            _pointer++;
            var writeBufferIndex = _pointer % _bufferCount;
            _backBuffer = _dxCanvasArray[writeBufferIndex];
        }
    }
}