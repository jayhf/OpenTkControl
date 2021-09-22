namespace OpenTkWPFHost
{
    /// <summary>
    /// double buffer 
    /// </summary>
    public interface IDoubleBuffer
    {
        IRenderBuffer GetFrontBuffer();

        IRenderBuffer GetBackBuffer();

        void SwapBuffer();
    }
}