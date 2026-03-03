
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for OutboxMessage
    /// </summary>
    public class OutboxMessageRepository : Repository<OutboxMessage>, IOutboxMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public OutboxMessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100)
        {
            var now = DateTime.UtcNow;
            return await _context.Set<OutboxMessage>()
                .Where(m => (m.Status == OutboxMessageStatus.Pending || 
                            (m.Status == OutboxMessageStatus.Failed && m.RetryCount < m.MaxRetryCount)) &&
                           (m.LockToken == null || m.LockExpiryAt <= now) &&
                           (m.ScheduledAt == null || m.ScheduledAt <= now))
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<OutboxMessage>> GetMessagesByStatusAsync(OutboxMessageStatus status, int take = 100)
        {
            return await _context.Set<OutboxMessage>()
                .Where(m => m.Status == status)
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<OutboxMessage>> GetMessagesByAggregateAsync(string aggregateType, int aggregateId)
        {
            return await _context.Set<OutboxMessage>()
                .Where(m => m.AggregateType == aggregateType && m.AggregateId == aggregateId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AcquireLockAsync(Guid messageId, string lockToken, TimeSpan lockDuration)
        {
            var message = await _context.Set<OutboxMessage>().FindAsync(messageId);
            if (message == null || message.IsLocked())
                return false;

            message.LockToken = lockToken;
            message.LockExpiryAt = DateTime.UtcNow.Add(lockDuration);
            message.Status = OutboxMessageStatus.Processing;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseLockAsync(Guid messageId, string lockToken)
        {
            var message = await _context.Set<OutboxMessage>().FindAsync(messageId);
            if (message == null || message.LockToken != lockToken)
                return false;

            message.LockToken = null;
            message.LockExpiryAt = null;
            if (message.Status == OutboxMessageStatus.Processing)
            {
                message.Status = OutboxMessageStatus.Pending;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkCompletedAsync(Guid messageId, string lockToken)
        {
            var message = await _context.Set<OutboxMessage>().FindAsync(messageId);
            if (message == null || message.LockToken != lockToken)
                return false;

            message.Status = OutboxMessageStatus.Completed;
            message.CompletedAt = DateTime.UtcNow;
            message.ProcessedAt = DateTime.UtcNow;
            message.LockToken = null;
            message.LockExpiryAt = null;
            message.ErrorMessage = null;
            message.ErrorStackTrace = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkFailedAsync(Guid messageId, string errorMessage, string? stackTrace = null)
        {
            var message = await _context.Set<OutboxMessage>().FindAsync(messageId);
            if (message == null)
                return false;

            message.ErrorMessage = errorMessage;
            message.ErrorStackTrace = stackTrace;
            message.Status = OutboxMessageStatus.Failed;
            message.ProcessedAt = DateTime.UtcNow;
            message.LockToken = null;
            message.LockExpiryAt = null;
            message.IncrementRetry();

            if (message.RetryCount >= message.MaxRetryCount)
            {
                message.Status = OutboxMessageStatus.DeadLetter;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveToDeadLetterAsync(Guid messageId, string reason)
        {
            var message = await _context.Set<OutboxMessage>().FindAsync(messageId);
            if (message == null)
                return false;

            message.Status = OutboxMessageStatus.DeadLetter;
            message.ErrorMessage = reason;
            message.LockToken = null;
            message.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<OutboxMessage>> GetDeadLetterMessagesAsync(int take = 100)
        {
            return await _context.Set<OutboxMessage>()
                .Where(m => m.Status == OutboxMessageStatus.DeadLetter)
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Dictionary<OutboxMessageStatus, int>> GetMessageCountByStatusAsync()
        {
            return await _context.Set<OutboxMessage>()
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }
    }
}
