
using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Implementation of distributed transaction service using the Saga pattern
    /// Orchestrates complex transactions across multiple services with compensation support
    /// </summary>
    public class DistributedTransactionService : IDistributedTransactionService
    {
        private readonly ISagaOrchestrator _sagaOrchestrator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPendingTransferRepository _pendingTransferRepository;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<DistributedTransactionService> _logger;

        public DistributedTransactionService(
            ISagaOrchestrator sagaOrchestrator,
            IUnitOfWork unitOfWork,
            IPendingTransferRepository pendingTransferRepository,
            IPaymentService paymentService,
            ILogger<DistributedTransactionService> logger)
        {
            _sagaOrchestrator = sagaOrchestrator;
            _unitOfWork = unitOfWork;
            _pendingTransferRepository = pendingTransferRepository;
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<Response<bool>> ExecutePaymentReleaseAsync(int bookingId, string workerStripeAccountId)
        {
            var correlationId = $"payment-release-{bookingId}-{Guid.NewGuid():N}";
            var transferGroup = $"tg_{bookingId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            _logger.LogInformation(
                "Starting payment release saga for BookingId: {BookingId}, CorrelationId: {CorrelationId}",
                bookingId, correlationId);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found");
            }

            var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
            if (payment == null)
            {
                return new Response<bool>(false, false, "Payment not found");
            }

            // Validate state transition
            if (payment.Status != PaymentStatus.Held && payment.Status != PaymentStatus.Pending)
            {
                return new Response<bool>(false, false,
                    $"Payment is in invalid state: {payment.Status}. Expected: Held or Pending");
            }

            // Create saga with initial data
            var initialData = new Dictionary<string, object>
            {
                { "BookingId", bookingId },
                { "PaymentId", payment.Id },
                { "WorkerStripeAccountId", workerStripeAccountId },
                { "TransferGroup", transferGroup },
                { "Amount", payment.Amount },
                { "OriginalPaymentStatus", payment.Status.ToString() },
                { "OriginalBookingStatus", booking.Status.ToString() }
            };

            var saga = await _sagaOrchestrator.StartSagaAsync(
                "PaymentRelease",
                correlationId,
                initialData,
                "Booking",
                bookingId);

            // Define saga steps
            var steps = new List<ISagaStep>
            {
                new CapturePaymentStep(_paymentService, _unitOfWork, _logger),
                new CreatePendingTransferStep(_pendingTransferRepository, _unitOfWork, _logger),
                new UpdateBookingStatusStep(_unitOfWork, _logger)
            };

            // Execute saga
            var result = await _sagaOrchestrator.ExecuteSagaAsync(saga.Id, steps);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Payment release saga completed successfully for BookingId: {BookingId}",
                    bookingId);
                return new Response<bool>(true, true, null);
            }
            else
            {
                _logger.LogError(
                    "Payment release saga failed for BookingId: {BookingId}. Error: {Error}",
                    bookingId, result.ErrorMessage);
                return new Response<bool>(false, false, result.ErrorMessage);
            }
        }

        public async Task<Response<bool>> ExecutePaymentHoldAsync(int bookingId)
        {
            var correlationId = $"payment-hold-{bookingId}-{Guid.NewGuid():N}";

            _logger.LogInformation(
                "Starting payment hold saga for BookingId: {BookingId}, CorrelationId: {CorrelationId}",
                bookingId, correlationId);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found");
            }

            var initialData = new Dictionary<string, object>
            {
                { "BookingId", bookingId },
                { "Amount", booking.AgreedRate },
                { "UserId", booking.PosterId }
            };

            var saga = await _sagaOrchestrator.StartSagaAsync(
                "PaymentHold",
                correlationId,
                initialData,
                "Booking",
                bookingId);

            var steps = new List<ISagaStep>
            {
                new CreatePaymentIntentStep(_paymentService, _unitOfWork, _logger)
            };

            var result = await _sagaOrchestrator.ExecuteSagaAsync(saga.Id, steps);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Payment hold saga completed successfully for BookingId: {BookingId}",
                    bookingId);
                return new Response<bool>(true, true, null);
            }
            else
            {
                _logger.LogError(
                    "Payment hold saga failed for BookingId: {BookingId}. Error: {Error}",
                    bookingId, result.ErrorMessage);
                return new Response<bool>(false, false, result.ErrorMessage);
            }
        }

        public async Task<Response<bool>> ExecuteBookingCancellationAsync(int bookingId, string cancelledBy, bool isLateCancellation)
        {
            var correlationId = $"booking-cancel-{bookingId}-{Guid.NewGuid():N}";

            _logger.LogInformation(
                "Starting booking cancellation saga for BookingId: {BookingId}, CorrelationId: {CorrelationId}",
                bookingId, correlationId);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found");
            }

            var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
            if (payment == null)
            {
                return new Response<bool>(false, false, "Payment not found");
            }

            var initialData = new Dictionary<string, object>
            {
                { "BookingId", bookingId },
                { "PaymentId", payment.Id },
                { "CancelledBy", cancelledBy },
                { "IsLateCancellation", isLateCancellation },
                { "OriginalBookingStatus", booking.Status.ToString() },
                { "OriginalPaymentStatus", payment.Status.ToString() }
            };

            var saga = await _sagaOrchestrator.StartSagaAsync(
                "BookingCancellation",
                correlationId,
                initialData,
                "Booking",
                bookingId);

            var steps = new List<ISagaStep>
            {
                new CancelPendingTransfersStep(_pendingTransferRepository, _logger),
                new ProcessRefundStep(_paymentService, _unitOfWork, _logger),
                new UpdateBookingCancellationStatusStep(_unitOfWork, _logger)
            };

            var result = await _sagaOrchestrator.ExecuteSagaAsync(saga.Id, steps);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Booking cancellation saga completed successfully for BookingId: {BookingId}",
                    bookingId);
                return new Response<bool>(true, true, null);
            }
            else
            {
                _logger.LogError(
                    "Booking cancellation saga failed for BookingId: {BookingId}. Error: {Error}",
                    bookingId, result.ErrorMessage);
                return new Response<bool>(false, false, result.ErrorMessage);
            }
        }

        public async Task<TransactionStatusResponse> GetTransactionStatusAsync(string correlationId)
        {
            var saga = await _sagaOrchestrator.GetSagaByCorrelationIdAsync(correlationId);
            if (saga == null)
            {
                return new TransactionStatusResponse { Found = false };
            }

            return new TransactionStatusResponse
            {
                Found = true,
                CorrelationId = saga.CorrelationId,
                SagaType = saga.SagaType,
                Status = saga.Status.ToString(),
                CreatedAt = saga.CreatedAt,
                CompletedAt = saga.CompletedAt,
                ErrorMessage = saga.ErrorMessage,
                CurrentStep = saga.CurrentStepIndex,
                TotalSteps = saga.TotalSteps,
                Steps = saga.Steps?.Select(s => new StepStatus
                {
                    StepName = s.StepName,
                    Status = s.IsCompensated ? "Compensated" : s.IsExecuted ? "Executed" : "Pending",
                    ExecutedAt = s.ExecutedAt,
                    CompensatedAt = s.CompensatedAt,
                    ErrorMessage = s.ErrorMessage
                }).ToList()
            };
        }

        public async Task<Response<bool>> RetryTransactionAsync(string correlationId)
        {
            var saga = await _sagaOrchestrator.GetSagaByCorrelationIdAsync(correlationId);
            if (saga == null)
            {
                return new Response<bool>(false, false, "Transaction not found");
            }

            if (saga.Status != SagaStatus.Failed && saga.Status != SagaStatus.Compensated)
            {
                return new Response<bool>(false, false, $"Cannot retry saga in status: {saga.Status}");
            }

            // Start new saga for retry
            var newCorrelationId = $"{correlationId}-retry-{Guid.NewGuid():N}";
            var initialData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(saga.SagaData ?? "{}") ?? new Dictionary<string, object>();
            initialData["RetryOf"] = correlationId;

            var newSaga = await _sagaOrchestrator.StartSagaAsync(
                saga.SagaType,
                newCorrelationId,
                initialData,
                saga.AggregateType,
                saga.AggregateId);

            return new Response<bool>(true, true, $"New saga started with correlation ID: {newCorrelationId}");
        }

        public async Task<TransferStatistics> GetTransferStatisticsAsync()
        {
            var counts = await _pendingTransferRepository.GetTransferCountByStatusAsync();
            var pendingAmount = await _pendingTransferRepository.GetTotalPendingAmountAsync();

            return new TransferStatistics
            {
                PendingCount = counts.GetValueOrDefault(TransferStatus.Pending, 0),
                ProcessingCount = counts.GetValueOrDefault(TransferStatus.Processing, 0),
                CompletedCount = counts.GetValueOrDefault(TransferStatus.Completed, 0),
                FailedCount = counts.GetValueOrDefault(TransferStatus.Failed, 0),
                PermanentlyFailedCount = counts.GetValueOrDefault(TransferStatus.PermanentlyFailed, 0),
                TotalPendingAmount = pendingAmount
            };
        }
    }
}
