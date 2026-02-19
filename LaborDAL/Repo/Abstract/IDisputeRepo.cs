using LaborDAL.Entities;
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository interface for Dispute entities
    /// </summary>
    public interface IDisputeRepo : IRepository<Dispute>
    {
        /// <summary>
        /// Gets a dispute by booking ID
        /// </summary>
        Task<Dispute?> GetByBookingIdAsync(int bookingId);

        /// <summary>
        /// Gets all disputes with a specific status
        /// </summary>
        Task<IEnumerable<Dispute>> GetDisputesByStatusAsync(DisputeStatus status);

        /// <summary>
        /// Gets all disputes raised by a specific user
        /// </summary>
        Task<IEnumerable<Dispute>> GetDisputesByUserAsync(string userId);

        /// <summary>
        /// Gets all disputes with related entities (Booking, Users)
        /// </summary>
        Task<IEnumerable<Dispute>> GetAllWithIncludesAsync();

        /// <summary>
        /// Gets a dispute by ID with all related entities
        /// </summary>
        Task<Dispute?> GetByIdWithIncludesAsync(int id);

        /// <summary>
        /// Checks if a dispute exists for a booking
        /// </summary>
        Task<bool> ExistsForBookingAsync(int bookingId);

        /// <summary>
        /// Gets count of disputes by status
        /// </summary>
        Task<int> GetCountByStatusAsync(DisputeStatus status);
    }
}
