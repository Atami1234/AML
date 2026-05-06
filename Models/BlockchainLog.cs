using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmlDetectionApi.Models
{
    public class BlockchainLog
    {
        [Key]
        public int BlockchainLogId { get; set; }

        [ForeignKey("Alert")]
        public int? AlertId { get; set; }
        public Alert? Alert { get; set; }

        [ForeignKey("Transaction")]
        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        public string LogType { get; set; } = "Alert"; // "Transaction" or "Alert"

        public string DataHash { get; set; } = string.Empty;
        public string BlockReference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
