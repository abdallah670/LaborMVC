using System.ComponentModel.DataAnnotations;
using LaborDAL.Enums;

namespace LaborBLL.ModelVM
{
    /// <summary>
    /// ViewModel for creating a new dispute
    /// </summary>
    public class CreateDisputeViewModel
    {
        [Required(ErrorMessage = "Booking ID is required")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for the dispute")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Reason must be between 20 and 2000 characters")]
        [Display(Name = "Reason for Dispute")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for displaying a dispute in a list
    /// </summary>
    public class DisputeListViewModel
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string RaisedByName { get; set; } = string.Empty;
        public DisputeStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying detailed dispute information
    /// </summary>
    public class DisputeDetailsViewModel
    {
        public int Id { get; set; }
        public int BookingId { get; set; }

        // Task Information
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;

        // Poster Information
        public string PosterId { get; set; } = string.Empty;
        public string PosterName { get; set; } = string.Empty;
        public string? PosterPhone { get; set; }

        // Worker Information
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string? WorkerPhone { get; set; }

        // Dispute Information
        public string RaisedBy { get; set; } = string.Empty;
        public string RaisedByName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DisputeStatus Status { get; set; }

        // Resolution Information
        public string? Resolution { get; set; }
        public ResolutionType? ResolutionType { get; set; }
        public int? WorkerPercentage { get; set; }
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Booking Information
        public decimal AgreedRate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public BookingStatus BookingStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        // Computed properties
        public bool CanResolve => Status != DisputeStatus.Resolved;
        public string StatusBadgeClass => Status switch
        {
            DisputeStatus.Open => "bg-danger",
            DisputeStatus.UnderReview => "bg-warning",
            DisputeStatus.Resolved => "bg-success",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// ViewModel for resolving a dispute
    /// </summary>
    public class ResolveDisputeViewModel
    {
        [Required(ErrorMessage = "Dispute ID is required")]
        public int DisputeId { get; set; }

        [Required(ErrorMessage = "Please provide resolution notes")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Resolution must be between 10 and 2000 characters")]
        [Display(Name = "Resolution Notes")]
        public string Resolution { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a resolution type")]
        [Display(Name = "Resolution Type")]
        public ResolutionType ResolutionType { get; set; }

        [Range(0, 100, ErrorMessage = "Worker percentage must be between 0 and 100")]
        [Display(Name = "Worker Percentage")]
        public int? WorkerPercentage { get; set; }

        // Display properties
        public string TaskTitle { get; set; } = string.Empty;
        public decimal AgreedRate { get; set; }
        public string PosterName { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for updating dispute status
    /// </summary>
    public class UpdateDisputeStatusViewModel
    {
        [Required]
        public int DisputeId { get; set; }

        [Required]
        public DisputeStatus NewStatus { get; set; }
    }
}
