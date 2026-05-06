using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmlDetectionApi.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Low";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
