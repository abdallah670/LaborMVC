using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Audit log entry for file upload attempts
    /// </summary>
    public class FileUploadAuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ID of the user who attempted the upload (null for anonymous)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// User name/email for easier identification
        /// </summary>
        [MaxLength(256)]
        public string? UserName { get; set; }

        /// <summary>
        /// IP address of the request
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        [MaxLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        [MaxLength(260)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Sanitized file name
        /// </summary>
        [MaxLength(260)]
        public string? SanitizedFileName { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        [MaxLength(50)]
        public string? FileExtension { get; set; }

        /// <summary>
        /// MIME type declared by the client
        /// </summary>
        [MaxLength(128)]
        public string? DeclaredMimeType { get; set; }

        /// <summary>
        /// MIME type detected from file content
        /// </summary>
        [MaxLength(128)]
        public string? DetectedMimeType { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// SHA256 hash of the file content
        /// </summary>
        [MaxLength(64)]
        public string? FileHash { get; set; }

        /// <summary>
        /// Whether the upload was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Whether the upload was blocked due to security violation
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Type of security violation if blocked
        /// </summary>
        [MaxLength(50)]
        public string? ViolationType { get; set; }

        /// <summary>
        /// Error code if validation failed
        /// </summary>
        [MaxLength(50)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        [MaxLength(1024)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Risk level of the violation
        /// </summary>
        [MaxLength(20)]
        public string? RiskLevel { get; set; }

        /// <summary>
        /// List of detected threats (serialized as JSON)
        /// </summary>
        public string? DetectedThreats { get; set; }

        /// <summary>
        /// Width of image if applicable
        /// </summary>
        public int? ImageWidth { get; set; }

        /// <summary>
        /// Height of image if applicable
        /// </summary>
        public int? ImageHeight { get; set; }

        /// <summary>
        /// Duration of validation in milliseconds
        /// </summary>
        public long? ValidationDurationMs { get; set; }

        /// <summary>
        /// Correlation ID for tracking related operations
        /// </summary>
        [MaxLength(50)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Timestamp when the upload was attempted
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Geolocation data (country/region)
        /// </summary>
        [MaxLength(100)]
        public string? Geolocation { get; set; }

        /// <summary>
        /// Request path where upload was attempted
        /// </summary>
        [MaxLength(512)]
        public string? RequestPath { get; set; }

        /// <summary>
        /// Additional metadata (serialized as JSON)
        /// </summary>
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Model for creating audit log entries
    /// </summary>
    public class FileUploadAuditEntry
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? OriginalFileName { get; set; }
        public string? SanitizedFileName { get; set; }
        public string? FileExtension { get; set; }
        public string? DeclaredMimeType { get; set; }
        public string? DetectedMimeType { get; set; }
        public long FileSize { get; set; }
        public string? FileHash { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsBlocked { get; set; }
        public string? ViolationType { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RiskLevel { get; set; }
        public List<string>? DetectedThreats { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public long? ValidationDurationMs { get; set; }
        public string? CorrelationId { get; set; }
        public string? RequestPath { get; set; }
        public string? Geolocation { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
