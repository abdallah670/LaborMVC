using LaborBLL.Response;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Result of a cancellation operation including financial outcomes
    /// </summary>
    public class CancellationResult
    {
        /// <summary>
        /// Whether the cancellation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if cancellation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The new status of the task after cancellation
        /// </summary>
        public TaskStatus? NewStatus { get; set; }

        /// <summary>
        /// Amount refunded to the client
        /// </summary>
        public decimal ClientRefundAmount { get; set; }

        /// <summary>
        /// Amount paid to the worker (if any)
        /// </summary>
        public decimal WorkerPaymentAmount { get; set; }

        /// <summary>
        /// Cancellation fee retained by platform
        /// </summary>
        public decimal PlatformFee { get; set; }

        /// <summary>
        /// Penalty tier applied
        /// </summary>
        public PenaltyTier PenaltyTier { get; set; }

        /// <summary>
        /// Whether a penalty was applied to the client
        /// </summary>
        public bool ClientPenalized { get; set; }

        /// <summary>
        /// Whether a penalty was applied to the worker
        /// </summary>
        public bool WorkerPenalized { get; set; }

        /// <summary>
        /// Description of the cancellation outcome for logging/display
        /// </summary>
        public string OutcomeDescription { get; set; } = string.Empty;

        /// <summary>
        /// Idempotency key for this operation
        /// </summary>
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Whether this was a duplicate request (already processed)
        /// </summary>
        public bool WasDuplicate { get; set; }
    }

    /// <summary>
    /// Request for cancelling a task
    /// </summary>
    public class CancellationRequest
    {
        /// <summary>
        /// ID of the task to cancel
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// ID of the user requesting cancellation (client or worker)
        /// </summary>
        public string RequestedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of cancellation
        /// </summary>
        public CancellationType CancellationType { get; set; }

        /// <summary>
        /// Reason for cancellation
        /// </summary>
        public CancellationReason Reason { get; set; }

        /// <summary>
        /// Additional notes/comments
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Idempotency key to prevent duplicate processing
        /// Format: "cancel:{TaskId}:{Action}:{ActorId}:{Timestamp}"
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }

    /// <summary>
    /// Service for handling task cancellations, no-shows, and financial settlements
    /// </summary>
    public interface ICancellationService
    {
        /// <summary>
        /// Cancels a task by the client
        /// </summary>
        Task<CancellationResult> CancelByClientAsync(CancellationRequest request);

        /// <summary>
        /// Cancels a task by the worker
        /// </summary>
        Task<CancellationResult> CancelByWorkerAsync(CancellationRequest request);

        /// <summary>
        /// Detects and handles no-show scenarios
        /// </summary>
        Task<CancellationResult> DetectNoShowAsync(int taskId);

        /// <summary>
        /// Handles worker no-show scenario
        /// </summary>
        Task<CancellationResult> HandleWorkerNoShowAsync(int taskId);

        /// <summary>
        /// Handles client no-show scenario
        /// </summary>
        Task<CancellationResult> HandleClientNoShowAsync(int taskId);

        /// <summary>
        /// Records worker check-in
        /// </summary>
        Task<bool> RecordWorkerCheckInAsync(int taskId, string workerId);

        /// <summary>
        /// Records client confirmation of presence
        /// </summary>
        Task<bool> RecordClientConfirmationAsync(int taskId, string clientId);

        /// <summary>
        /// Gets tasks that are eligible for no-show detection
        /// </summary>
        Task<IEnumerable<int>> GetTasksForNoShowDetectionAsync();

        /// <summary>
        /// Checks if a task can be cancelled in its current state
        /// </summary>
        Task<(bool CanCancel, string? Reason)> CanCancelAsync(int taskId, CancellationType type);
    }
}
