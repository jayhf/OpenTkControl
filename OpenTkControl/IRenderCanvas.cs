namespace OpenTkWPFHost
{
    /// <summary>
    /// render canvas, better to operate in ui thread.
    /// </summary>
    public interface IRenderCanvas
    {
        bool IsAvailable { get; }

        void Create(CanvasInfo info);
    }   
}