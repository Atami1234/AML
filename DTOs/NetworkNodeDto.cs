namespace AmlDetectionApi.DTOs
{
    public class NetworkNodeDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
    }
}
