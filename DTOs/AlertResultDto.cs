using System;
using System.Collections.Generic;

namespace AmlDetectionApi.DTOs
{
    public class AlertResultDto
    {
        public int AlertId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<AlertReasonDto> Reasons { get; set; } = new List<AlertReasonDto>();
        public string BlockchainHash { get; set; } = string.Empty;
        public string BlockReference { get; set; } = string.Empty;
    }

    public class AlertReasonDto
    {
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
