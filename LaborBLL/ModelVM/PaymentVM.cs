using System;
using System.ComponentModel.DataAnnotations;

namespace LaborBLL.ModelVM
{
    public class PaymentVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "User is required")]
        public string UserId { get; set; }
        [Required(ErrorMessage = "Booking Id is required")]
        public int BookingId { get; set; } 

        [Required(ErrorMessage = "Payment type is required")]
        [StringLength(50)]
        public string PaymentType { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(100)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string Currency { get; set; } = "USD";

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(20)]
        public string PaymentMethod { get; set; }

        [StringLength(50)]
        public string? TransactionId { get; set; }

        [StringLength(100)]
        public string? ClientSecret { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ProcessedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? BillingName { get; set; }

        [StringLength(500)]
        public string? BillingAddress { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? BillingEmail { get; set; }
        
        public decimal? AgreedRate { get; set; }


        public bool IsActive { get; set; } = true;
    }
}
