using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository for file upload audit logs
    /// </summary>
    public class FileUploadAuditRepo : IFileUploadAuditRepo
    {
        private readonly ApplicationDbContext _context;

        public FileUploadAuditRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FileUploadAuditLog> AddAsync(FileUploadAuditLog auditLog, CancellationToken cancellationToken = default)
        {
            await _context.FileUploadAuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return auditLog;
        }

        public async Task<FileUploadAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.FileUploadAuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<FileUploadAuditLog>> GetByUserIdAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            return await _context.FileUploadAuditLogs
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FileUploadAuditLog>> GetBlockedUploadsAsync(DateTime? since = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            var query = _context.FileUploadAuditLogs
                .AsNoTracking()
                .Where(x => x.IsBlocked);

            if (since.HasValue)
            {
                query = query.Where(x => x.Timestamp >= since.Value);
            }

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FileUploadAuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
        {
            return await _context.FileUploadAuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUploadCountForUserAsync(string userId, DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.FileUploadAuditLogs
                .AsNoTracking()
                .CountAsync(x => x.UserId == userId && x.Timestamp >= since, cancellationToken);
        }

        public async Task<int> GetUploadCountByIpAsync(string ipAddress, DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.FileUploadAuditLogs
                .AsNoTracking()
                .CountAsync(x => x.IpAddress == ipAddress && x.Timestamp >= since, cancellationToken);
        }

        public async Task<int> DeleteOldLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var logsToDelete = await _context.FileUploadAuditLogs
                .Where(x => x.Timestamp < olderThan)
                .ToListAsync(cancellationToken);

            _context.FileUploadAuditLogs.RemoveRange(logsToDelete);
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<FileUploadStatistics> GetStatisticsAsync(DateTime? since = null, CancellationToken cancellationToken = default)
        {
            var query = _context.FileUploadAuditLogs.AsNoTracking();

            if (since.HasValue)
            {
                query = query.Where(x => x.Timestamp >= since.Value);
            }

            var logs = await query.ToListAsync(cancellationToken);

            var stats = new FileUploadStatistics
            {
                TotalUploads = logs.Count,
                SuccessfulUploads = logs.Count(x => x.IsSuccess && !x.IsBlocked),
                BlockedUploads = logs.Count(x => x.IsBlocked),
                FailedUploads = logs.Count(x => !x.IsSuccess && !x.IsBlocked),
                BlockedByViolationType = logs
                    .Where(x => x.IsBlocked && !string.IsNullOrEmpty(x.ViolationType))
                    .GroupBy(x => x.ViolationType!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                UploadsByExtension = logs
                    .Where(x => !string.IsNullOrEmpty(x.FileExtension))
                    .GroupBy(x => x.FileExtension!.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Count()),
                TotalBytesUploaded = logs
                    .Where(x => x.IsSuccess && !x.IsBlocked)
                    .Sum(x => x.FileSize)
            };

            return stats;
        }
    }
}
