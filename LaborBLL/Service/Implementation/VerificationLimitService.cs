using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Implementation of verification tier limit enforcement
    /// M6: Verification Tiers - Enforce $100 limit for unverified users
    /// </summary>
    public class VerificationLimitService : IVerificationLimitService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<VerificationLimitService> _logger;

        // Tier limits configuration - matching actual VerificationTier enum
        private static readonly Dictionary<VerificationTier, decimal> TierLimits = new()
        {
            { VerificationTier.Unverified, 100.00m },
            { VerificationTier.EmailVerified, 500.00m },
            { VerificationTier.PhoneVerified, 2000.00m },
            { VerificationTier.IDVerified, 10000.00m }
        };

        public VerificationLimitService(
            UserManager<AppUser> userManager,
            ITaskRepository taskRepository,
            ILogger<VerificationLimitService> logger)
        {
            _userManager = userManager;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        /// <summary>
        /// Check if a user can create a task with the specified budget
        /// </summary>
        public async Task<(bool allowed, string? reason)> CanCreateTaskAsync(string userId, decimal budget)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when checking task creation permission", userId);
                return (false, "User not found");
            }

            var tier = user.VerificationTier;
            var maxBudget = GetMaxBudgetForTier(tier);

            if (budget > maxBudget)
            {
                _logger.LogInformation(
                    "User {UserId} with tier {Tier} attempted to create task with budget ${Budget} exceeding limit ${Limit}",
                    userId, tier, budget, maxBudget);

                var upgradeMessage = tier == VerificationTier.Unverified
                    ? "Please verify your account to post tasks with higher budgets."
                    : $"Your current tier ({tier}) has a maximum budget of ${maxBudget}. Please upgrade your verification tier.";

                return (false, $"Budget exceeds your verification tier limit of ${maxBudget}. {upgradeMessage}");
            }

            // Check remaining monthly capacity
            var remainingBudget = await GetRemainingBudgetAsync(userId);
            if (budget > remainingBudget)
            {
                _logger.LogInformation(
                    "User {UserId} attempted to create task with budget ${Budget} exceeding remaining capacity ${Remaining}",
                    userId, budget, remainingBudget);

                return (false, $"This task would exceed your monthly posting limit. Remaining capacity: ${remainingBudget:F2}");
            }

            return (true, null);
        }

        /// <summary>
        /// Check if a user can apply for a task with the specified budget
        /// </summary>
        public async Task<(bool allowed, string? reason)> CanApplyForTaskAsync(string userId, decimal taskBudget)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when checking task application permission", userId);
                return (false, "User not found");
            }

            var tier = user.VerificationTier;

            // Workers need at least EmailVerified to apply for high-value tasks
            if (taskBudget > 1000 && tier == VerificationTier.Unverified)
            {
                _logger.LogInformation(
                    "Unverified user {UserId} attempted to apply for high-value task (${Budget})",
                    userId, taskBudget);

                return (false, "You need Email verification or higher to apply for tasks over $1,000. Please verify your account.");
            }

            // Premium tasks require PhoneVerified or higher
            if (taskBudget > 5000 && tier < VerificationTier.PhoneVerified)
            {
                _logger.LogInformation(
                    "User {UserId} with tier {Tier} attempted to apply for premium task (${Budget})",
                    userId, tier, taskBudget);

                return (false, "You need Phone verification or higher to apply for tasks over $5,000.");
            }

            return (true, null);
        }

        /// <summary>
        /// Validate task budget against user's verification tier
        /// </summary>
        public bool ValidateTaskBudget(decimal budget, VerificationTier tier)
        {
            var maxBudget = GetMaxBudgetForTier(tier);
            return budget <= maxBudget;
        }

        /// <summary>
        /// Get the maximum allowed budget for a verification tier
        /// </summary>
        public decimal GetMaxBudgetForTier(VerificationTier tier)
        {
            return TierLimits.TryGetValue(tier, out var limit) ? limit : TierLimits[VerificationTier.Unverified];
        }

        /// <summary>
        /// Get remaining budget capacity for user in current month
        /// </summary>
        public async Task<decimal> GetRemainingBudgetAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return 0;

            var tier = user.VerificationTier;
            var maxBudget = GetMaxBudgetForTier(tier);

            // Calculate total active task value for current month
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var tasks = await _taskRepository.GetFilteredPagedAsync(
                filter: t => t.PosterId == userId &&
                            t.CreatedAt >= startOfMonth &&
                            t.CreatedAt <= endOfMonth &&
                            t.Status != LaborDAL.Enums.TaskStatus.Cancelled,
                page: 1,
                pageSize: 1000,
                orderBy: null,
                ascending: true);

            var usedBudget = tasks.Items.Sum(t => t.Budget);
            var remaining = maxBudget - usedBudget;

            _logger.LogDebug(
                "User {UserId} tier {Tier}: Max ${Max}, Used ${Used}, Remaining ${Remaining}",
                userId, tier, maxBudget, usedBudget, remaining);

            return Math.Max(0, remaining);
        }
    }
}
