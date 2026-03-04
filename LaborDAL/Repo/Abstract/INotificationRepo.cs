using System.Collections.Generic;
using System.Threading.Tasks;
using LaborDAL.Entities;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository interface for Notification entity
    /// </summary>
    public interface INotificationRepo : IRepository<Notification>
    {
        /// <summary>
        /// Get pending notifications that need to be sent
        /// </summary>
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int batchSize = 50);
        
        /// <summary>
        /// Get notifications for a specific user
        /// </summary>
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Get count of unread notifications for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(string userId);
        
        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId);
        
        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task<bool> MarkAllAsReadAsync(string userId);
        
        /// <summary>
        /// Get notifications by type and status
        /// </summary>
        Task<IEnumerable<Notification>> GetByTypeAndStatusAsync(NotificationType type, NotificationStatus status, int batchSize = 50);
    }
}
