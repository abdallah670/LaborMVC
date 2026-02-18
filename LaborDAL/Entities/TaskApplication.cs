namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a worker's application to a task
    /// </summary>
    public class TaskApplication : BaseEntity
    {
         public int TaskItemId { get; set; }
      
       
        /// <summary>
        /// Proposed budget for completing the task
        /// </summary>
       
        public decimal ProposedBudget { get; set; }

        /// <summary>
        /// Estimated hours to complete the task
        /// </summary>
        public decimal? EstimatedHours { get; set; }

        /// <summary>
        /// Cover letter/message from the worker
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Status of the application
        /// </summary>
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        /// <summary>
        /// Date when the application was viewed by the poster
        /// </summary>
        public DateTime? ViewedAt { get; set; }

        /// <summary>
        /// Reason for rejection (if rejected)
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Date when the application was accepted/rejected
        /// </summary>
        public DateTime? RespondedAt { get; set; }

        // Navigation properties

        /// <summary>
        /// The task being applied to
        /// </summary>
        public virtual TaskItem? Task { get; set; }

        /// <summary>
        /// The worker who applied
        /// </summary>
        
        public string? WorkerId { get; set; }
        public virtual AppUser? Worker { get; set; }

    }
}
