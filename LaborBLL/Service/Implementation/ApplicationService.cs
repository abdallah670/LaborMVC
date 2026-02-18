using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for task application-related business operations
    /// </summary>
    public class ApplicationService : IApplicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(
            IUnitOfWork unitOfWork,
            ApplicationDbContext context,
            ILogger<ApplicationService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _taskRepository = unitOfWork.Tasks;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new application to a task
        /// </summary>
        public async Task<Response<int>> CreateApplicationAsync(CreateApplicationViewModel model, string workerId)
        {
            try
            {
                // Get the task
                var task = await _taskRepository.GetByIdAsync(model.TaskId);
                if (task == null)
                {
                    return new Response<int>(0, false, "Task not found.");
                }

                // Validate task is open
                if (task.Status != TaskStatus.Open)
                {
                    return new Response<int>(0, false, "This task is no longer accepting applications.");
                }

                // Check if user is the poster
                if (task.PosterId == workerId)
                {
                    return new Response<int>(0, false, "You cannot apply to your own task.");
                }

                // Check if already applied
                var existingApplication = await _context.TaskApplications
                    .FirstOrDefaultAsync(a => a.TaskItemId == model.TaskId && a.WorkerId == workerId);
                
                if (existingApplication != null)
                {
                    return new Response<int>(0, false, "You have already applied to this task.");
                }

                // Create the application
                var application = new TaskApplication
                {
                    TaskItemId = model.TaskId,
                    WorkerId = workerId,
                    ProposedBudget = model.ProposedBudget,
                    EstimatedHours = model.EstimatedHours,
                    Message = model.Message,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskApplications.Add(application);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Application created: {ApplicationId} for task {TaskId} by {WorkerId}", 
                    application.Id, model.TaskId, workerId);

                return new Response<int>(application.Id, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application for task: {TaskId}", model.TaskId);
                return new Response<int>(0, false, "An error occurred while creating the application.");
            }
        }

        /// <summary>
        /// Gets an application by its ID
        /// </summary>
        public async Task<Response<TaskApplicationViewModel>> GetApplicationByIdAsync(int id)
        {
            try
            {
                var application = await _context.TaskApplications
                    .Include(a => a.Task)
                    .Include(a => a.Worker)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return new Response<TaskApplicationViewModel>(null, false, "Application not found.");
                }

                var viewModel = MapToViewModel(application);
                return new Response<TaskApplicationViewModel>(viewModel, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application: {ApplicationId}", id);
                return new Response<TaskApplicationViewModel>(null, false, "An error occurred while retrieving the application.");
            }
        }

        /// <summary>
        /// Withdraws an application
        /// </summary>
        public async Task<Response<bool>> WithdrawApplicationAsync(int applicationId, string workerId)
        {
            try
            {
                var application = await _context.TaskApplications.FindAsync(applicationId);
                
                if (application == null)
                {
                    return new Response<bool>(false, false, "Application not found.");
                }

                // Check ownership
                if (application.WorkerId != workerId)
                {
                    return new Response<bool>(false, false, "You can only withdraw your own applications.");
                }

                // Check if already processed
                if (application.Status != ApplicationStatus.Pending)
                {
                    return new Response<bool>(false, false, "Cannot withdraw an application that has already been processed.");
                }

                application.Status = ApplicationStatus.Withdrawn;
                application.RespondedAt = DateTime.UtcNow;

                _context.TaskApplications.Update(application);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Application withdrawn: {ApplicationId}", applicationId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application: {ApplicationId}", applicationId);
                return new Response<bool>(false, false, "An error occurred while withdrawing the application.");
            }
        }

        /// <summary>
        /// Gets all applications for a specific task
        /// </summary>
        public async Task<Response<IEnumerable<TaskApplicationViewModel>>> GetApplicationsByTaskAsync(int taskId, string posterId)
        {
            try
            {
                // Verify the user is the task poster
                var task = await _taskRepository.GetByIdAsync(taskId);
                if (task == null)
                {
                    return new Response<IEnumerable<TaskApplicationViewModel>>(null, false, "Task not found.");
                }

                if (task.PosterId != posterId)
                {
                    return new Response<IEnumerable<TaskApplicationViewModel>>(null, false, "You can only view applications for your own tasks.");
                }

                var applications = await _context.TaskApplications
                    .Include(a => a.Worker)
                    .Include(a => a.Task)
                    .Where(a => a.TaskItemId == taskId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var viewModels = applications.Select(a => MapToViewModel(a));
                return new Response<IEnumerable<TaskApplicationViewModel>>(viewModels, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications for task: {TaskId}", taskId);
                return new Response<IEnumerable<TaskApplicationViewModel>>(null, false, "An error occurred while retrieving applications.");
            }
        }

        /// <summary>
        /// Gets all applications submitted by a worker
        /// </summary>
        public async Task<Response<IEnumerable<TaskApplicationViewModel>>> GetApplicationsByWorkerAsync(string workerId)
        {
            try
            {
                var applications = await _context.TaskApplications
                    .Include(a => a.Worker)
                    .Include(a => a.Task)
                        .ThenInclude(t => t.Poster)
                    .Where(a => a.WorkerId == workerId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var viewModels = applications.Select(a => MapToViewModel(a));
                return new Response<IEnumerable<TaskApplicationViewModel>>(viewModels, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications for worker: {WorkerId}", workerId);
                return new Response<IEnumerable<TaskApplicationViewModel>>(null, false, "An error occurred while retrieving your applications.");
            }
        }

        /// <summary>
        /// Accepts an application (creates a booking)
        /// </summary>
        public async Task<Response<bool>> AcceptApplicationAsync(int applicationId, string posterId)
        {
            try
            {
                var application = await _context.TaskApplications
                    .Include(a => a.Task)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new Response<bool>(false, false, "Application not found.");
                }

                // Verify ownership
                if (application.Task?.PosterId != posterId)
                {
                    return new Response<bool>(false, false, "You can only accept applications for your own tasks.");
                }

                // Check if application is pending
                if (application.Status != ApplicationStatus.Pending)
                {
                    return new Response<bool>(false, false, "This application has already been processed.");
                }

                // Check if task is still open
                if (application.Task.Status != TaskStatus.Open)
                {
                    return new Response<bool>(false, false, "This task is no longer available.");
                }

                // Accept the application
                application.Status = ApplicationStatus.Accepted;
                application.RespondedAt = DateTime.UtcNow;

                // Update task status
                application.Task.Status = TaskStatus.Assigned;
                application.Task.AssignedAt = DateTime.UtcNow;

                // Create booking
                var booking = new Booking(
                    application.ProposedBudget,
                    application.Task.StartDate,
                    application.Task.DueDate,
                    application.TaskItemId,
                    application.WorkerId ?? "0"

                );

                _context.Bookings.Add(booking);

                // Reject other pending applications for this task
                var otherApplications = await _context.TaskApplications
                    .Where(a => a.TaskItemId == application.TaskItemId && a.Id != applicationId && a.Status == ApplicationStatus.Pending)
                    .ToListAsync();

                foreach (var other in otherApplications)
                {
                    other.Status = ApplicationStatus.Rejected;
                    other.RespondedAt = DateTime.UtcNow;
                    other.RejectionReason = "Another worker was selected for this task.";
                }

                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Application accepted: {ApplicationId}", applicationId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting application: {ApplicationId}", applicationId);
                return new Response<bool>(false, false, "An error occurred while accepting the application.");
            }
        }

        /// <summary>
        /// Rejects an application
        /// </summary>
        public async Task<Response<bool>> RejectApplicationAsync(int applicationId, string posterId, string? reason = null)
        {
            try
            {
                var application = await _context.TaskApplications
                    .Include(a => a.Task)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new Response<bool>(false, false, "Application not found.");
                }

                // Verify ownership
                if (application.Task?.PosterId != posterId)
                {
                    return new Response<bool>(false, false, "You can only reject applications for your own tasks.");
                }

                // Check if application is pending
                if (application.Status != ApplicationStatus.Pending)
                {
                    return new Response<bool>(false, false, "This application has already been processed.");
                }

                application.Status = ApplicationStatus.Rejected;
                application.RejectionReason = reason;
                application.RespondedAt = DateTime.UtcNow;

                _context.TaskApplications.Update(application);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Application rejected: {ApplicationId}", applicationId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application: {ApplicationId}", applicationId);
                return new Response<bool>(false, false, "An error occurred while rejecting the application.");
            }
        }

        /// <summary>
        /// Checks if a user has already applied to a task
        /// </summary>
        public async Task<Response<bool>> HasUserAppliedAsync(int taskId, string userId)
        {
            try
            {
                var hasApplied = await _context.TaskApplications
                    .AnyAsync(a => a.TaskItemId == taskId && a.WorkerId == userId);

                return new Response<bool>(hasApplied, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking application status for task: {TaskId}", taskId);
                return new Response<bool>(false, false, "An error occurred while checking application status.");
            }
        }

        /// <summary>
        /// Gets the count of applications for a task
        /// </summary>
        public async Task<Response<int>> GetApplicationCountAsync(int taskId)
        {
            try
            {
                var count = await _context.TaskApplications
                    .CountAsync(a => a.TaskItemId == taskId);

                return new Response<int>(count, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application count for task: {TaskId}", taskId);
                return new Response<int>(0, false, "An error occurred while getting application count.");
            }
        }

        /// <summary>
        /// Marks an application as viewed by the poster
        /// </summary>
        public async Task<Response<bool>> MarkAsViewedAsync(int applicationId, string posterId)
        {
            try
            {
                var application = await _context.TaskApplications
                    .Include(a => a.Task)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new Response<bool>(false, false, "Application not found.");
                }

                // Verify ownership
                if (application.Task?.PosterId != posterId)
                {
                    return new Response<bool>(false, false, "You can only mark applications for your own tasks.");
                }

                if (application.ViewedAt == null)
                {
                    application.ViewedAt = DateTime.UtcNow;
                    _context.TaskApplications.Update(application);
                    await _unitOfWork.SaveAsync();
                }

                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking application as viewed: {ApplicationId}", applicationId);
                return new Response<bool>(false, false, "An error occurred while updating the application.");
            }
        }

        #region Private Helper Methods

        private TaskApplicationViewModel MapToViewModel(TaskApplication application)
        {
            return new TaskApplicationViewModel
            {
                Id = application.Id,
                TaskId = application.TaskItemId,
                WorkerId = application.WorkerId ?? string.Empty,
                WorkerName = application.Worker != null 
                    ? $"{application.Worker.FirstName} {application.Worker.LastName}" 
                    : null,
                WorkerProfilePicture = application.Worker?.ProfilePictureUrl,
                WorkerRating = application.Worker?.AverageRating,
                WorkerSkills = application.Worker?.Skills,
                ProposedBudget = application.ProposedBudget,
                EstimatedHours = application.EstimatedHours,
                Message = application.Message,
                Status = application.Status,
                StatusDisplay = application.Status.ToString(),
                CreatedAt = application.CreatedAt,
                ViewedAt = application.ViewedAt,
                RespondedAt = application.RespondedAt,
                RejectionReason = application.RejectionReason
            };
        }

        #endregion
    }
}
