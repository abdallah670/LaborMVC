using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for Notification entity
    /// </summary>
    public class NotificationRepo : Repository<Notification>, INotificationRepo
    {
        public NotificationRepo(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int batchSize = 50)
        {
            return await _context.Notifications
                .Where(n => n.Status == NotificationStatus.Pending && 
                           (n.ScheduledAt == null || n.ScheduledAt <= DateTime.UtcNow))
                .OrderBy(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => n.Status == NotificationStatus.Sent && n.ReadAt == null);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && 
                                n.Status == NotificationStatus.Sent && 
                                n.ReadAt == null);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            notification.ReadAt = DateTime.UtcNow;
            notification.Status = NotificationStatus.Read;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && 
                           n.Status == NotificationStatus.Sent && 
                           n.ReadAt == null)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.ReadAt = DateTime.UtcNow;
                notification.Status = NotificationStatus.Read;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Notification>> GetByTypeAndStatusAsync(NotificationType type, NotificationStatus status, int batchSize = 50)
        {
            return await _context.Notifications
                .Where(n => n.Type == type && n.Status == status)
                .OrderBy(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }
    }
}
