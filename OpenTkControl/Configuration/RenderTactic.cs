namespace OpenTkWPFHost.Configuration
{
    public enum RenderTactic
    {
        /// <summary>
        /// max Throughput£¬
        /// </summary>
        ThroughputPriority = 0,

        /// <summary>
        /// lowest latency
        /// </summary>
        LatencyPriority = 1,

        Balance = 2,
    }
}