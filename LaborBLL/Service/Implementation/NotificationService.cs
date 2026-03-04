using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for managing notifications (Email, SMS, In-App)
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ISmsService smsService,
            UserManager<AppUser> userManager,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _smsService = smsService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null,
            string? metadata = null,
            DateTime? scheduledAt = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                Status = NotificationStatus.Pending,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                Metadata = metadata,
                ScheduledAt = scheduledAt,
                RetryCount = 0,
                MaxRetryCount = 3
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Created {Type} notification for user {UserId} with priority {Priority}",
                type, userId, priority);

            return notification;
        }

        public async Task<Notification> SendEmailAsync(
            string userId,
            string subject,
            string body,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null)
        {
            return await CreateNotificationAsync(
                userId, subject, body, NotificationType.Email, priority,
                relatedEntityType, relatedEntityId);
        }

        public async Task<Notification> SendSmsAsync(
            string userId,
            string message,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null)
        {
            return await CreateNotificationAsync(
                userId, "SMS", message, NotificationType.Sms, priority,
                relatedEntityType, relatedEntityId);
        }

        public async Task<Notification> SendInAppNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationPriority priority = NotificationPriority.Normal,
            string? relatedEntityType = null,
            int? relatedEntityId = null)
        {
            // In-app notifications are sent immediately without background processing
            var notification = await CreateNotificationAsync(
                userId, title, message, NotificationType.InApp, priority,
                relatedEntityType, relatedEntityId);

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            await _unitOfWork.SaveAsync();

            return notification;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(
            string userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
        {
            return await _unitOfWork.Notifications.GetUserNotificationsAsync(userId, unreadOnly, page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _unitOfWork.Notifications.GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            return await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            return await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
        }

        public async Task ProcessPendingNotificationsAsync(int batchSize = 50)
        {
            var pendingNotifications = await _unitOfWork.Notifications
                .GetPendingNotificationsAsync(batchSize);

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    await ProcessNotificationAsync(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to process notification {NotificationId} for user {UserId}",
                        notification.Id, notification.UserId);

                    notification.RetryCount++;
                    notification.ErrorMessage = ex.Message;

                    if (notification.RetryCount >= notification.MaxRetryCount)
                    {
                        notification.Status = NotificationStatus.Failed;
                        _logger.LogError(
                            "Notification {NotificationId} failed permanently after {RetryCount} attempts",
                            notification.Id, notification.RetryCount);
                    }

                    await _unitOfWork.SaveAsync();
                }
            }
        }

        private async Task ProcessNotificationAsync(Notification notification)
        {
            var user = await _userManager.FindByIdAsync(notification.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for notification {NotificationId}",
                    notification.UserId, notification.Id);
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "User not found";
                await _unitOfWork.SaveAsync();
                return;
            }

            switch (notification.Type)
            {
                case NotificationType.Email:
                    await ProcessEmailNotificationAsync(notification, user);
                    break;

                case NotificationType.Sms:
                    await ProcessSmsNotificationAsync(notification, user);
                    break;

                case NotificationType.InApp:
                    // In-app notifications are already marked as sent
                    notification.Status = NotificationStatus.Sent;
                    notification.SentAt = DateTime.UtcNow;
                    break;

                default:
                    _logger.LogWarning("Unknown notification type {Type}", notification.Type);
                    notification.Status = NotificationStatus.Failed;
                    notification.ErrorMessage = "Unknown notification type";
                    break;
            }

            await _unitOfWork.SaveAsync();
        }

        private async Task ProcessEmailNotificationAsync(Notification notification, AppUser user)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "User has no email address";
                return;
            }

            // Send email using the email service
            await _emailService.SendEmailAsync(user.Email, notification.Title, notification.Message);

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Email notification {NotificationId} sent to {Email}",
                notification.Id, user.Email);
        }

        private async Task ProcessSmsNotificationAsync(Notification notification, AppUser user)
        {
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "User has no phone number";
                return;
            }

            // Send SMS using the SMS service
            await _smsService.SendSmsAsync(user.PhoneNumber, notification.Message);

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;

            _logger.LogInformation(
                "SMS notification {NotificationId} sent to {PhoneNumber}",
                notification.Id, user.PhoneNumber);
        }
    }
}
