using LaborDAL.Enums;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service to enforce verification tier limits for users
    /// M6: Verification Tiers - Enforce $100 limit for unverified users
    /// </summary>
    public interface IVerificationLimitService
    {
        /// <summary>
        /// Maximum task budget for unverified users (M6 requirement)
        /// </summary>
        const decimal UnverifiedMaxBudget = 100.00m;

        /// <summary>
        /// Check if a user can create a task with the specified budget
        /// </summary>
        Task<(bool allowed, string? reason)> CanCreateTaskAsync(string userId, decimal budget);

        /// <summary>
        /// Check if a user can apply for a task with the specified budget
        /// </summary>
        Task<(bool allowed, string? reason)> CanApplyForTaskAsync(string userId, decimal taskBudget);

        /// <summary>
        /// Validate task budget against user's verification tier
        /// </summary>
        bool ValidateTaskBudget(decimal budget, VerificationTier tier);

        /// <summary>
        /// Get the maximum allowed budget for a verification tier
        /// </summary>
        decimal GetMaxBudgetForTier(VerificationTier tier);

        /// <summary>
        /// Get remaining budget capacity for user in current period
        /// </summary>
        Task<decimal> GetRemainingBudgetAsync(string userId);
    }
}
