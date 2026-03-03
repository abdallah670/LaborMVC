
using LaborBLL.ModelVM;
using LaborBLL.Response;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for orchestrating distributed transactions across multiple services
    /// Implements the Saga pattern for atomic operations involving payments, transfers, and booking updates
    /// </summary>
    public interface IDistributedTransactionService
    {
        /// <summary>
        /// Execute payment release saga: capture payment and transfer to worker atomically
        /// </summary>
        Task<Response<bool>> ExecutePaymentReleaseAsync(int bookingId, string workerStripeAccountId);

        /// <summary>
        /// Execute payment hold saga: hold payment for a booking
        /// </summary>
        Task<Response<bool>> ExecutePaymentHoldAsync(int bookingId);

        /// <summary>
        /// Execute booking cancellation saga: process refund and update booking status
        /// </summary>
        Task<Response<bool>> ExecuteBookingCancellationAsync(int bookingId, string cancelledBy, bool isLateCancellation);

        /// <summary>
        /// Get the status of a distributed transaction by correlation ID
        /// </summary>
        Task<TransactionStatusResponse> GetTransactionStatusAsync(string correlationId);

        /// <summary>
        /// Retry a failed transaction
        /// </summary>
        Task<Response<bool>> RetryTransactionAsync(string correlationId);

        /// <summary>
        /// Get pending transfer statistics
        /// </summary>
        Task<TransferStatistics> GetTransferStatisticsAsync();
    }

    /// <summary>
    /// Response containing transaction status information
    /// </summary>
    public class TransactionStatusResponse
    {
        public bool Found { get; set; }
        public string? CorrelationId { get; set; }
        public string? SagaType { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public List<StepStatus>? Steps { get; set; }
    }

    /// <summary>
    /// Status of an individual saga step
    /// </summary>
    public class StepStatus
    {
        public string? StepName { get; set; }
        public string? Status { get; set; } // Executed, Compensated, Failed
        public DateTime? ExecutedAt { get; set; }
        public DateTime? CompensatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Transfer statistics for monitoring
    /// </summary>
    public class TransferStatistics
    {
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int PermanentlyFailedCount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public int DeadLetterCount { get; set; }
    }
}
