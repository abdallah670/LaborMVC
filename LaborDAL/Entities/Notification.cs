using System;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a notification for users (Email/SMS/In-App)
    /// </summary>
    public enum NotificationType
    {
        Email,
        Sms,
        InApp
    }

    public enum NotificationStatus
    {
        Pending,
        Sent,
        Failed,
        Read
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Notification entity for the notification system
    /// </summary>
    public class Notification : BaseEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// The user who will receive the notification
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Notification title/subject
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Notification message/content
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Type of notification: Email, SMS, or In-App
        /// </summary>
        public NotificationType Type { get; set; }
        
        /// <summary>
        /// Current status of the notification
        /// </summary>
        public NotificationStatus Status { get; set; }
        
        /// <summary>
        /// Priority level of the notification
        /// </summary>
        public NotificationPriority Priority { get; set; }
        
        /// <summary>
        /// Related entity type (e.g., "Task", "Booking", "Payment")
        /// </summary>
        public string? RelatedEntityType { get; set; }
        
        /// <summary>
        /// Related entity ID
        /// </summary>
        public int? RelatedEntityId { get; set; }
        
        /// <summary>
        /// Additional data in JSON format
        /// </summary>
        public string? Metadata { get; set; }
        
        /// <summary>
        /// Send immediately or schedule for later
        /// </summary>
        public DateTime? ScheduledAt { get; set; }
        
        /// <summary>
        /// When the notification was actually sent
        /// </summary>
        public DateTime? SentAt { get; set; }
        
        /// <summary>
        /// When the notification was read (for In-App notifications)
        /// </summary>
        public DateTime? ReadAt { get; set; }
        
        /// <summary>
        /// Error message if sending failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public AppUser User { get; set; }
        
        public Notification()
        {
            Status = NotificationStatus.Pending;
            Priority = NotificationPriority.Normal;
            RetryCount = 0;
            MaxRetryCount = 3;
        }
    }
}
