using AutoMapper.Execution;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborDAL.Entities
    {
        public class Payment
        {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentType { get; set; } = "Membership";// "Membership"

        [Required]
        [StringLength(100)]
        public string Description { get; set; } = "Membership Payment";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Currency { get; set; } = "USD";

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } // "CreditCard", "DebitCard", "Cash", "BankTransfer"

        [StringLength(50)]
        public string? TransactionId { get; set; }

        [Required]
        [StringLength(20)]
        public PaymentStatus Status { get; set; } = PaymentStatus.Held; // "Pending", "Completed", "Failed", "Refunded"

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? ProcessedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Billing information
        [StringLength(200)]
        public string? BillingName { get; set; }

        [StringLength(500)]
        public string? BillingAddress { get; set; }

        [StringLength(100)]
        public string? BillingEmail { get; set; }

        // Navigation properties
        public virtual AppUser User { get; set; }

        public DateTime? ReleasedAt { get; set; }
        public virtual Booking Booking { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the dispute was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
