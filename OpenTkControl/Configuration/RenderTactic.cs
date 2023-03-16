namespace OpenTkWPFHost.Configuration
{
    /// <summary>
    /// render tactic
    /// </summary>
    public enum RenderTactic
    {
        /// <summary>
        /// use <see cref="System.Windows.Media.CompositionTarget.Rendering"/> event as render signal
        /// </summary>
        Default = 0,

        /// <summary>
        /// use internal signal
        /// </summary>
        ThroughoutPriority = 1,

        /// <summary>
        /// lowest latency
        /// </summary>
        LatencyPriority = 2,

        /// <summary>
        /// maximum frame rate
        /// </summary>
        MaxThroughout = 3,
    }
}