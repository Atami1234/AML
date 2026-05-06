using System;

namespace AmlDetectionApi.DTOs
{
    public class NetworkEdgeDto
    {
        public int From { get; set; }
        public int To { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
