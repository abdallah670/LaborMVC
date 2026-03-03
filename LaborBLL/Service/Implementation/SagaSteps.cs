
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;

namespace LaborBLL.Service.Implementation
{
    #region Payment Release Saga Steps

    /// <summary>
    /// Step 1: Capture the payment from the customer
    /// </summary>
    public class CapturePaymentStep : ISagaStep
    {
        public string StepName => "CapturePayment";

        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public CapturePaymentStep(IPaymentService paymentService, IUnitOfWork unitOfWork, ILogger logger)
        {
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var paymentId = context.Get<int>("PaymentId");
            
            _logger.LogInformation("Capturing payment {PaymentId} in saga {SagaId}", paymentId, context.SagaId);

            // Capture the payment using the existing payment service
            var bookingId = context.Get<int>("BookingId");
            var result = await _paymentService.CapturePaymentAsync(bookingId, null); // Transfer will be done separately

            if (!result.Success)
            {
                throw new Exception($"Payment capture failed: {result.ErrorMessage}");
            }

            // Get the updated payment to store the transfer group
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment != null)
            {
                // Store the transfer group in payment notes for reference
                var transferGroup = context.Get<string>("TransferGroup");
                payment.Notes = $"{payment.Notes}\nTransferGroup: {transferGroup}";
                await _unitOfWork.SaveAsync();
            }

            _logger.LogInformation("Successfully captured payment {PaymentId}", paymentId);
            return new { PaymentId = paymentId, Captured = true };
        }

        public async Task CompensateAsync(SagaContext context, object? executionResult)
        {
            var paymentId = context.Get<int>("PaymentId");
            _logger.LogWarning("Compensating payment capture for {PaymentId} in saga {SagaId}", 
                paymentId, context.SagaId);

            // Refund the captured payment
            var compensationService = context.ServiceProvider.GetRequiredService<ICompensationService>();
            await compensationService.CompensatePaymentCaptureAsync(paymentId, 
                $"Saga {context.SagaId} compensation");
        }
    }

    /// <summary>
    /// Step 2: Create a pending transfer to the worker
    /// </summary>
    public class CreatePendingTransferStep : ISagaStep
    {
        public string StepName => "CreatePendingTransfer";

        private readonly IPendingTransferRepository _pendingTransferRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public CreatePendingTransferStep(IPendingTransferRepository pendingTransferRepository, 
            IUnitOfWork unitOfWork, ILogger logger)
        {
            _pendingTransferRepository = pendingTransferRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var bookingId = context.Get<int>("BookingId");
            var paymentId = context.Get<int>("PaymentId");
            var workerStripeAccountId = context.Get<string>("WorkerStripeAccountId");
            var amount = context.Get<decimal>("Amount");
            var transferGroup = context.Get<string>("TransferGroup");

            _logger.LogInformation("Creating pending transfer for BookingId: {BookingId}, Worker: {WorkerId}",
                bookingId, workerStripeAccountId);

            // Calculate platform fee (10%)
            var platformFee = amount * 0.10m;
            var transferAmount = amount - platformFee;

            var pendingTransfer = new PendingTransfer
            {
                PaymentId = paymentId,
                BookingId = bookingId,
                WorkerStripeAccountId = workerStripeAccountId,
                Amount = transferAmount,
                Currency = "usd",
                TransferGroup = transferGroup,
                Description = $"Payment for booking #{bookingId}",
                PlatformFeeAmount = platformFee,
                Status = TransferStatus.Pending,
                MaxRetryCount = 5
            };

            await _pendingTransferRepository.AddAsync(pendingTransfer);

            _logger.LogInformation("Created pending transfer {TransferId} for booking {BookingId}",
                pendingTransfer.Id, bookingId);

            return new { PendingTransferId = pendingTransfer.Id, TransferAmount = transferAmount };
        }

        public async Task CompensateAsync(SagaContext context, object? executionResult)
        {
            if (executionResult == null) return;

            var resultDict = executionResult as Dictionary<string, object>;
            if (resultDict != null && resultDict.ContainsKey("PendingTransferId"))
            {
                var pendingTransferId = (Guid)resultDict["PendingTransferId"];
                _logger.LogWarning("Compensating pending transfer {TransferId} in saga {SagaId}",
                    pendingTransferId, context.SagaId);

                var compensationService = context.ServiceProvider.GetRequiredService<ICompensationService>();
                await compensationService.CompensatePendingTransferAsync(pendingTransferId,
                    $"Saga {context.SagaId} compensation");
            }
        }
    }

    /// <summary>
    /// Step 3: Update booking status to completed
    /// </summary>
    public class UpdateBookingStatusStep : ISagaStep
    {
        public string StepName => "UpdateBookingStatus";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public UpdateBookingStatusStep(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var bookingId = context.Get<int>("BookingId");
            
            _logger.LogInformation("Updating booking {BookingId} status to Completed", bookingId);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new Exception($"Booking {bookingId} not found");
            }

            // Store original status for compensation
            context.Set("OriginalBookingStatus", booking.Status.ToString());

            booking.Status = BookingStatus.Completed;
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Updated booking {BookingId} status to Completed", bookingId);
            return new { BookingId = bookingId, PreviousStatus = booking.Status.ToString() };
        }

        public async Task CompensateAsync(SagaContext context, object? executionResult)
        {
            var bookingId = context.Get<int>("BookingId");
            var originalStatus = context.ContainsKey("OriginalBookingStatus") 
                ? context.Get<string>("OriginalBookingStatus") 
                : "Confirmed";

            _logger.LogWarning("Compensating booking status for {BookingId} in saga {SagaId}",
                bookingId, context.SagaId);

            var compensationService = context.ServiceProvider.GetRequiredService<ICompensationService>();
            await compensationService.CompensateBookingStatusChangeAsync(bookingId, originalStatus,
                $"Saga {context.SagaId} compensation");
        }
    }

    #endregion

    #region Payment Hold Saga Steps

    /// <summary>
    /// Step: Create a payment intent to hold payment
    /// </summary>
    public class CreatePaymentIntentStep : ISagaStep
    {
        public string StepName => "CreatePaymentIntent";

        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public CreatePaymentIntentStep(IPaymentService paymentService, IUnitOfWork unitOfWork, ILogger logger)
        {
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var bookingId = context.Get<int>("BookingId");
            var amount = context.Get<decimal>("Amount");
            var userId = context.Get<string>("UserId");

            _logger.LogInformation("Creating payment intent for booking {BookingId}", bookingId);

            var payment = new PaymentVM
            {
                BookingId = bookingId,
                Amount = amount,
                UserId = userId,
                Status = PaymentStatus.Pending.ToString(),
                PaymentType = "Booking",
                Description = $"Payment for booking #{bookingId}",
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            var result = await _paymentService.CreateAsync(payment);
            if (!result.Success || result.Result == null)
            {
                throw new Exception($"Failed to create payment intent: {result.ErrorMessage}");
            }

            _logger.LogInformation("Created payment intent for booking {BookingId}, PaymentId: {PaymentId}",
                bookingId, result.Result.Id);

            context.Set("PaymentId", result.Result.Id);
            context.Set("ClientSecret", result.Result.ClientSecret);

            return new { PaymentId = result.Result.Id, ClientSecret = result.Result.ClientSecret };
        }

        public async Task CompensateAsync(SagaContext context, object? executionResult)
        {
            if (context.ContainsKey("PaymentId"))
            {
                var paymentId = context.Get<int>("PaymentId");
                _logger.LogWarning("Compensating payment hold for {PaymentId} in saga {SagaId}",
                    paymentId, context.SagaId);

                var compensationService = context.ServiceProvider.GetRequiredService<ICompensationService>();
                await compensationService.CompensatePaymentHoldAsync(paymentId,
                    $"Saga {context.SagaId} compensation");
            }
        }
    }

    #endregion

    #region Booking Cancellation Saga Steps

    /// <summary>
    /// Step 1: Cancel any pending transfers
    /// </summary>
    public class CancelPendingTransfersStep : ISagaStep
    {
        public string StepName => "CancelPendingTransfers";

        private readonly IPendingTransferRepository _pendingTransferRepository;
        private readonly ILogger _logger;

        public CancelPendingTransfersStep(IPendingTransferRepository pendingTransferRepository, ILogger logger)
        {
            _pendingTransferRepository = pendingTransferRepository;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var bookingId = context.Get<int>("BookingId");
            
            _logger.LogInformation("Cancelling pending transfers for booking {BookingId}", bookingId);

            var transfers = await _pendingTransferRepository.GetTransfersByBookingIdAsync(bookingId);
            var cancelledTransfers = new List<Guid>();

            foreach (var transfer in transfers.Where(t => 
                t.Status == TransferStatus.Pending || 
                t.Status == TransferStatus.Failed))
            {
                await _pendingTransferRepository.CancelTransferAsync(transfer.Id, 
                    "Booking cancelled");
                cancelledTransfers.Add(transfer.Id);
                _logger.LogInformation("Cancelled pending transfer {TransferId}", transfer.Id);
            }

            // If any transfers were already completed, we'll need to reverse them in the refund step
            var completedTransfers = transfers.Where(t => t.Status == TransferStatus.Completed).ToList();
            if (completedTransfers.Any())
            {
                context.Set("CompletedTransferIds", completedTransfers.Select(t => t.Id).ToList());
            }

            return new { CancelledTransferIds = cancelledTransfers };
        }

        public Task CompensateAsync(SagaContext context, object? executionResult)
        {
            // Nothing to compensate for cancellation
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Step 2: Process refund
    /// </summary>
    public class ProcessRefundStep : ISagaStep
    {
        public string StepName => "ProcessRefund";

        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public ProcessRefundStep(IPaymentService paymentService, IUnitOfWork unitOfWork, ILogger logger)
        {
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var paymentId = context.Get<int>("PaymentId");
            var isLateCancellation = context.Get<bool>("IsLateCancellation");

            _logger.LogInformation("Processing refund for payment {PaymentId}. Late cancellation: {IsLate}",
                paymentId, isLateCancellation);

            Response<bool> result;
            if (isLateCancellation)
            {
                // 50% refund for late cancellation
                var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    throw new Exception($"Payment {paymentId} not found");
                }
                var refundAmount = payment.Amount * 0.5m;
                result = await _paymentService.PartialRefundAsync(paymentId, refundAmount);
            }
            else
            {
                // Full refund
                result = await _paymentService.RefundPaymentAsync(paymentId);
            }

            if (!result.Success)
            {
                throw new Exception($"Refund failed: {result.ErrorMessage}");
            }

            _logger.LogInformation("Successfully processed refund for payment {PaymentId}", paymentId);
            return new { PaymentId = paymentId, Refunded = true, IsPartial = isLateCancellation };
        }

        public Task CompensateAsync(SagaContext context, object? executionResult)
        {
            // Refund compensation is tricky - we can't "un-refund"
            // This would require manual intervention
            _logger.LogError("Cannot compensate refund in saga {SagaId}. Manual intervention required.",
                context.SagaId);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Step 3: Update booking cancellation status
    /// </summary>
    public class UpdateBookingCancellationStatusStep : ISagaStep
    {
        public string StepName => "UpdateBookingCancellationStatus";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public UpdateBookingCancellationStatusStep(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<object?> ExecuteAsync(SagaContext context)
        {
            var bookingId = context.Get<int>("BookingId");
            var cancelledBy = context.Get<string>("CancelledBy");

            _logger.LogInformation("Updating booking {BookingId} to cancelled status", bookingId);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new Exception($"Booking {bookingId} not found");
            }

            context.Set("OriginalBookingStatusBeforeCancel", booking.Status.ToString());
            booking.Status = BookingStatus.Cancelled;
            // Note: You may need to add a CancelledBy, CancelledAt and UpdatedAt field to Booking entity

            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Updated booking {BookingId} to cancelled status", bookingId);
            return new { BookingId = bookingId, CancelledBy = cancelledBy };
        }

        public async Task CompensateAsync(SagaContext context, object? executionResult)
        {
            var bookingId = context.Get<int>("BookingId");
            var originalStatus = context.ContainsKey("OriginalBookingStatusBeforeCancel")
                ? context.Get<string>("OriginalBookingStatusBeforeCancel")
                : "Confirmed";

            _logger.LogWarning("Compensating booking cancellation for {BookingId} in saga {SagaId}",
                bookingId, context.SagaId);

            var compensationService = context.ServiceProvider.GetRequiredService<ICompensationService>();
            await compensationService.CompensateBookingStatusChangeAsync(bookingId, originalStatus,
                $"Saga {context.SagaId} compensation");
        }
    }

    #endregion
}
