using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Implementation of distributed caching service using Redis or in-memory fallback
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Get cached item by key
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var cached = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cached))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache key: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Set cached item with optional expiration
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
                };

                var serialized = JsonSerializer.Serialize(value, _jsonOptions);
                await _cache.SetStringAsync(key, serialized, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        /// <summary>
        /// Remove cached item by key
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var cached = await _cache.GetStringAsync(key);
                return !string.IsNullOrEmpty(cached);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Get or create cache entry using cache-aside pattern
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // Try to get from cache first
            var cached = await GetAsync<T>(key);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cached;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);

            // Call factory to get data
            var value = await factory();

            // Store in cache if not null
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }
    }

    /// <summary>
    /// Cache key prefixes for different entity types
    /// </summary>
    public static class CacheKeys
    {
        public const string Task = "task:";
        public const string TaskListPrefix = "tasks:list:";
        public const string User = "user:";
        public const string Booking = "booking:";
        public const string Payment = "payment:";
        public const string Category = "category:";
        public const string FeaturedTasks = "tasks:featured";
        public const string UrgentTasks = "tasks:urgent";
        public const string SearchResults = "search:";
        public const string DashboardStatsPrefix = "dashboard:stats:";
        public const string WorkerProfile = "worker:profile:";
        public const string ClientProfile = "client:profile:";

        /// <summary>
        /// Generate cache key for paginated task list
        /// </summary>
        public static string TaskList(int page, int pageSize, string? category = null, string? keyword = null)
        {
            var key = $"{TaskListPrefix}p{page}:s{pageSize}";
            if (!string.IsNullOrEmpty(category))
                key += $":c{category}";
            if (!string.IsNullOrEmpty(keyword))
                key += $":k{keyword.GetHashCode()}";
            return key;
        }

        /// <summary>
        /// Generate cache key for task details
        /// </summary>
        public static string TaskDetails(int taskId) => $"{Task}{taskId}";

        /// <summary>
        /// Generate cache key for user
        /// </summary>
        public static string UserDetails(string userId) => $"{User}{userId}";

        /// <summary>
        /// Generate cache key for booking
        /// </summary>
        public static string BookingDetails(int bookingId) => $"{Booking}{bookingId}";

        /// <summary>
        /// Generate cache key for payment
        /// </summary>
        public static string PaymentDetails(string paymentId) => $"{Payment}{paymentId}";

        /// <summary>
        /// Generate cache key for category list
        /// </summary>
        public static string CategoryList() => $"{Category}list";

        /// <summary>
        /// Generate cache key for featured tasks
        /// </summary>
        public static string FeaturedTaskList(int limit) => $"{FeaturedTasks}:limit{limit}";

        /// <summary>
        /// Generate cache key for urgent tasks
        /// </summary>
        public static string UrgentTaskList(int limit) => $"{UrgentTasks}:limit{limit}";

        /// <summary>
        /// Generate cache key for search results
        /// </summary>
        public static string Search(string keyword, int page, int pageSize) => 
            $"{SearchResults}{keyword.GetHashCode()}:p{page}:s{pageSize}";

        /// <summary>
        /// Generate cache key for dashboard statistics
        /// </summary>
        public static string DashboardStats(string userId) => $"{DashboardStatsPrefix}{userId}";

        /// <summary>
        /// Generate cache key for worker profile
        /// </summary>
        public static string WorkerProfileDetails(string workerId) => $"{WorkerProfile}{workerId}";

        /// <summary>
        /// Generate cache key for client profile
        /// </summary>
        public static string ClientProfileDetails(string clientId) => $"{ClientProfile}{clientId}";

        /// <summary>
        /// Generate cache key pattern for invalidation by prefix
        /// </summary>
        public static string ByPrefix(string prefix) => $"{prefix}*";
    }
}
