using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LaborBLL.Common;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Http;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for logging file upload audit events
    /// </summary>
    public interface IFileUploadAuditService
    {
        /// <summary>
        /// Log a successful file upload
        /// </summary>
        Task LogUploadSuccessAsync(
            IFormFile file,
            string sanitizedFileName,
            string? detectedMimeType,
            string? fileHash,
            int? imageWidth = null,
            int? imageHeight = null,
            long? validationDurationMs = null,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log a blocked file upload (security violation)
        /// </summary>
        Task LogUploadBlockedAsync(
            IFormFile file,
            FileUploadSecurityException exception,
            List<string>? detectedThreats = null,
            long? validationDurationMs = null,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log a failed file upload (validation error)
        /// </summary>
        Task LogUploadFailureAsync(
            IFormFile file,
            string errorCode,
            string errorMessage,
            long? validationDurationMs = null,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get upload statistics
        /// </summary>
        Task<FileUploadStatistics> GetStatisticsAsync(DateTime? since = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get blocked uploads
        /// </summary>
        Task<IEnumerable<FileUploadAuditLog>> GetBlockedUploadsAsync(DateTime? since = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clean up old audit logs
        /// </summary>
        Task<int> CleanupOldLogsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
    }
}
