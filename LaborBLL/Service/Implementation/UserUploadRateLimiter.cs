using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LaborBLL.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Rate limit check result
    /// </summary>
    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? RetryAfter { get; set; }
        public int RemainingUploads { get; set; }
        public int HourlyUploads { get; set; }
        public int DailyUploads { get; set; }
    }

    /// <summary>
    /// Service for enforcing per-user upload rate limits
    /// </summary>
    public interface IUserUploadRateLimiter
    {
        /// <summary>
        /// Check if a user can upload a file
        /// </summary>
        Task<RateLimitResult> CheckRateLimitAsync(
            string? userId,
            string? ipAddress,
            long fileSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Record a successful upload
        /// </summary>
        Task RecordUploadAsync(
            string? userId,
            string? ipAddress,
            long fileSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset rate limits for a user
        /// </summary>
        Task ResetLimitsAsync(string userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// In-memory implementation of user upload rate limiting
    /// </summary>
    public class UserUploadRateLimiter : IUserUploadRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly UploadRateLimitSettings _settings;
        private readonly ILogger<UserUploadRateLimiter> _logger;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public UserUploadRateLimiter(
            IMemoryCache cache,
            IOptions<UploadRateLimitSettings> settings,
            ILogger<UserUploadRateLimiter> logger)
        {
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<RateLimitResult> CheckRateLimitAsync(
            string? userId,
            string? ipAddress,
            long fileSize,
            CancellationToken cancellationToken = default)
        {
            var key = GetRateLimitKey(userId, ipAddress);
            var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await lockObj.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var stats = GetOrCreateStats(key);

                // Clean up old entries
                CleanupOldEntries(stats, now);

                // Check per-minute limit (burst protection)
                var uploadsLastMinute = stats.Uploads.Count(u => u.Timestamp > now.AddMinutes(-1));
                if (uploadsLastMinute >= _settings.MaxFilesPerMinute)
                {
                    var retryAfter = now.AddMinutes(1);
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        ErrorMessage = $"Rate limit exceeded. Maximum {_settings.MaxFilesPerMinute} uploads per minute allowed.",
                        RetryAfter = retryAfter,
                        HourlyUploads = stats.Uploads.Count(u => u.Timestamp > now.AddHours(-1)),
                        DailyUploads = stats.Uploads.Count(u => u.Timestamp > now.AddDays(-1)),
                        RemainingUploads = 0
                    };
                }

                // Check hourly limit
                var uploadsLastHour = stats.Uploads.Count(u => u.Timestamp > now.AddHours(-1));
                if (uploadsLastHour >= _settings.MaxFilesPerHour)
                {
                    var oldestUpload = stats.Uploads
                        .Where(u => u.Timestamp > now.AddHours(-1))
                        .OrderBy(u => u.Timestamp)
                        .FirstOrDefault();

                    var retryAfter = oldestUpload?.Timestamp.AddHours(1) ?? now.AddHours(1);

                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        ErrorMessage = $"Hourly upload limit exceeded. Maximum {_settings.MaxFilesPerHour} uploads per hour allowed.",
                        RetryAfter = retryAfter,
                        HourlyUploads = uploadsLastHour,
                        DailyUploads = stats.Uploads.Count(u => u.Timestamp > now.AddDays(-1)),
                        RemainingUploads = 0
                    };
                }

                // Check daily limit
                var uploadsLastDay = stats.Uploads.Count(u => u.Timestamp > now.AddDays(-1));
                if (uploadsLastDay >= _settings.MaxFilesPerDay)
                {
                    var oldestUpload = stats.Uploads
                        .Where(u => u.Timestamp > now.AddDays(-1))
                        .OrderBy(u => u.Timestamp)
                        .FirstOrDefault();

                    var retryAfter = oldestUpload?.Timestamp.AddDays(1) ?? now.AddDays(1);

                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        ErrorMessage = $"Daily upload limit exceeded. Maximum {_settings.MaxFilesPerDay} uploads per day allowed.",
                        RetryAfter = retryAfter,
                        HourlyUploads = uploadsLastHour,
                        DailyUploads = uploadsLastDay,
                        RemainingUploads = 0
                    };
                }

                // Check storage quota
                var storageUsedMB = stats.TotalBytesUploaded / (1024.0 * 1024.0);
                var fileSizeMB = fileSize / (1024.0 * 1024.0);
                if (storageUsedMB + fileSizeMB > _settings.MaxStorageMBPerUser)
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        ErrorMessage = $"Storage quota exceeded. Maximum {_settings.MaxStorageMBPerUser}MB per user allowed.",
                        HourlyUploads = uploadsLastHour,
                        DailyUploads = uploadsLastDay,
                        RemainingUploads = Math.Max(0, _settings.MaxFilesPerDay - uploadsLastDay)
                    };
                }

                return new RateLimitResult
                {
                    IsAllowed = true,
                    HourlyUploads = uploadsLastHour,
                    DailyUploads = uploadsLastDay,
                    RemainingUploads = Math.Min(
                        _settings.MaxFilesPerHour - uploadsLastHour,
                        _settings.MaxFilesPerDay - uploadsLastDay)
                };
            }
            finally
            {
                lockObj.Release();
            }
        }

        public async Task RecordUploadAsync(
            string? userId,
            string? ipAddress,
            long fileSize,
            CancellationToken cancellationToken = default)
        {
            var key = GetRateLimitKey(userId, ipAddress);
            var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await lockObj.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var stats = GetOrCreateStats(key);

                stats.Uploads.Add(new UploadEntry
                {
                    Timestamp = now,
                    FileSize = fileSize
                });

                stats.TotalBytesUploaded += fileSize;

                // Update cache with sliding expiration
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromDays(2))
                    .SetAbsoluteExpiration(TimeSpan.FromDays(7));

                _cache.Set(key, stats, cacheOptions);

                _logger.LogDebug(
                    "Recorded upload for user {UserId} from IP {IpAddress}. " +
                    "File size: {FileSize} bytes, Total uploads: {Count}",
                    userId ?? "anonymous", ipAddress, fileSize, stats.Uploads.Count);
            }
            finally
            {
                lockObj.Release();
            }
        }

        public Task ResetLimitsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var key = $"ratelimit:user:{userId}";
            _cache.Remove(key);

            _logger.LogInformation("Reset rate limits for user {UserId}", userId);

            return Task.CompletedTask;
        }

        private string GetRateLimitKey(string? userId, string? ipAddress)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                return $"ratelimit:user:{userId}";
            }

            if (_settings.TrackByIpForAnonymous && !string.IsNullOrEmpty(ipAddress))
            {
                return $"ratelimit:ip:{ipAddress}";
            }

            // Fallback for anonymous users without IP tracking
            return $"ratelimit:anonymous:{Guid.NewGuid()}";
        }

        private UploadStats GetOrCreateStats(string key)
        {
            if (_cache.TryGetValue(key, out UploadStats? stats) && stats != null)
            {
                return stats;
            }

            return new UploadStats();
        }

        private void CleanupOldEntries(UploadStats stats, DateTime now)
        {
            // Remove entries older than 24 hours
            var cutoff = now.AddDays(-1);
            stats.Uploads.RemoveAll(u => u.Timestamp < cutoff);
        }
    }

    /// <summary>
    /// Upload statistics for rate limiting
    /// </summary>
    internal class UploadStats
    {
        public List<UploadEntry> Uploads { get; set; } = new();
        public long TotalBytesUploaded { get; set; }
    }

    /// <summary>
    /// Individual upload entry
    /// </summary>
    internal class UploadEntry
    {
        public DateTime Timestamp { get; set; }
        public long FileSize { get; set; }
    }
}
