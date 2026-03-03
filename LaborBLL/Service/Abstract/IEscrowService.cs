

namespace LaborBLL.Service.Abstract
{
    public interface IEscrowService
    {
        Task<Response<bool>> HoldPaymentAsync(int bookingId);
        Task<Response<bool>> ReleasePaymentAsync(int bookingId);
        Task<Response<bool>> ProcessCancellationAsync(int bookingId, string cancelledBy);

        /// <summary>
        /// Check the status of a payment release transaction
        /// </summary>
        Task<TransactionStatusResponse> GetPaymentReleaseStatusAsync(string correlationId);

        /// <summary>
        /// Retry a failed payment release
        /// </summary>
        Task<Response<bool>> RetryPaymentReleaseAsync(string correlationId);

        /// <summary>
        /// Get transfer statistics for monitoring
        /// </summary>
        Task<TransferStatistics> GetTransferStatisticsAsync();
    }

}
