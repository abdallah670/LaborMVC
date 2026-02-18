using LaborBLL.ModelVM;
using LaborBLL.Response;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Interface for task-related business operations
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Gets a task by its ID with full details
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <param name="currentUserId">Current user ID for permission checks</param>
        /// <returns>Task details view model</returns>
        Task<Response<TaskDetailsViewModel>> GetTaskByIdAsync(int id, string? currentUserId = null);

        /// <summary>
        /// Gets a paginated list of tasks based on search criteria
        /// </summary>
        /// <param name="search">Search parameters</param>
        /// <param name="currentUserId">Current user ID for permission checks</param>
        /// <returns>Search results with pagination</returns>
        Task<Response<TaskSearchViewModel>> GetTaskListAsync(TaskSearchViewModel search, string? currentUserId = null);

        /// <summary>
        /// Creates a new task
        /// </summary>
        /// <param name="model">Task creation view model</param>
        /// <param name="posterId">ID of the user posting the task</param>
        /// <returns>ID of the created task</returns>
        Task<Response<int>> CreateTaskAsync(CreateTaskViewModel model, string posterId);

        /// <summary>
        /// Updates an existing task
        /// </summary>
        /// <param name="model">Task edit view model</param>
        /// <param name="posterId">ID of the user updating the task</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> UpdateTaskAsync(EditTaskViewModel model, string posterId);

        /// <summary>
        /// Deletes a task (soft delete)
        /// </summary>
        /// <param name="taskId">Task ID to delete</param>
        /// <param name="posterId">ID of the user deleting the task</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> DeleteTaskAsync(int taskId, string posterId);

        /// <summary>
        /// Gets tasks posted by a specific user
        /// </summary>
        /// <param name="posterId">Poster's user ID</param>
        /// <returns>List of tasks</returns>
        Task<Response<IEnumerable<TaskListViewModel>>> GetMyTasksAsync(string posterId);

        /// <summary>
        /// Gets tasks assigned to a specific worker
        /// </summary>
        /// <param name="workerId">Worker's user ID</param>
        /// <returns>List of tasks</returns>
        Task<Response<IEnumerable<TaskListViewModel>>> GetAssignedTasksAsync(string workerId);

        /// <summary>
        /// Checks if a user can apply to a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user can apply</returns>
        Task<Response<bool>> CanUserApplyAsync(int taskId, string userId);

        /// <summary>
        /// Increments the view count for a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> IncrementViewCountAsync(int taskId);

        /// <summary>
        /// Assigns a worker to a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="workerId">Worker's user ID</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> AssignTaskAsync(int taskId, string workerId);

        /// <summary>
        /// Marks a task as in progress
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="workerId">Worker's user ID</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> StartTaskAsync(int taskId, string workerId);

        /// <summary>
        /// Marks a task as completed
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> CompleteTaskAsync(int taskId);

        /// <summary>
        /// Cancels a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="reason">Cancellation reason</param>
        /// <param name="cancelledBy">User ID who cancelled</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> CancelTaskAsync(int taskId, string reason, string cancelledBy);
    }
}
