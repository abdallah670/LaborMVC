
using LaborDAL.Entities;
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository interface for PendingTransfer operations
    /// </summary>
    public interface IPendingTransferRepository : IRepository<PendingTransfer>
    {
        /// <summary>
        /// Get pending transfers ready for processing
        /// </summary>
        Task<IEnumerable<PendingTransfer>> GetPendingTransfersAsync(int batchSize = 100);

        /// <summary>
        /// Get transfers by status
        /// </summary>
        Task<IEnumerable<PendingTransfer>> GetTransfersByStatusAsync(TransferStatus status, int take = 100);

        /// <summary>
        /// Get transfers by payment ID
        /// </summary>
        Task<IEnumerable<PendingTransfer>> GetTransfersByPaymentIdAsync(int paymentId);

        /// <summary>
        /// Get transfers by booking ID
        /// </summary>
        Task<IEnumerable<PendingTransfer>> GetTransfersByBookingIdAsync(int bookingId);

        /// <summary>
        /// Get transfers by transfer group
        /// </summary>
        Task<IEnumerable<PendingTransfer>> GetTransfersByGroupAsync(string transferGroup);

        /// <summary>
        /// Acquire lock on a transfer for processing
        /// </summary>
        Task<bool> AcquireLockAsync(Guid transferId, string lockToken, TimeSpan lockDuration);

        /// <summary>
        /// Release lock on a transfer
        /// </summary>
        Task<bool> ReleaseLockAsync(Guid transferId, string lockToken);

        /// <summary>
        /// Mark transfer as completed with Stripe transfer ID
        /// </summary>
        Task<bool> MarkCompletedAsync(Guid transferId, string stripeTransferId, string lockToken);

        /// <summary>
        /// Mark transfer as failed with error
        /// </summary>
        Task<bool> MarkFailedAsync(Guid transferId, string errorMessage, string lockToken);

        /// <summary>
        /// Schedule transfer for retry
        /// </summary>
        Task<bool> ScheduleRetryAsync(Guid transferId, string lockToken);

        /// <summary>
        /// Mark transfer as permanently failed
        /// </summary>
        Task<bool> MarkPermanentlyFailedAsync(Guid transferId, string reason, string lockToken);

        /// <summary>
        /// Cancel a pending transfer
        /// </summary>
        Task<bool> CancelTransferAsync(Guid transferId, string reason);

        /// <summary>
        /// Get transfer statistics
        /// </summary>
        Task<Dictionary<TransferStatus, int>> GetTransferCountByStatusAsync();

        /// <summary>
        /// Get total amount pending transfer
        /// </summary>
        Task<decimal> GetTotalPendingAmountAsync();
    }
}
