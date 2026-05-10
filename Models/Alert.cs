using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmlDetectionApi.Models
{
    public class Alert
    {
        [Key]
        public int AlertId { get; set; }

        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        [ForeignKey("Account")]
        public int AccountId { get; set; }
        public Account? Account { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal RiskScore { get; set; }
        public string RiskLevel { get; set; } = "Medium";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AlertReason> AlertReasons { get; set; } = new List<AlertReason>();
        public BlockchainLog? BlockchainLog { get; set; }
    }
}
