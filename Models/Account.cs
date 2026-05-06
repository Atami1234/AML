using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmlDetectionApi.Models
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string AccountType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [InverseProperty("FromAccount")]
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();

        [InverseProperty("ToAccount")]
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
    }
}
