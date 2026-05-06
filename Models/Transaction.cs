using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmlDetectionApi.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [ForeignKey("FromAccount")]
        public int FromAccountId { get; set; }
        public Account? FromAccount { get; set; }

        [ForeignKey("ToAccount")]
        public int ToAccountId { get; set; }
        public Account? ToAccount { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Channel { get; set; } = "Online";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
