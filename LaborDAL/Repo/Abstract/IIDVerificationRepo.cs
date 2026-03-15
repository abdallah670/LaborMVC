using LaborDAL.Entities;
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository for ID verification submissions
    /// </summary>
    public interface IIDVerificationRepo : IRepository<IDVerification>
    {
        /// <summary>
        /// Get verification by user ID
        /// </summary>
        Task<IDVerification?> GetByUserIdAsync(string userId);

        /// <summary>
        /// Get latest verification by user ID
        /// </summary>
        Task<IDVerification?> GetLatestByUserIdAsync(string userId);

        /// <summary>
        /// Check if user has pending verification
        /// </summary>
        Task<bool> HasPendingVerificationAsync(string userId);

        /// <summary>
        /// Get all pending verifications
        /// </summary>
        Task<IEnumerable<IDVerification>> GetPendingVerificationsAsync();

        /// <summary>
        /// Get verifications by status
        /// </summary>
        Task<IEnumerable<IDVerification>> GetByStatusAsync(VerificationStatus status);
    }
}
