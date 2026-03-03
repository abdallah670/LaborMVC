
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaborDAL.Enums;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a Saga instance for managing distributed transactions
    /// Saga pattern implementation for atomic operations across multiple services
    /// </summary>
    public class SagaInstance
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type of saga (e.g., "PaymentRelease", "BookingCancellation")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SagaType { get; set; } = string.Empty;

        /// <summary>
        /// Correlation ID for distributed tracing
        /// </summary>
        [Required]
        [StringLength(100)]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Current status of the saga
        /// </summary>
        [Required]
        public SagaStatus Status { get; set; } = SagaStatus.Created;

        /// <summary>
        /// Current step index being executed
        /// </summary>
        public int CurrentStepIndex { get; set; } = 0;

        /// <summary>
        /// Total number of steps in the saga
        /// </summary>
        public int TotalSteps { get; set; } = 0;

        /// <summary>
        /// Serialized saga data/context
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? SagaData { get; set; }

        /// <summary>
        /// Related entity type (e.g., "Booking", "Payment")
        /// </summary>
        [StringLength(50)]
        public string? AggregateType { get; set; }

        /// <summary>
        /// Related entity ID
        /// </summary>
        public int? AggregateId { get; set; }

        /// <summary>
        /// Timestamp when the saga was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the saga was started
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Timestamp when the saga was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Timestamp when the saga compensation started
        /// </summary>
        public DateTime? CompensatedAt { get; set; }

        /// <summary>
        /// Error message if saga failed
        /// </summary>
        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace if saga failed
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
        /// Navigation property for saga steps
        /// </summary>
        public virtual ICollection<SagaStep> Steps { get; set; } = new List<SagaStep>();

        /// <summary>
        /// Check if the saga can be executed
        /// </summary>
        public bool CanExecute()
        {
            return Status == SagaStatus.Created || Status == SagaStatus.Running;
        }

        /// <summary>
        /// Check if the saga is locked
        /// </summary>
        public bool IsLocked()
        {
            return LockToken != null && LockExpiryAt > DateTime.UtcNow;
        }

        /// <summary>
        /// Mark saga as started
        /// </summary>
        public void MarkStarted()
        {
            Status = SagaStatus.Running;
            StartedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark saga as completed successfully
        /// </summary>
        public void MarkCompleted()
        {
            Status = SagaStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark saga as compensating
        /// </summary>
        public void MarkCompensating()
        {
            Status = SagaStatus.Compensating;
        }

        /// <summary>
        /// Mark saga as compensated
        /// </summary>
        public void MarkCompensated()
        {
            Status = SagaStatus.Compensated;
            CompensatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark saga as failed
        /// </summary>
        public void MarkFailed(string errorMessage, string? stackTrace = null)
        {
            Status = SagaStatus.Failed;
            ErrorMessage = errorMessage;
            ErrorStackTrace = stackTrace;
        }
    }
}
