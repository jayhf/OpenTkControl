namespace OpenTkWPFHost
{
    public enum RenderTactic
    {
        // Balance = 2,

        /// <summary>
        /// max Throughput£¬
        /// </summary>
        ThroughputPriority = 0,

        /// <summary>
        /// lowest latency
        /// </summary>
        LatencyPriority = 1,
    }
}