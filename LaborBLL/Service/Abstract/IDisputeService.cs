using LaborBLL.ModelVM;
using LaborDAL.Enums;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service interface for dispute management
    /// </summary>
    public interface IDisputeService
    {
        #region User Actions

        /// <summary>
        /// Raises a new dispute for a booking
        /// </summary>
        /// <param name="model">The dispute creation model</param>
        /// <param name="userId">The ID of the user raising the dispute</param>
        /// <returns>Response with the created dispute details</returns>
        Task<Response<DisputeDetailsViewModel>> RaiseDisputeAsync(CreateDisputeViewModel model, string userId);

        /// <summary>
        /// Gets dispute details by ID
        /// </summary>
        /// <param name="disputeId">The dispute ID</param>
        /// <returns>Dispute details or null if not found</returns>
        Task<DisputeDetailsViewModel?> GetDisputeDetailsAsync(int disputeId);

        /// <summary>
        /// Gets all disputes raised by a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of disputes</returns>
        Task<IEnumerable<DisputeListViewModel>> GetUserDisputesAsync(string userId);

        /// <summary>
        /// Checks if a user can raise a dispute for a booking
        /// </summary>
        /// <param name="bookingId">The booking ID</param>
        /// <param name="userId">The user ID</param>
        /// <returns>True if dispute can be raised</returns>
        Task<bool> CanRaiseDisputeAsync(int bookingId, string userId);

        #endregion

        #region Admin Actions

        /// <summary>
        /// Gets all disputes, optionally filtered by status
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of disputes</returns>
        Task<IEnumerable<DisputeListViewModel>> GetAllDisputesAsync(DisputeStatus? status = null);

        /// <summary>
        /// Updates the status of a dispute
        /// </summary>
        /// <param name="disputeId">The dispute ID</param>
        /// <param name="status">The new status</param>
        /// <returns>Response indicating success or failure</returns>
        Task<Response<bool>> UpdateStatusAsync(int disputeId, DisputeStatus status);

        /// <summary>
        /// Resolves a dispute
        /// </summary>
        /// <param name="model">The resolution model</param>
        /// <param name="adminId">The ID of the admin resolving the dispute</param>
        /// <returns>Response indicating success or failure</returns>
        Task<Response<bool>> ResolveDisputeAsync(ResolveDisputeViewModel model, string adminId);

        /// <summary>
        /// Gets the count of open disputes
        /// </summary>
        /// <returns>Number of open disputes</returns>
        Task<int> GetOpenDisputeCountAsync();

        /// <summary>
        /// Gets dispute statistics for dashboard
        /// </summary>
        /// <returns>Dictionary with status counts</returns>
        Task<Dictionary<string, int>> GetDisputeStatsAsync();

        #endregion
    }
}
