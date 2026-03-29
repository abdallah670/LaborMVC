namespace LaborBLL.Service.Implementation
{
    using LaborBLL.Service.Abstract;
    using LaborDAL.Common;
    using LaborDAL.DB;
    using LaborDAL.Entities;
    using LaborDAL.Enums;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implementation of penalty service for managing user penalties
    /// </summary>
    public class PenaltyService : IPenaltyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IClock _clock;
        private readonly ILogger<PenaltyService> _logger;

        // Configuration constants
        private const int SuspensionThreshold = 3; // 3 strikes = suspension
        private const int RestrictionThreshold = 2; // 2 no-shows = restriction
        private static readonly TimeSpan StrikeExpiration = TimeSpan.FromDays(90); // Strikes expire after 90 days
        private static readonly TimeSpan DefaultSuspensionDuration = TimeSpan.FromDays(7); // 1 week suspension
        private static readonly TimeSpan DefaultRestrictionDuration = TimeSpan.FromDays(30); // 30 days restriction

        public PenaltyService(
            ApplicationDbContext context,
            IClock clock,
            ILogger<PenaltyService> logger)
        {
            _context = context;
            _clock = clock;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> ApplyWorkerPenaltyAsync(string workerId, int taskId, PenaltyTier tier, string reason)
        {
            _logger.LogInformation(
                "Applying worker penalty - WorkerId: {WorkerId}, TaskId: {TaskId}, Tier: {Tier}",
                workerId, taskId, tier);

            switch (tier)
            {
                case PenaltyTier.Minor:
                    // Just a warning/note
                    return await CreatePenaltyAsync(workerId, taskId, PenaltyType.Warning, tier, reason);

                case PenaltyTier.Moderate:
                    // Strike + rating decrease
                    await AddStrikeAsync(workerId, reason, taskId);
                    await DecreaseRatingAsync(workerId, 0.5m, reason, taskId);
                    return await CreatePenaltyAsync(workerId, taskId, PenaltyType.RatingDecrease, tier, reason);

                case PenaltyTier.Severe:
                    // Strike + significant rating decrease + suspension consideration
                    await AddStrikeAsync(workerId, reason, taskId);
                    await DecreaseRatingAsync(workerId, 1.0m, reason, taskId);
                    await RestrictAcceptanceAsync(workerId, DefaultRestrictionDuration, reason);

                    if (await CheckSuspensionThresholdAsync(workerId))
                    {
                        await SuspendUserAsync(workerId, DefaultSuspensionDuration, reason, taskId);
                    }
                    return await CreatePenaltyAsync(workerId, taskId, PenaltyType.Suspension, tier, reason);

                default:
                    return 0;
            }
        }

        /// <inheritdoc />
        public async Task<int> ApplyClientPenaltyAsync(string clientId, int taskId, PenaltyTier tier, string reason)
        {
            _logger.LogInformation(
                "Applying client penalty - ClientId: {ClientId}, TaskId: {TaskId}, Tier: {Tier}",
                clientId, taskId, tier);

            switch (tier)
            {
                case PenaltyTier.Minor:
                    // Just a warning/note
                    return await CreatePenaltyAsync(clientId, taskId, PenaltyType.Warning, tier, reason);

                case PenaltyTier.Moderate:
                    // Flag + restriction consideration
                    var user = await _context.Users.FindAsync(clientId);
                    if (user != null)
                    {
                        user.NoShowCount++;
                        user.HasUnacknowledgedPenalties = true;
                        await _context.SaveChangesAsync();
                    }

                    if (user?.NoShowCount >= RestrictionThreshold)
                    {
                        await RestrictPostingAsync(clientId, DefaultRestrictionDuration, reason);
                    }
                    return await CreatePenaltyAsync(clientId, taskId, PenaltyType.Warning, tier, reason);

                case PenaltyTier.Severe:
                    // Immediate posting restriction
                    await RestrictPostingAsync(clientId, DefaultRestrictionDuration, reason);
                    return await CreatePenaltyAsync(clientId, taskId, PenaltyType.PostingRestriction, tier, reason);

                default:
                    return 0;
            }
        }

        /// <inheritdoc />
        public async Task<bool> AddStrikeAsync(string userId, string reason, int taskId)
        {
            _logger.LogInformation("Adding strike - UserId: {UserId}, TaskId: {TaskId}", userId, taskId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            // Create penalty record
            var penalty = new UserPenalty
            {
                UserId = userId,
                Type = PenaltyType.Strike,
                Severity = PenaltyTier.Moderate,
                Reason = reason,
                AppliedAt = now,
                ExpiresAt = now.Add(StrikeExpiration),
                IsActive = true,
                RelatedTaskId = taskId
            };

            _context.UserPenalties.Add(penalty);

            // Update user record
            user.StrikeCount++;
            user.LastStrikeDate = now;
            user.HasUnacknowledgedPenalties = true;
            user.ActivePenaltyCount = await GetActivePenaltyCountAsync(userId);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Strike added - UserId: {UserId}, New Strike Count: {Count}",
                userId, user.StrikeCount);

            return true;
        }

        /// <inheritdoc />
        public async Task<decimal> DecreaseRatingAsync(string userId, decimal amount, string reason, int taskId)
        {
            _logger.LogInformation(
                "Decreasing rating - UserId: {UserId}, Amount: {Amount}",
                userId, amount);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return 0;

            var previousRating = user.AverageRating ?? 5.0m;
            var newRating = Math.Max(1.0m, previousRating - amount); // Don't go below 1.0

            var now = _clock.UtcNow.DateTime;

            // Create penalty record
            var penalty = new UserPenalty
            {
                UserId = userId,
                Type = PenaltyType.RatingDecrease,
                Severity = PenaltyTier.Moderate,
                Reason = reason,
                AppliedAt = now,
                IsActive = true,
                RelatedTaskId = taskId,
                RatingDecreaseAmount = amount,
                PreviousRating = previousRating,
                NewRating = newRating
            };

            _context.UserPenalties.Add(penalty);

            // Update user record
            user.AverageRating = newRating;
            user.HasUnacknowledgedPenalties = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Rating decreased - UserId: {UserId}, Previous: {Prev}, New: {New}",
                userId, previousRating, newRating);

            return newRating;
        }

        /// <inheritdoc />
        public async Task<bool> SuspendUserAsync(string userId, TimeSpan duration, string reason, int taskId)
        {
            _logger.LogInformation(
                "Suspending user - UserId: {UserId}, Duration: {Duration}",
                userId, duration);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            // Create penalty record
            var penalty = new UserPenalty
            {
                UserId = userId,
                Type = PenaltyType.Suspension,
                Severity = PenaltyTier.Severe,
                Reason = reason,
                AppliedAt = now,
                ExpiresAt = now.Add(duration),
                IsActive = true,
                RelatedTaskId = taskId
            };

            _context.UserPenalties.Add(penalty);

            // Update user record
            user.IsSuspended = true;
            user.SuspensionEndDate = now.Add(duration);
            user.HasUnacknowledgedPenalties = true;
            user.ActivePenaltyCount = await GetActivePenaltyCountAsync(userId);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User suspended - UserId: {UserId}, Until: {Until}",
                userId, user.SuspensionEndDate);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RestrictPostingAsync(string userId, TimeSpan? duration, string reason)
        {
            _logger.LogInformation("Restricting posting - UserId: {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            // Create penalty record
            var penalty = new UserPenalty
            {
                UserId = userId,
                Type = PenaltyType.PostingRestriction,
                Severity = PenaltyTier.Moderate,
                Reason = reason,
                AppliedAt = now,
                ExpiresAt = duration.HasValue ? now.Add(duration.Value) : null,
                IsActive = true
            };

            _context.UserPenalties.Add(penalty);

            // Update user record
            user.IsPostingRestricted = true;
            user.RestrictionReason = reason;
            user.RestrictionEndDate = duration.HasValue ? now.Add(duration.Value) : null;
            user.HasUnacknowledgedPenalties = true;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RestrictAcceptanceAsync(string userId, TimeSpan? duration, string reason)
        {
            _logger.LogInformation("Restricting acceptance - UserId: {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            // Create penalty record
            var penalty = new UserPenalty
            {
                UserId = userId,
                Type = PenaltyType.AcceptanceRestriction,
                Severity = PenaltyTier.Moderate,
                Reason = reason,
                AppliedAt = now,
                ExpiresAt = duration.HasValue ? now.Add(duration.Value) : null,
                IsActive = true
            };

            _context.UserPenalties.Add(penalty);

            // Update user record
            user.IsAcceptanceRestricted = true;
            user.RestrictionReason = reason;
            user.RestrictionEndDate = duration.HasValue ? now.Add(duration.Value) : null;
            user.HasUnacknowledgedPenalties = true;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> CheckSuspensionThresholdAsync(string userId)
        {
            var activeStrikes = await _context.UserPenalties
                .CountAsync(p => p.UserId == userId &&
                                 p.Type == PenaltyType.Strike &&
                                 p.IsActive);

            return activeStrikes >= SuspensionThreshold;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PenaltyInfo>> GetActivePenaltiesAsync(string userId)
        {
            return await _context.UserPenalties
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.AppliedAt)
                .Select(p => new PenaltyInfo
                {
                    Id = p.Id,
                    Type = p.Type,
                    Severity = p.Severity,
                    Reason = p.Reason,
                    AppliedAt = p.AppliedAt,
                    ExpiresAt = p.ExpiresAt,
                    IsActive = p.IsActive,
                    RelatedTaskId = p.RelatedTaskId,
                    RatingDecreaseAmount = p.RatingDecreaseAmount,
                    IsAcknowledged = p.IsAcknowledged
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<PenaltyStats> GetPenaltyStatsAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Penalties)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new PenaltyStats();
            }

            var activeStrikes = await _context.UserPenalties
                .CountAsync(p => p.UserId == userId &&
                                 p.Type == PenaltyType.Strike &&
                                 p.IsActive);

            var unacknowledged = await _context.UserPenalties
                .CountAsync(p => p.UserId == userId &&
                                 !p.IsAcknowledged &&
                                 p.IsActive);

            var ratingImpact = await _context.UserPenalties
                .Where(p => p.UserId == userId &&
                           p.Type == PenaltyType.RatingDecrease &&
                           p.IsActive)
                .SumAsync(p => p.RatingDecreaseAmount ?? 0);

            var now = _clock.UtcNow.DateTime;

            return new PenaltyStats
            {
                TotalStrikes = user.StrikeCount,
                ActiveStrikes = activeStrikes,
                NoShowCount = user.NoShowCount,
                CancellationCount = user.CancellationCount,
                RecentCancellations = user.RecentCancellationCount,
                IsSuspended = user.IsSuspended && (user.SuspensionEndDate == null || user.SuspensionEndDate > now),
                SuspensionEndDate = user.SuspensionEndDate,
                IsPostingRestricted = user.IsPostingRestricted && (user.RestrictionEndDate == null || user.RestrictionEndDate > now),
                IsAcceptanceRestricted = user.IsAcceptanceRestricted && (user.RestrictionEndDate == null || user.RestrictionEndDate > now),
                UnacknowledgedPenalties = unacknowledged,
                CurrentRating = user.AverageRating ?? 5.0m,
                RatingImpact = ratingImpact
            };
        }

        /// <inheritdoc />
        public async Task<bool> AcknowledgePenaltyAsync(int penaltyId)
        {
            var penalty = await _context.UserPenalties.FindAsync(penaltyId);
            if (penalty == null) return false;

            var now = _clock.UtcNow.DateTime;

            penalty.IsAcknowledged = true;
            penalty.AcknowledgedAt = now;

            // Check if all penalties are acknowledged
            var user = await _context.Users.FindAsync(penalty.UserId);
            if (user != null)
            {
                var hasUnacknowledged = await _context.UserPenalties
                    .AnyAsync(p => p.UserId == user.Id &&
                                   !p.IsAcknowledged &&
                                   p.IsActive);
                user.HasUnacknowledgedPenalties = hasUnacknowledged;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<int> ExpireOldPenaltiesAsync()
        {
            var now = _clock.UtcNow.DateTime;

            var expiredPenalties = await _context.UserPenalties
                .Where(p => p.IsActive &&
                           p.ExpiresAt.HasValue &&
                           p.ExpiresAt < now)
                .ToListAsync();

            foreach (var penalty in expiredPenalties)
            {
                penalty.IsActive = false;
            }

            // Update user active penalty counts
            var affectedUsers = expiredPenalties.Select(p => p.UserId).Distinct();
            foreach (var userId in affectedUsers)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ActivePenaltyCount = await GetActivePenaltyCountAsync(userId);

                    // Clear restrictions if expired
                    if (user.RestrictionEndDate.HasValue && user.RestrictionEndDate < now)
                    {
                        user.IsPostingRestricted = false;
                        user.IsAcceptanceRestricted = false;
                        user.RestrictionEndDate = null;
                    }

                    // Clear suspension if expired
                    if (user.SuspensionEndDate.HasValue && user.SuspensionEndDate < now)
                    {
                        user.IsSuspended = false;
                        user.SuspensionEndDate = null;
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Expired {Count} penalties", expiredPenalties.Count);
            return expiredPenalties.Count;
        }

        /// <inheritdoc />
        public async Task<bool> IsPostingRestrictedAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            return user.IsPostingRestricted &&
                   (user.RestrictionEndDate == null || user.RestrictionEndDate > now);
        }

        /// <inheritdoc />
        public async Task<bool> IsAcceptanceRestrictedAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            return user.IsAcceptanceRestricted &&
                   (user.RestrictionEndDate == null || user.RestrictionEndDate > now);
        }

        /// <inheritdoc />
        public async Task<bool> IsSuspendedAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var now = _clock.UtcNow.DateTime;

            return user.IsSuspended &&
                   (user.SuspensionEndDate == null || user.SuspensionEndDate > now);
        }

        #region Private Helper Methods

        private async Task<int> CreatePenaltyAsync(string userId, int taskId, PenaltyType type, PenaltyTier tier, string reason)
        {
            var now = _clock.UtcNow.DateTime;

            var penalty = new UserPenalty
            {
                UserId = userId,
                Type = type,
                Severity = tier,
                Reason = reason,
                AppliedAt = now,
                IsActive = true,
                RelatedTaskId = taskId
            };

            _context.UserPenalties.Add(penalty);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ActivePenaltyCount = await GetActivePenaltyCountAsync(userId) + 1;
                user.HasUnacknowledgedPenalties = true;
            }

            await _context.SaveChangesAsync();
            return penalty.Id;
        }

        private async Task<int> GetActivePenaltyCountAsync(string userId)
        {
            return await _context.UserPenalties
                .CountAsync(p => p.UserId == userId && p.IsActive);
        }

        #endregion
    }
}
