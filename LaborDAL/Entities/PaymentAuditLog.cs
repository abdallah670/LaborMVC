using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Audit trail for payment status changes
    /// Tracks who changed what and when for compliance and dispute resolution
    /// </summary>
    public class PaymentAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        public PaymentStatus OldStatus { get; set; }

        [Required]
        public PaymentStatus NewStatus { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? ChangedBy { get; set; } // User ID or "System"

        [StringLength(500)]
        public string? Reason { get; set; } // Why the change occurred

        [StringLength(50)]
        public string? TransactionId { get; set; } // Stripe transaction reference

        [StringLength(50)]
        public string? IdempotencyKey { get; set; } // For tracking idempotent operations

        [StringLength(2000)]
        public string? AdditionalData { get; set; } // JSON data for extra context

        // Navigation property
        public virtual Payment Payment { get; set; }
    }
}
