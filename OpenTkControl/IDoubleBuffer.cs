namespace OpenTkControl
{
    public interface IDoubleBuffer
    {
        void Create(CanvasInfo info);

        IRenderCanvas GetFrontBuffer();

        IRenderCanvas GetBackBuffer();

        void SwapBuffer();
    }
}