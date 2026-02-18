using LaborBLL.ModelVM;
using LaborBLL.Response;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Interface for task application-related business operations
    /// </summary>
    public interface IApplicationService
    {
        /// <summary>
        /// Creates a new application to a task
        /// </summary>
        /// <param name="model">Application creation view model</param>
        /// <param name="workerId">ID of the worker applying</param>
        /// <returns>ID of the created application</returns>
        Task<Response<int>> CreateApplicationAsync(CreateApplicationViewModel model, string workerId);

        /// <summary>
        /// Gets an application by its ID
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <returns>Application details</returns>
        Task<Response<TaskApplicationViewModel>> GetApplicationByIdAsync(int id);

        /// <summary>
        /// Withdraws an application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <param name="workerId">ID of the worker withdrawing</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> WithdrawApplicationAsync(int applicationId, string workerId);

        /// <summary>
        /// Gets all applications for a specific task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="posterId">ID of the task poster (for authorization)</param>
        /// <returns>List of applications</returns>
        Task<Response<IEnumerable<TaskApplicationViewModel>>> GetApplicationsByTaskAsync(int taskId, string posterId);

        /// <summary>
        /// Gets all applications submitted by a worker
        /// </summary>
        /// <param name="workerId">Worker's user ID</param>
        /// <returns>List of applications</returns>
        Task<Response<IEnumerable<TaskApplicationViewModel>>> GetApplicationsByWorkerAsync(string workerId);

        /// <summary>
        /// Accepts an application (creates a booking)
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <param name="posterId">ID of the task poster</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> AcceptApplicationAsync(int applicationId, string posterId);

        /// <summary>
        /// Rejects an application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <param name="posterId">ID of the task poster</param>
        /// <param name="reason">Optional rejection reason</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> RejectApplicationAsync(int applicationId, string posterId, string? reason = null);

        /// <summary>
        /// Checks if a user has already applied to a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user has applied</returns>
        Task<Response<bool>> HasUserAppliedAsync(int taskId, string userId);

        /// <summary>
        /// Gets the count of applications for a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Application count</returns>
        Task<Response<int>> GetApplicationCountAsync(int taskId);

        /// <summary>
        /// Marks an application as viewed by the poster
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <param name="posterId">ID of the poster viewing</param>
        /// <returns>Success indicator</returns>
        Task<Response<bool>> MarkAsViewedAsync(int applicationId, string posterId);
    }
}
