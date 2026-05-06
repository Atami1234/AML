using System;

namespace AmlDetectionApi.DTOs
{
    public class TransactionCsvDto
    {
        public string FromAccountNumber { get; set; } = string.Empty;
        public string ToAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Channel { get; set; } = string.Empty;
    }
}
