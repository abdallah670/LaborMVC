using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LaborDAL.Entities;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository for file upload audit logs
    /// </summary>
    public interface IFileUploadAuditRepo
    {
        /// <summary>
        /// Add a new audit log entry
        /// </summary>
        Task<FileUploadAuditLog> AddAsync(FileUploadAuditLog auditLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit log by ID
        /// </summary>
        Task<FileUploadAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit logs for a specific user
        /// </summary>
        Task<IEnumerable<FileUploadAuditLog>> GetByUserIdAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get blocked uploads
        /// </summary>
        Task<IEnumerable<FileUploadAuditLog>> GetBlockedUploadsAsync(DateTime? since = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get recent audit logs
        /// </summary>
        Task<IEnumerable<FileUploadAuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get upload count for user within time period
        /// </summary>
        Task<int> GetUploadCountForUserAsync(string userId, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get upload count by IP within time period
        /// </summary>
        Task<int> GetUploadCountByIpAsync(string ipAddress, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete old audit logs
        /// </summary>
        Task<int> DeleteOldLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get statistics for a time period
        /// </summary>
        Task<FileUploadStatistics> GetStatisticsAsync(DateTime? since = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics for file uploads
    /// </summary>
    public class FileUploadStatistics
    {
        public int TotalUploads { get; set; }
        public int SuccessfulUploads { get; set; }
        public int BlockedUploads { get; set; }
        public int FailedUploads { get; set; }
        public Dictionary<string, int> BlockedByViolationType { get; set; } = new();
        public Dictionary<string, int> UploadsByExtension { get; set; } = new();
        public long TotalBytesUploaded { get; set; }
    }
}
