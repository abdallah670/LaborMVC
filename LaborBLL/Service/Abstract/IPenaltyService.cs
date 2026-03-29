namespace LaborBLL.Service.Abstract
{
    using LaborDAL.Enums;

    /// <summary>
    /// Service for applying and managing user penalties (strikes, rating decreases, suspensions)
    /// </summary>
    public interface IPenaltyService
    {
        /// <summary>
        /// Applies a penalty to a worker
        /// </summary>
        /// <param name="workerId">ID of the worker to penalize</param>
        /// <param name="taskId">ID of the related task</param>
        /// <param name="tier">Severity tier of the penalty</param>
        /// <param name="reason">Human-readable reason for the penalty</param>
        /// <returns>The created penalty record ID</returns>
        Task<int> ApplyWorkerPenaltyAsync(string workerId, int taskId, PenaltyTier tier, string reason);

        /// <summary>
        /// Applies a penalty to a client
        /// </summary>
        /// <param name="clientId">ID of the client to penalize</param>
        /// <param name="taskId">ID of the related task</param>
        /// <param name="tier">Severity tier of the penalty</param>
        /// <param name="reason">Human-readable reason for the penalty</param>
        /// <returns>The created penalty record ID</returns>
        Task<int> ApplyClientPenaltyAsync(string clientId, int taskId, PenaltyTier tier, string reason);

        /// <summary>
        /// Adds a strike to the user's account
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Reason for the strike</param>
        /// <param name="taskId">Related task ID</param>
        /// <returns>True if successful</returns>
        Task<bool> AddStrikeAsync(string userId, string reason, int taskId);

        /// <summary>
        /// Decreases the user's rating
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="amount">Amount to decrease (0.0 to 5.0)</param>
        /// <param name="reason">Reason for the decrease</param>
        /// <param name="taskId">Related task ID</param>
        /// <returns>The new rating value</returns>
        Task<decimal> DecreaseRatingAsync(string userId, decimal amount, string reason, int taskId);

        /// <summary>
        /// Suspends a user account temporarily
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="duration">Duration of suspension</param>
        /// <param name="reason">Reason for suspension</param>
        /// <param name="taskId">Related task ID</param>
        /// <returns>True if successful</returns>
        Task<bool> SuspendUserAsync(string userId, TimeSpan duration, string reason, int taskId);

        /// <summary>
        /// Restricts a user from posting tasks
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="duration">Duration of restriction (null for permanent)</param>
        /// <param name="reason">Reason for restriction</param>
        /// <returns>True if successful</returns>
        Task<bool> RestrictPostingAsync(string userId, TimeSpan? duration, string reason);

        /// <summary>
        /// Restricts a user from accepting tasks
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="duration">Duration of restriction (null for permanent)</param>
        /// <param name="reason">Reason for restriction</param>
        /// <returns>True if successful</returns>
        Task<bool> RestrictAcceptanceAsync(string userId, TimeSpan? duration, string reason);

        /// <summary>
        /// Checks if user has reached suspension threshold (3+ strikes)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if user should be suspended</returns>
        Task<bool> CheckSuspensionThresholdAsync(string userId);

        /// <summary>
        /// Gets all active penalties for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of active penalties</returns>
        Task<IEnumerable<PenaltyInfo>> GetActivePenaltiesAsync(string userId);

        /// <summary>
        /// Gets penalty statistics for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Penalty statistics</returns>
        Task<PenaltyStats> GetPenaltyStatsAsync(string userId);

        /// <summary>
        /// Acknowledges a penalty (marks as viewed by user)
        /// </summary>
        /// <param name="penaltyId">Penalty ID</param>
        /// <returns>True if successful</returns>
        Task<bool> AcknowledgePenaltyAsync(int penaltyId);

        /// <summary>
        /// Expires old penalties that have passed their expiration date
        /// </summary>
        /// <returns>Number of penalties expired</returns>
        Task<int> ExpireOldPenaltiesAsync();

        /// <summary>
        /// Checks if user is currently restricted from posting tasks
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if restricted</returns>
        Task<bool> IsPostingRestrictedAsync(string userId);

        /// <summary>
        /// Checks if user is currently restricted from accepting tasks
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if restricted</returns>
        Task<bool> IsAcceptanceRestrictedAsync(string userId);

        /// <summary>
        /// Checks if user account is suspended
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if suspended</returns>
        Task<bool> IsSuspendedAsync(string userId);
    }

    /// <summary>
    /// Information about a penalty
    /// </summary>
    public class PenaltyInfo
    {
        public int Id { get; set; }
        public PenaltyType Type { get; set; }
        public PenaltyTier Severity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public int? RelatedTaskId { get; set; }
        public decimal? RatingDecreaseAmount { get; set; }
        public bool IsAcknowledged { get; set; }
    }

    /// <summary>
    /// Penalty statistics for a user
    /// </summary>
    public class PenaltyStats
    {
        public int TotalStrikes { get; set; }
        public int ActiveStrikes { get; set; }
        public int NoShowCount { get; set; }
        public int CancellationCount { get; set; }
        public int RecentCancellations { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime? SuspensionEndDate { get; set; }
        public bool IsPostingRestricted { get; set; }
        public bool IsAcceptanceRestricted { get; set; }
        public int UnacknowledgedPenalties { get; set; }
        public decimal CurrentRating { get; set; }
        public decimal RatingImpact { get; set; }
    }
}
