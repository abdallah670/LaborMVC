
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaborDAL.Enums;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Outbox pattern implementation for reliable message delivery
    /// Ensures eventual consistency between database and external services
    /// </summary>
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type of message/event to be processed
        /// </summary>
        [Required]
        [StringLength(100)]
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Serialized message payload
        /// </summary>
        [Required]
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Correlation ID for distributed tracing
        /// </summary>
        [StringLength(100)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Related entity type (e.g., "Payment", "Booking")
        /// </summary>
        [StringLength(50)]
        public string? AggregateType { get; set; }

        /// <summary>
        /// Related entity ID
        /// </summary>
        public int? AggregateId { get; set; }

        /// <summary>
        /// Current status of the message
        /// </summary>
        [Required]
        public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

        /// <summary>
        /// Number of processing attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryCount { get; set; } = 5;

        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the message should be processed (for delayed processing)
        /// </summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>
        /// Timestamp when the message was last processed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Timestamp when the message processing completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace if processing failed
        /// </summary>
        public string? ErrorStackTrace { get; set; }

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
        /// Additional headers/metadata for the message
        /// </summary>
        [StringLength(1000)]
        public string? Headers { get; set; }

        /// <summary>
        /// Check if the message can be processed
        /// </summary>
        public bool CanProcess()
        {
            return Status == OutboxMessageStatus.Pending || 
                   (Status == OutboxMessageStatus.Failed && RetryCount < MaxRetryCount);
        }

        /// <summary>
        /// Check if the message is locked
        /// </summary>
        public bool IsLocked()
        {
            return LockToken != null && LockExpiryAt > DateTime.UtcNow;
        }

        /// <summary>
        /// Increment retry count and set next scheduled time with exponential backoff
        /// </summary>
        public void IncrementRetry()
        {
            RetryCount++;
            var backoffSeconds = Math.Min(Math.Pow(2, RetryCount) * 10, 3600); // Max 1 hour
            ScheduledAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
        }
    }
}
