using LaborDAL.Enums;
using System.ComponentModel.DataAnnotations;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborBLL.ModelVM
{
    /// <summary>
    /// ViewModel for creating a new task
    /// </summary>
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 5000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public TaskCategory Category { get; set; } = TaskCategory.Other;

        [Required(ErrorMessage = "Budget type is required")]
        [Display(Name = "Budget Type")]
        public BudgetType BudgetType { get; set; } = BudgetType.Fixed;

        [Required(ErrorMessage = "Budget is required")]
        [Range(0.01, 1000000, ErrorMessage = "Budget must be between 0.01 and 1,000,000")]
        [Display(Name = "Budget")]
        public decimal Budget { get; set; }

        [Range(0.1, 1000, ErrorMessage = "Estimated hours must be between 0.1 and 1000")]
        [Display(Name = "Estimated Hours")]
        public decimal? EstimatedHours { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [StringLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Location URL")]
        public string? LocationUrl { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        [Display(Name = "Latitude")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        [Display(Name = "Longitude")]
        public decimal? Longitude { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "Can be done remotely")]
        public bool IsRemote { get; set; } = false;

        [Range(1, 100, ErrorMessage = "Workers needed must be between 1 and 100")]
        [Display(Name = "Workers Needed")]
        public int WorkersNeeded { get; set; } = 1;

        [StringLength(1000, ErrorMessage = "Required skills cannot exceed 1000 characters")]
        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; }

        [Display(Name = "Is Urgent")]
        public bool IsUrgent { get; set; } = false;
    }

    /// <summary>
    /// ViewModel for editing a task
    /// </summary>
    public class EditTaskViewModel : CreateTaskViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;
    }

    /// <summary>
    /// ViewModel for displaying task details
    /// </summary>
    public class TaskDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskCategory Category { get; set; }
        public string CategoryDisplay { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public BudgetType BudgetType { get; set; }
        public string BudgetTypeDisplay { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public decimal? EstimatedHours { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public string? Location { get; set; }
        public string? LocationUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public bool IsRemote { get; set; }
        public int WorkersNeeded { get; set; }
        public string? RequiredSkills { get; set; }
        public string? AttachmentUrls { get; set; }
        public string PosterId { get; set; } = string.Empty;
        public string? PosterName { get; set; }
        public string? PosterProfilePicture { get; set; }
        public decimal? PosterRating { get; set; }
        public string? AssignedWorkerId { get; set; }
        public string? AssignedWorkerName { get; set; }
        public string? AssignedWorkerProfilePicture { get; set; }
        public decimal? AssignedWorkerRating { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CancellationReason { get; set; }
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsUrgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ApplicationCount { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanApply { get; set; }
        public bool HasApplied { get; set; }
        public List<TaskApplicationViewModel> Applications { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for task list item
    /// </summary>
    public class TaskListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskCategory Category { get; set; }
        public string CategoryDisplay { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public BudgetType BudgetType { get; set; }
        public string BudgetTypeDisplay { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsRemote { get; set; }
        public string? PosterName { get; set; }
        public string? PosterProfilePicture { get; set; }
        public decimal? PosterRating { get; set; }
        public int ApplicationCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsUrgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public double? Distance { get; set; }
    }

    /// <summary>
    /// ViewModel for task application
    /// </summary>
    public class TaskApplicationViewModel
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string WorkerId { get; set; } = string.Empty;
        public string? WorkerName { get; set; }
        public string? WorkerProfilePicture { get; set; }
        public decimal? WorkerRating { get; set; }
        public string? WorkerSkills { get; set; }
        public decimal ProposedBudget { get; set; }
        public decimal? EstimatedHours { get; set; }
        public string? Message { get; set; }
        public ApplicationStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ViewedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// ViewModel for creating a task application
    /// </summary>
    public class CreateApplicationViewModel
    {
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Proposed budget is required")]
        [Range(0.01, 1000000, ErrorMessage = "Proposed budget must be between 0.01 and 1,000,000")]
        [Display(Name = "Your Proposed Budget")]
        public decimal ProposedBudget { get; set; }

        [Range(0.1, 1000, ErrorMessage = "Estimated hours must be between 0.1 and 1000")]
        [Display(Name = "Estimated Hours")]
        public decimal? EstimatedHours { get; set; }

        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        [Display(Name = "Cover Letter / Message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// ViewModel for task search filters
    /// </summary>
    public class TaskSearchViewModel
    {
        public string? Keyword { get; set; }
        public TaskCategory? Category { get; set; }
        public TaskStatus? Status { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public bool? IsRemote { get; set; }
        public bool? IsUrgent { get; set; }
        public string? Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double? RadiusKm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public List<TaskListViewModel> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}