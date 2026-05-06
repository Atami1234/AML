namespace AmlDetectionApi.DTOs
{
    public class AccountMetricsDto
    {
        public int IncomingCount { get; set; }
        public int OutgoingCount { get; set; }
        public int UniqueSenders { get; set; }
        public int UniqueReceivers { get; set; }
        public bool IsFunnelAccount { get; set; }
        public bool IsCircularPatternDetected { get; set; }
    }
}
