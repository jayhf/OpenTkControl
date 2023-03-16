using System;
using OpenTkWPFHost.Abstraction;
using OpenTkWPFHost.Core;

namespace OpenTkWPFHost.DirectX
{
    /// <summary>
    /// ����d3dimage�Ļ��������ñ��������ӦDispatcher���ύ��
    /// ���������δ�ύ��������������������cpuռ�ã���ʹ��˫����
    /// </summary>
    public class MultiDxCanvas : GenericMultiBuffer<DxCanvas>
    {
        public MultiDxCanvas(int bufferCount = 3) : base(bufferCount, ((i, canvas) => new DxCanvas()))
        {
        }

        public bool CanAsyncFlush { get; } = true;

        public bool Ready => this.GetBackBuffer().Ready;

        public CanvasArgs Flush(FrameArgs frame)
        {
            var dxFrameArgs = (DXFrameArgs)frame;
            return new DxCanvasArgs(dxFrameArgs.RenderTargetIntPtr, this.GetBackBuffer(), dxFrameArgs.TargetInfo);
        }
    }
}