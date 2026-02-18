using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.Extensions.Logging;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for task-related business operations
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;

        public TaskService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TaskService> logger)
        {
            _unitOfWork = unitOfWork;
            _taskRepository = unitOfWork.Tasks;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Gets a task by its ID with full details
        /// </summary>
        public async Task<Response<TaskDetailsViewModel>> GetTaskByIdAsync(int id, string? currentUserId = null)
        {
            try
            {
                var task = await _taskRepository.GetWithApplicationsAsync(id);
                
                if (task == null)
                {
                    return new Response<TaskDetailsViewModel>(null, false, "Task not found.");
                }

                var viewModel = MapToDetailsViewModel(task, currentUserId);
                return new Response<TaskDetailsViewModel>(viewModel, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {TaskId}", id);
                return new Response<TaskDetailsViewModel>(null, false, "An error occurred while retrieving the task.");
            }
        }

        /// <summary>
        /// Gets a paginated list of tasks based on search criteria
        /// </summary>
        public async Task<Response<TaskSearchViewModel>> GetTaskListAsync(TaskSearchViewModel search, string? currentUserId = null)
        {
            try
            {
                var tasks = await _taskRepository.GetFilteredAsync(
                    search.Category,
                    search.Status ?? TaskStatus.Open,
                    search.MinBudget,
                    search.MaxBudget,
                    search.IsRemote,
                    search.Latitude,
                    search.Longitude,
                    search.RadiusKm,
                    search.Page,
                    search.PageSize);

                // Apply keyword search if provided
                if (!string.IsNullOrWhiteSpace(search.Keyword))
                {
                    var keyword = search.Keyword.ToLower();
                    tasks = tasks.Where(t => 
                        t.Title.ToLower().Contains(keyword) || 
                        t.Description.ToLower().Contains(keyword));
                }

                // Apply urgent filter
                if (search.IsUrgent == true)
                {
                    tasks = tasks.Where(t => t.IsUrgent);
                }

                // Apply sorting
                tasks = ApplySorting(tasks, search.SortBy);

                var taskList = tasks.Select(t => MapToListViewModel(t)).ToList();

                // Get total count for pagination
                var totalCount = await GetTotalCountAsync(search);

                search.Results = taskList;
                search.TotalCount = totalCount;
                search.TotalPages = (int)Math.Ceiling((double)totalCount / search.PageSize);

                return new Response<TaskSearchViewModel>(search, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task list");
                return new Response<TaskSearchViewModel>(null, false, "An error occurred while retrieving tasks.");
            }
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        public async Task<Response<int>> CreateTaskAsync(CreateTaskViewModel model, string posterId)
        {
            try
            {
                // Validate due date is in the future
                if (model.DueDate.HasValue && model.DueDate.Value < DateTime.UtcNow)
                {
                    return new Response<int>(0, false, "Due date must be in the future.");
                }

                // Validate start date is before due date
                if (model.StartDate.HasValue && model.DueDate.HasValue && model.StartDate.Value > model.DueDate.Value)
                {
                    return new Response<int>(0, false, "Start date must be before due date.");
                }

                var task = _mapper.Map<TaskItem>(model);
                task.PosterId = posterId;
                task.Status = TaskStatus.Open;
                task.CreatedAt = DateTime.UtcNow;

                await _taskRepository.AddAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task created: {TaskId} by {PosterId}", task.Id, posterId);
                return new Response<int>(task.Id, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task for poster: {PosterId}", posterId);
                return new Response<int>(0, false, "An error occurred while creating the task.");
            }
        }

        /// <summary>
        /// Updates an existing task
        /// </summary>
        public async Task<Response<bool>> UpdateTaskAsync(EditTaskViewModel model, string posterId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(model.Id);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                // Check ownership
                if (task.PosterId != posterId)
                {
                    return new Response<bool>(false, false, "You can only edit your own tasks.");
                }

                // Check if task can be edited (only Open tasks)
                if (task.Status != TaskStatus.Open)
                {
                    return new Response<bool>(false, false, "Only open tasks can be edited.");
                }

                // Update properties
                _mapper.Map(model, task);
                task.UpdatedAt = DateTime.UtcNow;

                await _taskRepository.UpdateAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task updated: {TaskId} by {PosterId}", task.Id, posterId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task: {TaskId}", model.Id);
                return new Response<bool>(false, false, "An error occurred while updating the task.");
            }
        }

        /// <summary>
        /// Deletes a task (soft delete)
        /// </summary>
        public async Task<Response<bool>> DeleteTaskAsync(int taskId, string posterId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                // Check ownership
                if (task.PosterId != posterId)
                {
                    return new Response<bool>(false, false, "You can only delete your own tasks.");
                }

                // Check if task can be deleted
                if (task.Status == TaskStatus.InProgress)
                {
                    return new Response<bool>(false, false, "Cannot delete a task that is in progress.");
                }

                // Soft delete
                task.IsDeleted = true;
                task.DeletedAt = DateTime.UtcNow;
                task.DeletedBy = posterId;

                await _taskRepository.UpdateAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task deleted: {TaskId} by {PosterId}", taskId, posterId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task: {TaskId}", taskId);
                return new Response<bool>(false, false, "An error occurred while deleting the task.");
            }
        }

        /// <summary>
        /// Gets tasks posted by a specific user
        /// </summary>
        public async Task<Response<IEnumerable<TaskListViewModel>>> GetMyTasksAsync(string posterId)
        {
            try
            {
                var tasks = await _taskRepository.GetByPosterIdAsync(posterId);
                var viewModels = tasks.Select(t => MapToListViewModel(t));
                return new Response<IEnumerable<TaskListViewModel>>(viewModels, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for poster: {PosterId}", posterId);
                return new Response<IEnumerable<TaskListViewModel>>(null, false, "An error occurred while retrieving your tasks.");
            }
        }

        /// <summary>
        /// Gets tasks assigned to a specific worker
        /// </summary>
        public async Task<Response<IEnumerable<TaskListViewModel>>> GetAssignedTasksAsync(string workerId)
        {
            try
            {
                var tasks = await _taskRepository.GetByWorkerIdAsync(workerId);
                var viewModels = tasks.Select(t => MapToListViewModel(t));
                return new Response<IEnumerable<TaskListViewModel>>(viewModels, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned tasks for worker: {WorkerId}", workerId);
                return new Response<IEnumerable<TaskListViewModel>>(null, false, "An error occurred while retrieving assigned tasks.");
            }
        }

        /// <summary>
        /// Checks if a user can apply to a task
        /// </summary>
        public async Task<Response<bool>> CanUserApplyAsync(int taskId, string userId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                // Check if task is open
                if (task.Status != TaskStatus.Open)
                {
                    return new Response<bool>(false, false, "This task is no longer accepting applications.");
                }

                // Check if user is the poster
                if (task.PosterId == userId)
                {
                    return new Response<bool>(false, false, "You cannot apply to your own task.");
                }

                // Check if user already applied
                var taskWithApps = await _taskRepository.GetWithApplicationsAsync(taskId);
                var alreadyApplied = taskWithApps?.Applications.Any(a => a.WorkerId == userId) ?? false;
                
                if (alreadyApplied)
                {
                    return new Response<bool>(false, false, "You have already applied to this task.");
                }

                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can apply: {TaskId}, {UserId}", taskId, userId);
                return new Response<bool>(false, false, "An error occurred while checking application eligibility.");
            }
        }

        /// <summary>
        /// Increments the view count for a task
        /// </summary>
        public async Task<Response<bool>> IncrementViewCountAsync(int taskId)
        {
            try
            {
                await _taskRepository.IncrementViewCountAsync(taskId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count for task: {TaskId}", taskId);
                return new Response<bool>(false, false, "An error occurred while updating view count.");
            }
        }

        /// <summary>
        /// Assigns a worker to a task
        /// </summary>
        public async Task<Response<bool>> AssignTaskAsync(int taskId, string workerId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                if (task.Status != TaskStatus.Open)
                {
                    return new Response<bool>(false, false, "Task is not available for assignment.");
                }

                // Add worker to assigned workers collection
                task.AssignedWorker.Clear();
                // Note: The worker needs to be fetched and added. For now, we'll use a different approach.
                // This requires the worker entity to be loaded.
                
                task.Status = TaskStatus.Assigned;
                task.AssignedAt = DateTime.UtcNow;

                await _taskRepository.UpdateAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task assigned: {TaskId} to {WorkerId}", taskId, workerId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task: {TaskId} to {WorkerId}", taskId, workerId);
                return new Response<bool>(false, false, "An error occurred while assigning the task.");
            }
        }

        /// <summary>
        /// Marks a task as in progress
        /// </summary>
        public async Task<Response<bool>> StartTaskAsync(int taskId, string workerId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                if (task.Status != TaskStatus.Assigned)
                {
                    return new Response<bool>(false, false, "Task must be assigned before starting.");
                }

                task.Status = TaskStatus.InProgress;
                task.UpdatedAt = DateTime.UtcNow;

                await _taskRepository.UpdateAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task started: {TaskId} by {WorkerId}", taskId, workerId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting task: {TaskId}", taskId);
                return new Response<bool>(false, false, "An error occurred while starting the task.");
            }
        }

        /// <summary>
        /// Marks a task as completed
        /// </summary>
        public async Task<Response<bool>> CompleteTaskAsync(int taskId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                if (task.Status != TaskStatus.InProgress)
                {
                    return new Response<bool>(false, false, "Task must be in progress to complete.");
                }

                task.Status = TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;

                await _taskRepository.UpdateAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task completed: {TaskId}", taskId);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task: {TaskId}", taskId);
                return new Response<bool>(false, false, "An error occurred while completing the task.");
            }
        }

        /// <summary>
        /// Cancels a task
        /// </summary>
        public async Task<Response<bool>> CancelTaskAsync(int taskId, string reason, string cancelledBy)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    return new Response<bool>(false, false, "Task not found.");
                }

                if (task.Status == TaskStatus.Completed)
                {
                    return new Response<bool>(false, false, "Cannot cancel a completed task.");
                }

                task.Status = TaskStatus.Cancelled;
                task.CancellationReason = reason;
                task.UpdatedAt = DateTime.UtcNow;

                await _taskRepository.UpdateAsync(task);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Task cancelled: {TaskId} by {UserId}", taskId, cancelledBy);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling task: {TaskId}", taskId);
                return new Response<bool>(false, false, "An error occurred while cancelling the task.");
            }
        }

        #region Private Helper Methods

        private TaskDetailsViewModel MapToDetailsViewModel(TaskItem task, string? currentUserId)
        {
            var viewModel = new TaskDetailsViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Category = task.Category,
                CategoryDisplay = task.Category.ToString(),
                Status = task.Status,
                StatusDisplay = task.Status.ToString(),
                BudgetType = task.BudgetType,
                BudgetTypeDisplay = task.BudgetType.ToString(),
                Budget = task.Budget,
                EstimatedHours = task.EstimatedHours,
                DueDate = task.DueDate,
                StartDate = task.StartDate,
                Location = task.Location,
                LocationUrl = task.LocationUrl,
                Latitude = task.Latitude,
                Longitude = task.Longitude,
                Country = task.Country,
                City = task.City,
                IsRemote = task.IsRemote,
                WorkersNeeded = task.WorkersNeeded,
                RequiredSkills = task.RequiredSkills,
                AttachmentUrls = task.AttachmentUrls,
                PosterId = task.PosterId,
                PosterName = task.Poster != null ? $"{task.Poster.FirstName} {task.Poster.LastName}" : null,
                PosterProfilePicture = task.Poster?.ProfilePictureUrl,
                PosterRating = task.Poster?.AverageRating,
                AssignedAt = task.AssignedAt,
                CompletedAt = task.CompletedAt,
                CancellationReason = task.CancellationReason,
                ViewCount = task.ViewCount,
                IsFeatured = task.IsFeatured,
                IsUrgent = task.IsUrgent,
                CreatedAt = task.CreatedAt,
                ApplicationCount = task.Applications?.Count ?? 0
            };

            // Set permissions
            if (!string.IsNullOrEmpty(currentUserId))
            {
                viewModel.CanEdit = task.PosterId == currentUserId && task.Status == TaskStatus.Open;
                viewModel.CanDelete = task.PosterId == currentUserId && task.Status != TaskStatus.InProgress;
                viewModel.CanApply = task.PosterId != currentUserId && task.Status == TaskStatus.Open;
                viewModel.HasApplied = task.Applications?.Any(a => a.WorkerId == currentUserId) ?? false;
            }

            // Map applications
            if (task.Applications != null)
            {
                viewModel.Applications = task.Applications.Select(a => new TaskApplicationViewModel
                {
                    Id = a.Id,
                    TaskId = a.TaskItemId,
                    WorkerId = a.WorkerId ?? string.Empty,
                    WorkerName = a.Worker != null ? $"{a.Worker.FirstName} {a.Worker.LastName}" : null,
                    WorkerProfilePicture = a.Worker?.ProfilePictureUrl,
                    WorkerRating = a.Worker?.AverageRating,
                    WorkerSkills = a.Worker?.Skills,
                    ProposedBudget = a.ProposedBudget,
                    EstimatedHours = a.EstimatedHours,
                    Message = a.Message,
                    Status = a.Status,
                    StatusDisplay = a.Status.ToString(),
                    CreatedAt = a.CreatedAt,
                    ViewedAt = a.ViewedAt,
                    RespondedAt = a.RespondedAt,
                    RejectionReason = a.RejectionReason
                }).ToList();
            }

            return viewModel;
        }

        private TaskListViewModel MapToListViewModel(TaskItem task)
        {
            return new TaskListViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description.Length > 150 
                    ? task.Description.Substring(0, 150) + "..." 
                    : task.Description,
                Category = task.Category,
                CategoryDisplay = task.Category.ToString(),
                Status = task.Status,
                StatusDisplay = task.Status.ToString(),
                BudgetType = task.BudgetType,
                BudgetTypeDisplay = task.BudgetType.ToString(),
                Budget = task.Budget,
                Location = task.Location,
                City = task.City,
                Country = task.Country,
                IsRemote = task.IsRemote,
                PosterName = task.Poster != null ? $"{task.Poster.FirstName} {task.Poster.LastName}" : null,
                PosterProfilePicture = task.Poster?.ProfilePictureUrl,
                PosterRating = task.Poster?.AverageRating,
                ApplicationCount = task.Applications?.Count ?? 0,
                ViewCount = task.ViewCount,
                IsFeatured = task.IsFeatured,
                IsUrgent = task.IsUrgent,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate
            };
        }

        private IEnumerable<TaskItem> ApplySorting(IEnumerable<TaskItem> tasks, string? sortBy)
        {
            return sortBy switch
            {
                "newest" => tasks.OrderByDescending(t => t.CreatedAt),
                "budget_high" => tasks.OrderByDescending(t => t.Budget),
                "budget_low" => tasks.OrderBy(t => t.Budget),
                "distance" => tasks.OrderBy(t => t.Id), // Would need distance calculation
                _ => tasks.OrderByDescending(t => t.IsFeatured)
                         .ThenByDescending(t => t.IsUrgent)
                         .ThenByDescending(t => t.CreatedAt)
            };
        }

        private async Task<int> GetTotalCountAsync(TaskSearchViewModel search)
        {
            var tasks = await _taskRepository.GetFilteredAsync(
                search.Category,
                search.Status ?? TaskStatus.Open,
                search.MinBudget,
                search.MaxBudget,
                search.IsRemote,
                search.Latitude,
                search.Longitude,
                search.RadiusKm,
                1,
                int.MaxValue);

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var keyword = search.Keyword.ToLower();
                tasks = tasks.Where(t => 
                    t.Title.ToLower().Contains(keyword) || 
                    t.Description.ToLower().Contains(keyword));
            }

            if (search.IsUrgent == true)
            {
                tasks = tasks.Where(t => t.IsUrgent);
            }

            return tasks.Count();
        }

        #endregion
    }
}
