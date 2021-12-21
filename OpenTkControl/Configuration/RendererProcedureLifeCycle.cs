namespace OpenTkWPFHost.Configuration
{
    public enum RendererProcedureLifeCycle
    {
        /// <summary>
        /// when self unload, Renderer will be disposed (not suggest)
        /// </summary>
        Self,

        /// <summary>
        /// renderer dispose when window closed
        /// </summary>
        BoundToWindow,

        /// <summary>
        /// renderer dispose when app shutdown
        /// </summary>
        BoundToApplication,
    }
}