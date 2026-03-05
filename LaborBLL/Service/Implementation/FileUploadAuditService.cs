using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LaborBLL.Common;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for auditing file upload operations
    /// </summary>
    public class FileUploadAuditService : IFileUploadAuditService
    {
        private readonly IFileUploadAuditRepo _auditRepo;
        private readonly FileUploadAuditSettings _settings;
        private readonly ILogger<FileUploadAuditService> _logger;

        public FileUploadAuditService(
            IFileUploadAuditRepo auditRepo,
            IOptions<FileUploadAuditSettings> settings,
            ILogger<FileUploadAuditService> logger)
        {
            _auditRepo = auditRepo;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task LogUploadSuccessAsync(
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
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled || !_settings.LogSuccessfulUploads)
                return;

            try
            {
                var auditLog = new FileUploadAuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Substring(0, Math.Min(userAgent?.Length ?? 0, 512)),
                    OriginalFileName = file.FileName?.Substring(0, Math.Min(file.FileName?.Length ?? 0, 260)),
                    SanitizedFileName = sanitizedFileName?.Substring(0, Math.Min(sanitizedFileName?.Length ?? 0, 260)),
                    FileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant(),
                    DeclaredMimeType = file.ContentType,
                    DetectedMimeType = detectedMimeType,
                    FileSize = file.Length,
                    FileHash = fileHash ?? await CalculateFileHashAsync(file, cancellationToken),
                    IsSuccess = true,
                    IsBlocked = false,
                    ImageWidth = imageWidth,
                    ImageHeight = imageHeight,
                    ValidationDurationMs = validationDurationMs,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };

                await _auditRepo.AddAsync(auditLog, cancellationToken);

                _logger.LogDebug(
                    "Logged successful upload: {FileName} by user {UserId} from IP {IpAddress}",
                    file.FileName, userId, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log successful upload for file {FileName}", file.FileName);
            }
        }

        public async Task LogUploadBlockedAsync(
            IFormFile file,
            FileUploadSecurityException exception,
            List<string>? detectedThreats = null,
            long? validationDurationMs = null,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled || !_settings.LogFailedAttempts)
                return;

            try
            {
                var auditLog = new FileUploadAuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Substring(0, Math.Min(userAgent?.Length ?? 0, 512)),
                    OriginalFileName = file.FileName?.Substring(0, Math.Min(file.FileName?.Length ?? 0, 260)),
                    SanitizedFileName = exception.FileName?.Substring(0, Math.Min(exception.FileName?.Length ?? 0, 260)),
                    FileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant(),
                    DeclaredMimeType = file.ContentType,
                    FileSize = file.Length,
                    IsSuccess = false,
                    IsBlocked = true,
                    ViolationType = exception.ViolationType.ToString(),
                    ErrorCode = exception.ErrorCode,
                    ErrorMessage = exception.Message?.Substring(0, Math.Min(exception.Message?.Length ?? 0, 1024)),
                    RiskLevel = exception.RiskLevel.ToString(),
                    DetectedThreats = detectedThreats != null ? JsonSerializer.Serialize(detectedThreats) : null,
                    ValidationDurationMs = validationDurationMs,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };

                await _auditRepo.AddAsync(auditLog, cancellationToken);

                _logger.LogWarning(
                    "Logged blocked upload: {FileName} by user {UserId} from IP {IpAddress}. " +
                    "Violation: {ViolationType}, Risk: {RiskLevel}",
                    file.FileName, userId, ipAddress, exception.ViolationType, exception.RiskLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log blocked upload for file {FileName}", file.FileName);
            }
        }

        public async Task LogUploadFailureAsync(
            IFormFile file,
            string errorCode,
            string errorMessage,
            long? validationDurationMs = null,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled || !_settings.LogFailedAttempts)
                return;

            try
            {
                var auditLog = new FileUploadAuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Substring(0, Math.Min(userAgent?.Length ?? 0, 512)),
                    OriginalFileName = file.FileName?.Substring(0, Math.Min(file.FileName?.Length ?? 0, 260)),
                    FileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant(),
                    DeclaredMimeType = file.ContentType,
                    FileSize = file.Length,
                    IsSuccess = false,
                    IsBlocked = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage?.Substring(0, Math.Min(errorMessage?.Length ?? 0, 1024)),
                    ValidationDurationMs = validationDurationMs,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };

                await _auditRepo.AddAsync(auditLog, cancellationToken);

                _logger.LogDebug(
                    "Logged failed upload: {FileName} by user {UserId} from IP {IpAddress}. Error: {ErrorCode}",
                    file.FileName, userId, ipAddress, errorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log upload failure for file {FileName}", file.FileName);
            }
        }

        public async Task<FileUploadStatistics> GetStatisticsAsync(DateTime? since = null, CancellationToken cancellationToken = default)
        {
            return await _auditRepo.GetStatisticsAsync(since, cancellationToken);
        }

        public async Task<IEnumerable<FileUploadAuditLog>> GetBlockedUploadsAsync(
            DateTime? since = null,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            return await _auditRepo.GetBlockedUploadsAsync(since, skip, take, cancellationToken);
        }

        public async Task<int> CleanupOldLogsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
            var deletedCount = await _auditRepo.DeleteOldLogsAsync(cutoffDate, cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} old file upload audit logs older than {CutoffDate}",
                deletedCount, cutoffDate);

            return deletedCount;
        }

        private async Task<string?> CalculateFileHashAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (!_settings.CalculateFileHash)
                return null;

            try
            {
                using var stream = file.OpenReadStream();
                using var sha256 = SHA256.Create();
                var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
                return Convert.ToHexString(hash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate file hash for {FileName}", file.FileName);
                return null;
            }
        }
    }
}
