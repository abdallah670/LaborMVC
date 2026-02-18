using LaborDAL.Entities;
using LaborDAL.Enums;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository interface for TaskItem with specialized query methods
    /// </summary>
    public interface ITaskRepository : IRepository<TaskItem>
    {
        /// <summary>
        /// Get tasks within a radius from a given point
        /// </summary>
        /// <param name="latitude">Center latitude</param>
        /// <param name="longitude">Center longitude</param>
        /// <param name="radiusKm">Radius in kilometers</param>
        /// <returns>Tasks within the specified radius</returns>
        Task<IEnumerable<TaskItem>> GetTasksWithinRadiusAsync(decimal latitude, decimal longitude, double radiusKm);

        /// <summary>
        /// Get tasks by category
        /// </summary>
        /// <param name="category">Task category</param>
        /// <returns>Tasks in the specified category</returns>
        Task<IEnumerable<TaskItem>> GetByCategoryAsync(TaskCategory category);

        /// <summary>
        /// Get tasks by status
        /// </summary>
        /// <param name="status">Task status</param>
        /// <returns>Tasks with the specified status</returns>
        Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskStatus status);

        /// <summary>
        /// Get tasks posted by a specific user
        /// </summary>
        /// <param name="posterId">Poster's user ID</param>
        /// <returns>Tasks posted by the user</returns>
        Task<IEnumerable<TaskItem>> GetByPosterIdAsync(string posterId);

        /// <summary>
        /// Get tasks assigned to a specific worker
        /// </summary>
        /// <param name="workerId">Worker's user ID</param>
        /// <returns>Tasks assigned to the worker</returns>
        Task<IEnumerable<TaskItem>> GetByWorkerIdAsync(string workerId);

        /// <summary>
        /// Search tasks by keyword in title or description
        /// </summary>
        /// <param name="keyword">Search keyword</param>
        /// <returns>Matching tasks</returns>
        Task<IEnumerable<TaskItem>> SearchAsync(string keyword);

        /// <summary>
        /// Get featured tasks
        /// </summary>
        /// <returns>Featured tasks</returns>
        Task<IEnumerable<TaskItem>> GetFeaturedTasksAsync();

        /// <summary>
        /// Get urgent tasks
        /// </summary>
        /// <returns>Urgent tasks</returns>
        Task<IEnumerable<TaskItem>> GetUrgentTasksAsync();

        /// <summary>
        /// Get tasks with filters
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="minBudget">Minimum budget</param>
        /// <param name="maxBudget">Maximum budget</param>
        /// <param name="isRemote">Remote work filter</param>
        /// <param name="latitude">Optional latitude for distance filter</param>
        /// <param name="longitude">Optional longitude for distance filter</param>
        /// <param name="radiusKm">Radius in kilometers</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Filtered tasks</returns>
        Task<IEnumerable<TaskItem>> GetFilteredAsync(
            TaskCategory? category = null,
            TaskStatus? status = null,
            decimal? minBudget = null,
            decimal? maxBudget = null,
            bool? isRemote = null,
            decimal? latitude = null,
            decimal? longitude = null,
            double? radiusKm = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Get task with applications
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Task with applications loaded</returns>
        Task<TaskItem?> GetWithApplicationsAsync(int taskId);

        /// <summary>
        /// Increment view count for a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        Task IncrementViewCountAsync(int taskId);

        /// <summary>
        /// Get tasks count by status
        /// </summary>
        /// <param name="status">Task status</param>
        /// <returns>Count of tasks</returns>
        Task<int> CountByStatusAsync(TaskStatus status);
    }
}
