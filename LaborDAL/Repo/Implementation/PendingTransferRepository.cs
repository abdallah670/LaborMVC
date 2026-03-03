
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for PendingTransfer
    /// </summary>
    public class PendingTransferRepository : Repository<PendingTransfer>, IPendingTransferRepository
    {
        private readonly ApplicationDbContext _context;

        public PendingTransferRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PendingTransfer>> GetPendingTransfersAsync(int batchSize = 100)
        {
            var now = DateTime.UtcNow;
            return await _context.Set<PendingTransfer>()
                .Where(t => (t.Status == TransferStatus.Pending || t.Status == TransferStatus.Failed) &&
                           t.RetryCount < t.MaxRetryCount &&
                           (t.LockToken == null || t.LockExpiryAt <= now) &&
                           (t.NextRetryAt == null || t.NextRetryAt <= now))
                .OrderBy(t => t.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<PendingTransfer>> GetTransfersByStatusAsync(TransferStatus status, int take = 100)
        {
            return await _context.Set<PendingTransfer>()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<PendingTransfer>> GetTransfersByPaymentIdAsync(int paymentId)
        {
            return await _context.Set<PendingTransfer>()
                .Where(t => t.PaymentId == paymentId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PendingTransfer>> GetTransfersByBookingIdAsync(int bookingId)
        {
            return await _context.Set<PendingTransfer>()
                .Where(t => t.BookingId == bookingId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PendingTransfer>> GetTransfersByGroupAsync(string transferGroup)
        {
            return await _context.Set<PendingTransfer>()
                .Where(t => t.TransferGroup == transferGroup)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AcquireLockAsync(Guid transferId, string lockToken, TimeSpan lockDuration)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.IsLocked())
                return false;

            transfer.LockToken = lockToken;
            transfer.LockExpiryAt = DateTime.UtcNow.Add(lockDuration);
            transfer.Status = TransferStatus.Processing;
            transfer.LastAttemptAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseLockAsync(Guid transferId, string lockToken)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.LockToken != lockToken)
                return false;

            transfer.LockToken = null;
            transfer.LockExpiryAt = null;
            if (transfer.Status == TransferStatus.Processing)
            {
                transfer.Status = TransferStatus.Pending;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkCompletedAsync(Guid transferId, string stripeTransferId, string lockToken)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.LockToken != lockToken)
                return false;

            transfer.MarkCompleted(stripeTransferId);
            transfer.LockToken = null;
            transfer.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkFailedAsync(Guid transferId, string errorMessage, string lockToken)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.LockToken != lockToken)
                return false;

            transfer.MarkFailed(errorMessage);
            transfer.LockToken = null;
            transfer.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ScheduleRetryAsync(Guid transferId, string lockToken)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.LockToken != lockToken)
                return false;

            transfer.ScheduleRetry();
            transfer.LockToken = null;
            transfer.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkPermanentlyFailedAsync(Guid transferId, string reason, string lockToken)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.LockToken != lockToken)
                return false;

            transfer.Status = TransferStatus.PermanentlyFailed;
            transfer.ErrorMessage = reason;
            transfer.LockToken = null;
            transfer.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelTransferAsync(Guid transferId, string reason)
        {
            var transfer = await _context.Set<PendingTransfer>().FindAsync(transferId);
            if (transfer == null || transfer.Status == TransferStatus.Completed)
                return false;

            transfer.Status = TransferStatus.Cancelled;
            transfer.ErrorMessage = reason;
            transfer.LockToken = null;
            transfer.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<TransferStatus, int>> GetTransferCountByStatusAsync()
        {
            return await _context.Set<PendingTransfer>()
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<decimal> GetTotalPendingAmountAsync()
        {
            return await _context.Set<PendingTransfer>()
                .Where(t => t.Status == TransferStatus.Pending || t.Status == TransferStatus.Failed)
                .SumAsync(t => t.Amount);
        }
    }
}
