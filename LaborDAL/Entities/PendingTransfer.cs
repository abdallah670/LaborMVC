
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaborDAL.Enums;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a pending transfer in the transfer queue
    /// Used for reliable worker payment transfers with retry mechanism
    /// Implements the transactional outbox pattern for Stripe transfers
    /// </summary>
    public class PendingTransfer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Related payment ID
        /// </summary>
        [Required]
        public int PaymentId { get; set; }

        /// <summary>
        /// Related booking ID
        /// </summary>
        [Required]
        public int BookingId { get; set; }

        /// <summary>
        /// Worker's Stripe Connect account ID
        /// </summary>
        [Required]
        [StringLength(100)]
        public string WorkerStripeAccountId { get; set; } = string.Empty;

        /// <summary>
        /// Amount to transfer (in cents for Stripe)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency (e.g., "usd")
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "usd";

        /// <summary>
        /// Stripe transfer_group ID for atomic payment tracking
        /// Groups all transfers related to a single payment
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TransferGroup { get; set; } = string.Empty;

        /// <summary>
        /// Description for the transfer
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Current status of the transfer
        /// </summary>
        [Required]
        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        /// <summary>
        /// Stripe transfer ID once completed
        /// </summary>
        [StringLength(100)]
        public string? StripeTransferId { get; set; }

        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryCount { get; set; } = 5;

        /// <summary>
        /// Timestamp when the transfer was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the transfer was last attempted
        /// </summary>
        public DateTime? LastAttemptAt { get; set; }

        /// <summary>
        /// Timestamp when the transfer should be retried
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// Timestamp when the transfer was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if transfer failed
        /// </summary>
        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Lock token to prevent concurrent processing
        /// </summary>
        [StringLength(100)]
        public string? LockToken { get; set; }

        /// <summary>
        /// Timestamp when the lock expires
        /// </summary>
        public DateTime? LockExpiryAt { get; set; }

        /// <summary>
        /// Platform fee amount retained (in cents)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformFeeAmount { get; set; }

        /// <summary>
        /// Navigation property to Payment
        /// </summary>
        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; } = null!;

        /// <summary>
        /// Check if the transfer can be processed
        /// </summary>
        public bool CanProcess()
        {
            return (Status == TransferStatus.Pending || Status == TransferStatus.Failed) 
                && RetryCount < MaxRetryCount
                && Status != TransferStatus.Cancelled
                && Status != TransferStatus.PermanentlyFailed
                && Status != TransferStatus.Completed;
        }

        /// <summary>
        /// Check if the transfer is locked
        /// </summary>
        public bool IsLocked()
        {
            return LockToken != null && LockExpiryAt > DateTime.UtcNow;
        }

        /// <summary>
        /// Calculate next retry time with exponential backoff
        /// </summary>
        public void ScheduleRetry()
        {
            RetryCount++;
            if (RetryCount >= MaxRetryCount)
            {
                Status = TransferStatus.PermanentlyFailed;
                NextRetryAt = null;
            }
            else
            {
                var backoffSeconds = Math.Min(Math.Pow(2, RetryCount) * 30, 86400); // Max 1 day
                NextRetryAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
            }
        }

        /// <summary>
        /// Mark transfer as successfully completed
        /// </summary>
        public void MarkCompleted(string stripeTransferId)
        {
            Status = TransferStatus.Completed;
            StripeTransferId = stripeTransferId;
            CompletedAt = DateTime.UtcNow;
            ErrorMessage = null;
        }

        /// <summary>
        /// Mark transfer as failed
        /// </summary>
        public void MarkFailed(string errorMessage)
        {
            ErrorMessage = errorMessage;
            LastAttemptAt = DateTime.UtcNow;
            ScheduleRetry();
        }
    }
}
