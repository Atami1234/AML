using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmlDetectionApi.Models
{
    public class AlertReason
    {
        [Key]
        public int AlertReasonId { get; set; }

        [ForeignKey("Alert")]
        public int AlertId { get; set; }
        public Alert? Alert { get; set; }

        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
