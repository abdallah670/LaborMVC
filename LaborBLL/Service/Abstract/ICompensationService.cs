
namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for handling compensation operations in distributed transactions
    /// Each compensation action should be idempotent and handle partial failures
    /// </summary>
    public interface ICompensationService
    {
        /// <summary>
        /// Compensate a payment capture - refund the captured payment
        /// </summary>
        Task<CompensationResult> CompensatePaymentCaptureAsync(int paymentId, string reason);

        /// <summary>
        /// Compensate a worker transfer - create a reversal transfer
        /// </summary>
        Task<CompensationResult> CompensateWorkerTransferAsync(string stripeTransferId, string reason);

        /// <summary>
        /// Compensate a booking status change - revert to previous status
        /// </summary>
        Task<CompensationResult> CompensateBookingStatusChangeAsync(int bookingId, string previousStatus, string reason);

        /// <summary>
        /// Compensate payment hold - release authorization
        /// </summary>
        Task<CompensationResult> CompensatePaymentHoldAsync(int paymentId, string reason);

        /// <summary>
        /// Compensate a completed transfer by queuing a reversal
        /// </summary>
        Task<CompensationResult> CompensatePendingTransferAsync(Guid pendingTransferId, string reason);
    }

    /// <summary>
    /// Result of a compensation operation
    /// </summary>
    public class CompensationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CompensationId { get; set; }
        public bool RequiresManualIntervention { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }

        public static CompensationResult Succeeded(string? compensationId = null, Dictionary<string, object>? data = null)
        {
            return new CompensationResult
            {
                Success = true,
                CompensationId = compensationId,
                AdditionalData = data
            };
        }

        public static CompensationResult Failed(string errorMessage, bool requiresManualIntervention = false)
        {
            return new CompensationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                RequiresManualIntervention = requiresManualIntervention
            };
        }
    }
}
