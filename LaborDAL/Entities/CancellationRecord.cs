namespace LaborDAL.Entities
{
    using LaborDAL.Enums;

    /// <summary>
    /// Records details of task cancellations for audit and tracking purposes
    /// </summary>
    public class CancellationRecord : BaseEntity
    {
        /// <summary>
        /// ID of the task that was cancelled
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Navigation property to the task
        /// </summary>
        public virtual TaskItem? Task { get; set; }

        /// <summary>
        /// ID of the user who cancelled (or "System" for automated cancellations)
        /// </summary>
        public string CancelledByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of cancellation (Client, Worker, or System)
        /// </summary>
        public CancellationType Type { get; set; }

        /// <summary>
        /// Reason for the cancellation
        /// </summary>
        public CancellationReason Reason { get; set; }

        /// <summary>
        /// Additional notes about the cancellation
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Amount refunded to the client
        /// </summary>
        public decimal ClientRefundAmount { get; set; }

        /// <summary>
        /// Amount paid to the worker
        /// </summary>
        public decimal WorkerPaymentAmount { get; set; }

        /// <summary>
        /// Platform fee retained (if any)
        /// </summary>
        public decimal PlatformFee { get; set; }

        /// <summary>
        /// Idempotency key for this cancellation
        /// </summary>
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>
        /// Whether the financial settlement has been completed
        /// </summary>
        public bool FinancialSettlementComplete { get; set; } = false;

        /// <summary>
        /// When the financial settlement was completed
        /// </summary>
        public DateTime? FinancialSettlementCompletedAt { get; set; }

        /// <summary>
        /// Whether a penalty was applied
        /// </summary>
        public bool PenaltyApplied { get; set; } = false;

        /// <summary>
        /// The penalty tier applied (if any)
        /// </summary>
        public PenaltyTier? PenaltyTier { get; set; }

        /// <summary>
        /// ID of the penalty record (if created)
        /// </summary>
        public int? PenaltyId { get; set; }

        /// <summary>
        /// Navigation property to the penalty record
        /// </summary>
        public virtual UserPenalty? Penalty { get; set; }

        /// <summary>
        /// Time between scheduled start and cancellation
        /// </summary>
        public TimeSpan? TimeBeforeStart { get; set; }

        /// <summary>
        /// Whether worker had checked in before cancellation
        /// </summary>
        public bool WorkerHadCheckedIn { get; set; } = false;

        /// <summary>
        /// Whether client had confirmed before cancellation
        /// </summary>
        public bool ClientHadConfirmed { get; set; } = false;

        /// <summary>
        /// Human-readable description of the outcome
        /// </summary>
        public string OutcomeDescription { get; set; } = string.Empty;
    }
}
