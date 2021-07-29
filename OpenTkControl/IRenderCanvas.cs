namespace OpenTkWPFHost
{
    /// <summary>
    /// render canvas, better to operate in ui thread.
    /// </summary>
    public interface IRenderCanvas
    {
        void Create(CanvasInfo info);
    }   
}