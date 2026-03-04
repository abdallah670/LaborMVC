using System.Collections.Generic;
using System.Threading.Tasks;
using LaborDAL.Entities;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service interface for managing notifications
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Create and queue a new notification
        /// </summary>
        Task<Notification> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null,
            string? metadata = null,
            DateTime? scheduledAt = null);

        /// <summary>
        /// Create an email notification
        /// </summary>
        Task<Notification> SendEmailAsync(
            string userId,
            string subject,
            string body,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null);

        /// <summary>
        /// Create an SMS notification
        /// </summary>
        Task<Notification> SendSmsAsync(
            string userId,
            string message,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null);

        /// <summary>
        /// Create an in-app notification
        /// </summary>
        Task<Notification> SendInAppNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null);

        /// <summary>
        /// Get notifications for a user
        /// </summary>
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(
            string userId,
            bool unreadOnly = false,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Get count of unread notifications for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId);

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task<bool> MarkAllAsReadAsync(string userId);

        /// <summary>
        /// Process pending notifications (for background job)
        /// </summary>
        Task ProcessPendingNotificationsAsync(int batchSize = 50);
    }
}
