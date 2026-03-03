
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a single step in a Saga
    /// Tracks execution and compensation of individual steps
    /// </summary>
    public class SagaStep
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Reference to the parent saga
        /// </summary>
        [Required]
        public Guid SagaInstanceId { get; set; }

        /// <summary>
        /// Step name/identifier
        /// </summary>
        [Required]
        [StringLength(100)]
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// Order of execution in the saga
        /// </summary>
        [Required]
        public int StepOrder { get; set; }

        /// <summary>
        /// Indicates if this step has been executed
        /// </summary>
        [Required]
        public bool IsExecuted { get; set; } = false;

        /// <summary>
        /// Indicates if this step has been compensated
        /// </summary>
        [Required]
        public bool IsCompensated { get; set; } = false;

        /// <summary>
        /// Timestamp when the step was executed
        /// </summary>
        public DateTime? ExecutedAt { get; set; }

        /// <summary>
        /// Timestamp when the step was compensated
        /// </summary>
        public DateTime? CompensatedAt { get; set; }

        /// <summary>
        /// Serialized step input data
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? InputData { get; set; }

        /// <summary>
        /// Serialized step result data
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ResultData { get; set; }

        /// <summary>
        /// Error message if step failed
        /// </summary>
        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace if step failed
        /// </summary>
        public string? ErrorStackTrace { get; set; }

        /// <summary>
        /// Navigation property to parent saga
        /// </summary>
        [ForeignKey("SagaInstanceId")]
        public virtual SagaInstance SagaInstance { get; set; } = null!;

        /// <summary>
        /// Mark step as executed
        /// </summary>
        public void MarkExecuted(object? result = null)
        {
            IsExecuted = true;
            ExecutedAt = DateTime.UtcNow;
            if (result != null)
            {
                ResultData = System.Text.Json.JsonSerializer.Serialize(result);
            }
        }

        /// <summary>
        /// Mark step as failed
        /// </summary>
        public void MarkFailed(string errorMessage, string? stackTrace = null)
        {
            ErrorMessage = errorMessage;
            ErrorStackTrace = stackTrace;
        }

        /// <summary>
        /// Mark step as compensated
        /// </summary>
        public void MarkCompensated()
        {
            IsCompensated = true;
            CompensatedAt = DateTime.UtcNow;
        }
    }
}
