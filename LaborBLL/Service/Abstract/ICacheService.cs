namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Distributed caching service interface for Redis or in-memory cache
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached item by key
        /// </summary>
        Task<T?> GetAsync<T>(string key);
        
        /// <summary>
        /// Set cached item with optional expiration
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        
        /// <summary>
        /// Remove cached item by key
        /// </summary>
        Task RemoveAsync(string key);
        
        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key);
        
        /// <summary>
        /// Get or create cache entry
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }
}
