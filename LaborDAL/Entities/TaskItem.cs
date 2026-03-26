using LaborDAL.Enums;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a task/job posted by a user in the labor marketplace
    /// </summary>
    public class TaskItem : BaseEntity
    {
        /// <summary>
        /// Title of the task
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the task
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category of the task
        /// </summary>
        public TaskCategory Category { get; set; } = TaskCategory.Other;

        /// <summary>
        /// Current status of the task
        /// </summary>
        public TaskStatus Status { get; set; } = TaskStatus.Open;

        /// <summary>
        /// Budget type (Fixed, Hourly, Negotiable)
        /// </summary>
        public BudgetType BudgetType { get; set; } = BudgetType.Fixed;

        /// <summary>
        /// Budget amount for the task
        /// </summary>
        public decimal Budget { get; set; }

        /// <summary>
        /// Estimated duration in hours
        /// </summary>
        public decimal? EstimatedHours { get; set; }

        /// <summary>
        /// Date when the task should be completed by
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Date when the task needs to start (legacy - use StartTime)
        /// </summary>
        public DateTime? StartDate { get; set; }

        #region Cancellation & Lifecycle Fields

        /// <summary>
        /// Exact scheduled start time (UTC) - used for cancellation windows
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// When the task was actually started by the worker
        /// </summary>
        public DateTimeOffset? StartedAt { get; set; }

        /// <summary>
        /// When the task was cancelled
        /// </summary>
        public DateTimeOffset? CancelledAt { get; set; }

        /// <summary>
        /// Who cancelled the task (ClientId, WorkerId, or System)
        /// </summary>
        public string? CancelledBy { get; set; }

        /// <summary>
        /// Type of cancellation
        /// </summary>
        public CancellationType? CancellationType { get; set; }

        /// <summary>
        /// Reason for cancellation
        /// </summary>
        public CancellationReason CancellationReason { get; set; } = CancellationReason.NotSpecified;

        /// <summary>
        /// When the worker checked in
        /// </summary>
        public DateTimeOffset? WorkerCheckedInAt { get; set; }

        /// <summary>
        /// When the client confirmed presence
        /// </summary>
        public DateTimeOffset? ClientConfirmedAt { get; set; }

        /// <summary>
        /// No-show detection timestamp
        /// </summary>
        public DateTimeOffset? NoShowDetectedAt { get; set; }

        /// <summary>
        /// Which party was marked as no-show
        /// </summary>
        public string? NoShowParty { get; set; }

        /// <summary>
        /// Concurrency token for optimistic locking
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Idempotency key for the last operation (prevents duplicate processing)
        /// </summary>
        public string? LastOperationIdempotencyKey { get; set; }

        /// <summary>
        /// Whether the cancellation has been fully processed (financial settlement complete)
        /// </summary>
        public bool IsCancellationProcessed { get; set; }

        #endregion

        /// <summary>
        /// Task location address
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Task location URL (map link)
        /// </summary>
        public string? LocationUrl { get; set; }

        /// <summary>
        /// Geographic latitude of the task location (-90 to 90)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Geographic longitude of the task location (-180 to 180)
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// SQL Server GEOGRAPHY point for spatial queries (SRID 4326 - WGS 84)
        /// </summary>
        public Point? LocationGeography { get; set; }

        /// <summary>
        /// Country where the task is located
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// City where the task is located
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Whether the task can be done remotely
        /// </summary>
        public bool IsRemote { get; set; } = false;

        /// <summary>
        /// Number of workers needed for this task
        /// </summary>
        public int WorkersNeeded { get; set; } = 1;

        /// <summary>
        /// Skills required for the task (comma-separated)
        /// </summary>
        public string? RequiredSkills { get; set; }

        /// <summary>
        /// URLs to task attachments/images (JSON array)
        /// </summary>
        public string? AttachmentUrls { get; set; }

        /// <summary>
        /// ID of the user who posted the task
        /// </summary>
        public string PosterId { get; set; } = string.Empty;


        /// <summary>
        /// Date when the task was assigned
        /// </summary>
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Date when the task was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// View count for the task
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// Whether the task is featured (highlighted in search)
        /// </summary>
        public bool IsFeatured { get; set; } = false;

        /// <summary>
        /// Whether the task is urgent
        /// </summary>
        public bool IsUrgent { get; set; } = false;

        // Navigation properties

        /// <summary>
        /// User who posted the task
        /// </summary>
        public virtual AppUser? Poster { get; set; }


        /// <summary>
        /// Worker assigned to the task
        /// </summary>
        public ICollection<AppUser?> AssignedWorker { get; set; } = new List<AppUser?>();

        /// <summary>
        /// Applications submitted for this task
        /// </summary>
        public virtual ICollection<TaskApplication> Applications { get; set; } = new List<TaskApplication>();

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
