using LaborDAL.Enums;
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
        /// Date when the task needs to start
        /// </summary>
        public DateTime? StartDate { get; set; }

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
        /// Reason for cancellation (if cancelled)
        /// </summary>
        public string? CancellationReason { get; set; }

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
        public  ICollection<AppUser?> AssignedWorker { get; set; } = new List<AppUser?>();

        /// <summary>
        /// Applications submitted for this task
        /// </summary>
        public virtual ICollection<TaskApplication> Applications { get; set; } = new List<TaskApplication>();
    }
}
