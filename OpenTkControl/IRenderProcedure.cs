using System;
using System.Windows.Media;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    /// <summary>
    /// 渲染过程
    /// </summary>
    public interface IRenderProcedure : IDisposable
    {
        IGraphicsContext Initialize(IWindowInfo window, GLSettings settings);

        IFrameBuffer FrameBuffer { get; }

        void SizeFrame(PixelSize pixelSize);

        void PreRender();

        BufferArgs PostRender();

        /// <summary>
        /// 创建canvas不一定要使用
        /// </summary>
        /// <returns></returns>
        IRenderCanvas CreateCanvas();

        /// <summary>
        /// post render之前必须要绑定canvas
        /// </summary>
        /// <param name="canvas"></param>
        void BindCanvas(IRenderCanvas canvas);
    }
}