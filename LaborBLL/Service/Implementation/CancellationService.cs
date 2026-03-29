using LaborBLL.Service.Abstract;
using LaborDAL.Common;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborBLL.Service
{
    /// <summary>
    /// Implementation of cancellation service with business rules, concurrency control, financial settlement, and notifications
    /// </summary>
    public class CancellationService : ICancellationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IClock _clock;
        private readonly ILogger<CancellationService> _logger;
        private readonly IPenaltyService _penaltyService;
        private readonly IStripeService _stripeService;
        private readonly IEmailService _emailService;

        // Business rule constants
        private static readonly TimeSpan FreeCancellationWindow = TimeSpan.FromHours(2);
        private static readonly TimeSpan NoShowThreshold = TimeSpan.FromMinutes(30);
        private static readonly decimal LateCancellationClientRefundPercent = 0.5m;
        private static readonly decimal LateCancellationWorkerPayPercent = 0.5m;

        public CancellationService(
            ApplicationDbContext context,
            IClock clock,
            ILogger<CancellationService> logger,
            IPenaltyService penaltyService,
            IStripeService stripeService,
            IEmailService emailService)
        {
            _context = context;
            _clock = clock;
            _logger = logger;
            _penaltyService = penaltyService;
            _stripeService = stripeService;
            _emailService = emailService;
        }

        #region Public API Methods

        /// <inheritdoc />
        public async Task<CancellationResult> CancelByClientAsync(CancellationRequest request)
        {
            var idempotencyKey = request.IdempotencyKey ??
                $"cancel:client:{request.TaskId}:{request.RequestedByUserId}:{_clock.UtcNow:yyyyMMddHHmmss}";

            _logger.LogInformation(
                "Client cancellation requested - TaskId: {TaskId}, ClientId: {ClientId}",
                request.TaskId, request.RequestedByUserId);

            // Check idempotency
            var existingOperation = await CheckIdempotencyAsync(request.TaskId, idempotencyKey);
            if (existingOperation != null)
            {
                _logger.LogInformation(
                    "Duplicate client cancellation detected - returning cached result for TaskId: {TaskId}",
                    request.TaskId);
                existingOperation.WasDuplicate = true;
                return existingOperation;
            }

            var task = await GetTaskWithConcurrencyCheckAsync(request.TaskId);
            if (task == null)
            {
                return FailureResult("Task not found");
            }

            // Validate client owns this task
            if (task.PosterId != request.RequestedByUserId)
            {
                return FailureResult("Only the task owner can cancel");
            }

            // Check if task can be cancelled
            var (canCancel, reason) = await CanCancelAsync(task, CancellationType.ClientCancellation);
            if (!canCancel)
            {
                return FailureResult(reason ?? "Task cannot be cancelled");
            }

            // Calculate cancellation outcome based on timing
            var outcome = CalculateClientCancellationOutcome(task);

            try
            {
                await ExecuteCancellationAsync(task, request, outcome, idempotencyKey);

                // Apply penalty if applicable
                if (outcome.PenaltyTier != PenaltyTier.None && outcome.NewStatus == TaskStatus.NoShow)
                {
                    await _penaltyService.ApplyClientPenaltyAsync(
                        task.PosterId,
                        task.Id,
                        outcome.PenaltyTier,
                        "Client no-show after start time");

                    // Send penalty notification
                    await SendPenaltyNotificationAsync(task.PosterId, "Client No-Show", "You have been flagged for a no-show.");
                }

                // Send cancellation notification
                await SendCancellationNotificationAsync(task, request.RequestedByUserId, outcome);

                _logger.LogInformation(
                    "Client cancellation successful - TaskId: {TaskId}, Refund: {Refund:C}, WorkerPay: {WorkerPay:C}",
                    request.TaskId, outcome.ClientRefundAmount, outcome.WorkerPaymentAmount);

                return outcome;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict during client cancellation - TaskId: {TaskId}", request.TaskId);
                return FailureResult("Task was modified by another operation. Please try again.");
            }
        }

        /// <inheritdoc />
        public async Task<CancellationResult> CancelByWorkerAsync(CancellationRequest request)
        {
            var idempotencyKey = request.IdempotencyKey ??
                $"cancel:worker:{request.TaskId}:{request.RequestedByUserId}:{_clock.UtcNow:yyyyMMddHHmmss}";

            _logger.LogInformation(
                "Worker cancellation requested - TaskId: {TaskId}, WorkerId: {WorkerId}",
                request.TaskId, request.RequestedByUserId);

            // Check idempotency
            var existingOperation = await CheckIdempotencyAsync(request.TaskId, idempotencyKey);
            if (existingOperation != null)
            {
                _logger.LogInformation(
                    "Duplicate worker cancellation detected - returning cached result for TaskId: {TaskId}",
                    request.TaskId);
                existingOperation.WasDuplicate = true;
                return existingOperation;
            }

            var task = await GetTaskWithConcurrencyCheckAsync(request.TaskId);
            if (task == null)
            {
                return FailureResult("Task not found");
            }

            // Validate worker is assigned to this task
            var isAssignedWorker = task.AssignedWorker?.Any(w => w?.Id == request.RequestedByUserId) ?? false;
            if (!isAssignedWorker)
            {
                return FailureResult("Only the assigned worker can cancel");
            }

            // Check if task can be cancelled
            var (canCancel, reason) = await CanCancelAsync(task, CancellationType.WorkerCancellation);
            if (!canCancel)
            {
                return FailureResult(reason ?? "Task cannot be cancelled");
            }

            // Calculate cancellation outcome based on timing
            var outcome = CalculateWorkerCancellationOutcome(task, request.RequestedByUserId);

            try
            {
                await ExecuteCancellationAsync(task, request, outcome, idempotencyKey);

                // Apply penalty if applicable
                if (outcome.PenaltyTier != PenaltyTier.None && outcome.WorkerPenalized)
                {
                    await _penaltyService.ApplyWorkerPenaltyAsync(
                        request.RequestedByUserId,
                        task.Id,
                        outcome.PenaltyTier,
                        "Worker cancellation less than 2 hours before start");

                    // Send penalty notification
                    await SendPenaltyNotificationAsync(request.RequestedByUserId, "Cancellation Penalty", "You have received a strike for late cancellation.");
                }

                // Send cancellation notification
                await SendCancellationNotificationAsync(task, request.RequestedByUserId, outcome);

                _logger.LogInformation(
                    "Worker cancellation successful - TaskId: {TaskId}, PenaltyTier: {PenaltyTier}",
                    request.TaskId, outcome.PenaltyTier);

                return outcome;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict during worker cancellation - TaskId: {TaskId}", request.TaskId);
                return FailureResult("Task was modified by another operation. Please try again.");
            }
        }

        /// <inheritdoc />
        public async Task<CancellationResult> DetectNoShowAsync(int taskId)
        {
            _logger.LogInformation("No-show detection initiated - TaskId: {TaskId}", taskId);

            var task = await GetTaskWithConcurrencyCheckAsync(taskId);
            if (task == null)
            {
                return FailureResult("Task not found");
            }

            // Validate no-show conditions
            if (!CanDetectNoShow(task, out string? failureReason))
            {
                return FailureResult(failureReason ?? "No-show detection not applicable");
            }

            // Determine who is the no-show party
            if (task.WorkerCheckedInAt == null && task.ClientConfirmedAt == null)
            {
                // Both didn't show
                return await HandleMutualNoShowAsync(task);
            }
            else if (task.WorkerCheckedInAt == null)
            {
                // Worker no-show
                return await HandleWorkerNoShowAsync(taskId);
            }
            else
            {
                // Client no-show
                return await HandleClientNoShowAsync(taskId);
            }
        }

        /// <inheritdoc />
        public async Task<CancellationResult> HandleWorkerNoShowAsync(int taskId)
        {
            _logger.LogInformation("Processing worker no-show - TaskId: {TaskId}", taskId);

            var task = await GetTaskWithConcurrencyCheckAsync(taskId);
            if (task == null)
            {
                return FailureResult("Task not found");
            }

            var idempotencyKey = $"noshow:worker:{taskId}:{_clock.UtcNow:yyyyMMddHHmmss}";

            // Check idempotency
            var existingOperation = await CheckIdempotencyAsync(taskId, idempotencyKey);
            if (existingOperation != null)
            {
                existingOperation.WasDuplicate = true;
                return existingOperation;
            }

            // Get assigned worker
            var worker = task.AssignedWorker?.FirstOrDefault();
            if (worker == null)
            {
                return FailureResult("No worker assigned to task");
            }

            // Full refund to client
            var outcome = new CancellationResult
            {
                Success = true,
                NewStatus = TaskStatus.NoShow,
                ClientRefundAmount = task.Budget,
                WorkerPaymentAmount = 0,
                PlatformFee = 0,
                PenaltyTier = PenaltyTier.Moderate,
                WorkerPenalized = true,
                OutcomeDescription = "Worker no-show: Full refund to client. Worker receives strike and rating penalty."
            };

            var request = new CancellationRequest
            {
                TaskId = taskId,
                CancellationType = CancellationType.SystemCancellation,
                Reason = CancellationReason.WorkerNoShow
            };

            try
            {
                await ExecuteNoShowAsync(task, request, outcome, idempotencyKey, "Worker");

                // Apply worker penalties via PenaltyService
                await _penaltyService.ApplyWorkerPenaltyAsync(
                    worker.Id,
                    taskId,
                    PenaltyTier.Moderate,
                    "Worker no-show - did not check in within 30 minutes of start time");

                // Send penalty notification to worker
                await SendPenaltyNotificationAsync(worker.Id, "No-Show Penalty", "You have been penalized for not showing up to an assigned task.");

                _logger.LogInformation(
                    "Worker no-show processed - TaskId: {TaskId}, ClientRefund: {Refund:C}",
                    taskId, outcome.ClientRefundAmount);

                return outcome;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict during worker no-show - TaskId: {TaskId}", taskId);
                return FailureResult("Task was modified by another operation. Please try again.");
            }
        }

        /// <inheritdoc />
        public async Task<CancellationResult> HandleClientNoShowAsync(int taskId)
        {
            _logger.LogInformation("Processing client no-show - TaskId: {TaskId}", taskId);

            var task = await GetTaskWithConcurrencyCheckAsync(taskId);
            if (task == null)
            {
                return FailureResult("Task not found");
            }

            var idempotencyKey = $"noshow:client:{taskId}:{_clock.UtcNow:yyyyMMddHHmmss}";

            // Check idempotency
            var existingOperation = await CheckIdempotencyAsync(taskId, idempotencyKey);
            if (existingOperation != null)
            {
                existingOperation.WasDuplicate = true;
                return existingOperation;
            }

            // Full payment to worker
            var outcome = new CancellationResult
            {
                Success = true,
                NewStatus = TaskStatus.NoShow,
                ClientRefundAmount = 0,
                WorkerPaymentAmount = task.Budget,
                PlatformFee = 0,
                PenaltyTier = PenaltyTier.Moderate,
                ClientPenalized = true,
                OutcomeDescription = "Client no-show: Full payment to worker. Client flagged for no-show."
            };

            var request = new CancellationRequest
            {
                TaskId = taskId,
                CancellationType = CancellationType.SystemCancellation,
                Reason = CancellationReason.ClientNoShow
            };

            try
            {
                await ExecuteNoShowAsync(task, request, outcome, idempotencyKey, "Client");

                // Apply client penalties via PenaltyService
                await _penaltyService.ApplyClientPenaltyAsync(
                    task.PosterId,
                    taskId,
                    PenaltyTier.Moderate,
                    "Client no-show - did not confirm presence after worker check-in");

                // Send penalty notification to client
                await SendPenaltyNotificationAsync(task.PosterId, "No-Show Penalty", "You have been flagged for not showing up to your scheduled task.");

                _logger.LogInformation(
                    "Client no-show processed - TaskId: {TaskId}, WorkerPay: {Pay:C}",
                    taskId, outcome.WorkerPaymentAmount);

                return outcome;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict during client no-show - TaskId: {TaskId}", taskId);
                return FailureResult("Task was modified by another operation. Please try again.");
            }
        }

        /// <inheritdoc />
        public async Task<bool> RecordWorkerCheckInAsync(int taskId, string workerId)
        {
            _logger.LogInformation("Worker check-in - TaskId: {TaskId}, WorkerId: {WorkerId}", taskId, workerId);

            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            // Validate worker is assigned
            var isAssigned = task.AssignedWorker?.Any(w => w?.Id == workerId) ?? false;
            if (!isAssigned) return false;

            // Check if check-in is too late (after no-show threshold)
            if (task.StartTime.HasValue)
            {
                var noShowThreshold = task.StartTime.Value.Add(NoShowThreshold);
                if (_clock.UtcNow > noShowThreshold)
                {
                    _logger.LogWarning(
                        "Late worker check-in - TaskId: {TaskId}, CheckInTime: {CheckInTime}, Threshold: {Threshold}",
                        taskId, _clock.UtcNow, noShowThreshold);
                }
            }

            task.WorkerCheckedInAt = _clock.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RecordClientConfirmationAsync(int taskId, string clientId)
        {
            _logger.LogInformation("Client confirmation - TaskId: {TaskId}, ClientId: {ClientId}", taskId, clientId);

            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            // Validate client owns the task
            if (task.PosterId != clientId) return false;

            task.ClientConfirmedAt = _clock.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<int>> GetTasksForNoShowDetectionAsync()
        {
            var threshold = _clock.UtcNow.Subtract(NoShowThreshold);

            var tasks = await _context.Tasks
                .AsNoTracking()
                .Where(t =>
                    t.Status == TaskStatus.Scheduled &&
                    t.StartTime.HasValue &&
                    t.StartTime <= threshold &&
                    !t.StartedAt.HasValue &&
                    !t.NoShowDetectedAt.HasValue)
                .Select(t => t.Id)
                .ToListAsync();

            return tasks;
        }

        /// <inheritdoc />
        public async Task<(bool CanCancel, string? Reason)> CanCancelAsync(int taskId, CancellationType type)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return (false, "Task not found");
            }

            return await CanCancelAsync(task, type);
        }

        #endregion

        #region Private Helper Methods

        private async Task<TaskItem?> GetTaskWithConcurrencyCheckAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.AssignedWorker)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        private async Task<CancellationResult?> CheckIdempotencyAsync(int taskId, string idempotencyKey)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task?.LastOperationIdempotencyKey == idempotencyKey)
            {
                return new CancellationResult
                {
                    Success = true,
                    WasDuplicate = true,
                    IdempotencyKey = idempotencyKey,
                    OutcomeDescription = "Operation already processed"
                };
            }

            return null;
        }

        private async Task<(bool CanCancel, string? Reason)> CanCancelAsync(TaskItem task, CancellationType type)
        {
            if (task.Status == TaskStatus.Cancelled ||
                task.Status == TaskStatus.Completed ||
                task.Status == TaskStatus.NoShow)
            {
                return (false, "Task is already in a terminal state");
            }

            if (task.StartedAt.HasValue)
            {
                return (false, "Task has already started and cannot be cancelled");
            }

            if (type == CancellationType.WorkerCancellation &&
                task.StartTime.HasValue &&
                _clock.UtcNow >= task.StartTime.Value)
            {
                return (false, "Cannot cancel after task start time. This will be marked as a no-show.");
            }

            return (true, null);
        }

        private CancellationResult CalculateClientCancellationOutcome(TaskItem task)
        {
            if (!task.StartTime.HasValue)
            {
                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.Cancelled,
                    ClientRefundAmount = task.Budget,
                    WorkerPaymentAmount = 0,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.None,
                    OutcomeDescription = "Full refund to client (no scheduled start time)"
                };
            }

            var now = _clock.UtcNow;
            var timeUntilStart = task.StartTime.Value - now;

            if (timeUntilStart >= FreeCancellationWindow)
            {
                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.Cancelled,
                    ClientRefundAmount = task.Budget,
                    WorkerPaymentAmount = 0,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.None,
                    OutcomeDescription = "Full refund: Cancelled more than 2 hours before start time"
                };
            }
            else if (timeUntilStart > TimeSpan.Zero)
            {
                var clientRefund = task.Budget * LateCancellationClientRefundPercent;
                var workerPay = task.Budget * LateCancellationWorkerPayPercent;

                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.Cancelled,
                    ClientRefundAmount = clientRefund,
                    WorkerPaymentAmount = workerPay,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.None,
                    OutcomeDescription = $"50% refund: Cancelled less than 2 hours before start. Client: {clientRefund:C}, Worker: {workerPay:C}"
                };
            }
            else
            {
                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.NoShow,
                    ClientRefundAmount = 0,
                    WorkerPaymentAmount = task.Budget,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.Moderate,
                    ClientPenalized = true,
                    OutcomeDescription = "Client no-show: After start time. Full payment to worker, client flagged."
                };
            }
        }

        private CancellationResult CalculateWorkerCancellationOutcome(TaskItem task, string workerId)
        {
            if (!task.StartTime.HasValue)
            {
                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.Cancelled,
                    ClientRefundAmount = task.Budget,
                    WorkerPaymentAmount = 0,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.None,
                    OutcomeDescription = "Worker cancellation accepted (no scheduled start time)"
                };
            }

            var now = _clock.UtcNow;
            var timeUntilStart = task.StartTime.Value - now;

            if (timeUntilStart >= FreeCancellationWindow)
            {
                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.Cancelled,
                    ClientRefundAmount = task.Budget,
                    WorkerPaymentAmount = 0,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.None,
                    OutcomeDescription = "Worker cancellation accepted: More than 2 hours before start"
                };
            }
            else if (timeUntilStart > TimeSpan.Zero)
            {
                return new CancellationResult
                {
                    Success = true,
                    NewStatus = TaskStatus.Cancelled,
                    ClientRefundAmount = task.Budget,
                    WorkerPaymentAmount = 0,
                    PlatformFee = 0,
                    PenaltyTier = PenaltyTier.Moderate,
                    WorkerPenalized = true,
                    OutcomeDescription = "Worker cancellation accepted with penalty: Less than 2 hours before start. Rating decrease and strike applied."
                };
            }
            else
            {
                return new CancellationResult
                {
                    Success = false,
                    ErrorMessage = "Cannot cancel after start time. This is a severe violation."
                };
            }
        }

        private bool CanDetectNoShow(TaskItem task, out string? failureReason)
        {
            failureReason = null;

            if (task.Status == TaskStatus.Cancelled ||
                task.Status == TaskStatus.Completed ||
                task.Status == TaskStatus.NoShow)
            {
                failureReason = "Task is already in a terminal state";
                return false;
            }

            if (!task.StartTime.HasValue)
            {
                failureReason = "Task has no scheduled start time";
                return false;
            }

            if (task.StartedAt.HasValue)
            {
                failureReason = "Task has already started";
                return false;
            }

            var noShowThreshold = task.StartTime.Value.Add(NoShowThreshold);
            if (_clock.UtcNow < noShowThreshold)
            {
                failureReason = $"No-show threshold not reached yet. Threshold: {noShowThreshold:O}";
                return false;
            }

            if (task.NoShowDetectedAt.HasValue)
            {
                failureReason = "No-show already detected";
                return false;
            }

            return true;
        }

        private async Task<CancellationResult> HandleMutualNoShowAsync(TaskItem task)
        {
            _logger.LogInformation("Processing mutual no-show - TaskId: {TaskId}", task.Id);

            var idempotencyKey = $"noshow:mutual:{task.Id}:{_clock.UtcNow:yyyyMMddHHmmss}";

            var outcome = new CancellationResult
            {
                Success = true,
                NewStatus = TaskStatus.Cancelled,
                ClientRefundAmount = task.Budget,
                WorkerPaymentAmount = 0,
                PlatformFee = 0,
                PenaltyTier = PenaltyTier.None,
                OutcomeDescription = "Mutual no-show: Full refund. No penalties applied (MVP)."
            };

            var request = new CancellationRequest
            {
                TaskId = task.Id,
                CancellationType = CancellationType.SystemCancellation,
                Reason = CancellationReason.MutualNoShow
            };

            await ExecuteNoShowAsync(task, request, outcome, idempotencyKey, "Mutual");

            return outcome;
        }

        private async Task ExecuteCancellationAsync(
            TaskItem task,
            CancellationRequest request,
            CancellationResult outcome,
            string idempotencyKey)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                task.Status = outcome.NewStatus ?? TaskStatus.Cancelled;
                task.CancelledAt = _clock.UtcNow;
                task.CancelledBy = request.RequestedByUserId;
                task.CancellationType = request.CancellationType;
                task.CancellationReason = request.Reason;
                task.LastOperationIdempotencyKey = idempotencyKey;
                task.IsCancellationProcessed = false;

                await _context.SaveChangesAsync();

                await ProcessFinancialSettlementAsync(task, outcome);

                task.IsCancellationProcessed = true;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                outcome.IdempotencyKey = idempotencyKey;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ExecuteNoShowAsync(
            TaskItem task,
            CancellationRequest request,
            CancellationResult outcome,
            string idempotencyKey,
            string noShowParty)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                task.Status = TaskStatus.NoShow;
                task.NoShowDetectedAt = _clock.UtcNow;
                task.NoShowParty = noShowParty;
                task.CancelledAt = _clock.UtcNow;
                task.CancelledBy = "System";
                task.CancellationType = CancellationType.SystemCancellation;
                task.CancellationReason = request.Reason;
                task.LastOperationIdempotencyKey = idempotencyKey;
                task.IsCancellationProcessed = false;

                await _context.SaveChangesAsync();

                await ProcessFinancialSettlementAsync(task, outcome);

                task.IsCancellationProcessed = true;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                outcome.IdempotencyKey = idempotencyKey;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ProcessFinancialSettlementAsync(TaskItem task, CancellationResult outcome)
        {
            try
            {
                // Process client refund if applicable
                if (outcome.ClientRefundAmount > 0)
                {
                    // Get payment intent for this task's booking
                    var booking = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.TaskItemId == task.Id);

                    if (booking != null)
                    {
                        var paymentIntentId = await _stripeService.GetPaymentIntentForBookingAsync(booking.Id);
                        if (!string.IsNullOrEmpty(paymentIntentId))
                        {
                            var refundResult = await _stripeService.RefundPaymentAsync(
                                paymentIntentId,
                                outcome.ClientRefundAmount,
                                "requested_by_customer");

                            if (refundResult.Success)
                            {
                                _logger.LogInformation(
                                    "Refund processed - TaskId: {TaskId}, Amount: {Amount:C}, RefundId: {RefundId}",
                                    task.Id, outcome.ClientRefundAmount, refundResult.RefundId);
                            }
                            else
                            {
                                _logger.LogError(
                                    "Refund failed - TaskId: {TaskId}, Error: {Error}",
                                    task.Id, refundResult.ErrorMessage);
                            }
                        }
                    }
                }

                // Process worker payment if applicable
                if (outcome.WorkerPaymentAmount > 0)
                {
                    var worker = task.AssignedWorker?.FirstOrDefault();
                    if (worker != null && !string.IsNullOrEmpty(worker.StripeAccountId))
                    {
                        var transferResult = await _stripeService.TransferToWorkerAsync(
                            worker.StripeAccountId,
                            outcome.WorkerPaymentAmount,
                            $"Payment for task {task.Id} - {task.Title}");

                        if (transferResult.Success)
                        {
                            _logger.LogInformation(
                                "Worker payment transferred - TaskId: {TaskId}, Amount: {Amount:C}, TransferId: {TransferId}",
                                task.Id, outcome.WorkerPaymentAmount, transferResult.TransferId);
                        }
                        else
                        {
                            _logger.LogError(
                                "Worker payment failed - TaskId: {TaskId}, Error: {Error}",
                                task.Id, transferResult.ErrorMessage);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Worker payment pending - TaskId: {TaskId}, Worker has no Stripe account",
                            task.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Financial settlement failed - TaskId: {TaskId}", task.Id);
                throw;
            }
        }

        private async Task SendCancellationNotificationAsync(TaskItem task, string userId, CancellationResult outcome)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.Email)) return;

                var subject = $"Task Cancelled - {task.Title}";
                var body = $@"
                    <h2>Task Cancellation Notification</h2>
                    <p>Your task <strong>{task.Title}</strong> has been cancelled.</p>
                    <p><strong>Outcome:</strong> {outcome.OutcomeDescription}</p>
                    <p><strong>Client Refund:</strong> {outcome.ClientRefundAmount:C}</p>
                    <p><strong>Worker Payment:</strong> {outcome.WorkerPaymentAmount:C}</p>
                    <p>If you have any questions, please contact support.</p>
                ";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation notification - TaskId: {TaskId}", task.Id);
            }
        }

        private async Task SendPenaltyNotificationAsync(string userId, string penaltyType, string message)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.Email)) return;

                var subject = $"Account Notice - {penaltyType}";
                var body = $@"
                    <h2>Account Penalty Notification</h2>
                    <p>Dear {user.FirstName ?? "User"},</p>
                    <p>{message}</p>
                    <p>This may affect your ability to post or accept tasks. Please review our terms of service.</p>
                    <p>If you believe this is an error, please contact support.</p>
                ";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send penalty notification - UserId: {UserId}", userId);
            }
        }

        private CancellationResult FailureResult(string errorMessage)
        {
            return new CancellationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        #endregion
    }
}
