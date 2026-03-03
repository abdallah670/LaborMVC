
using LaborBLL.Service.Abstract;
using LaborDAL.DB;
using LaborDAL.Enums;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Implementation of compensation operations for distributed transactions
    /// All compensation methods are idempotent to handle partial failures
    /// </summary>
    public class CompensationService : ICompensationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CompensationService> _logger;
        private readonly IPaymentService _paymentService;

        public CompensationService(
            ApplicationDbContext dbContext,
            ILogger<CompensationService> logger,
            IPaymentService paymentService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _paymentService = paymentService;
        }

        public async Task<CompensationResult> CompensatePaymentCaptureAsync(int paymentId, string reason)
        {
            try
            {
                _logger.LogInformation("Compensating payment capture for PaymentId: {PaymentId}. Reason: {Reason}",
                    paymentId, reason);

                var payment = await _dbContext.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment {PaymentId} not found. Compensation not needed.", paymentId);
                    return CompensationResult.Succeeded();
                }

                // If payment is already refunded, nothing to do
                if (payment.Status == PaymentStatus.Refunded || payment.Status == PaymentStatus.PartiallyRefunded)
                {
                    _logger.LogInformation("Payment {PaymentId} is already refunded. Skipping compensation.", paymentId);
                    return CompensationResult.Succeeded();
                }

                // If payment was captured, we need to refund it
                if (payment.Status == PaymentStatus.Released)
                {
                    var result = await _paymentService.RefundPaymentAsync(paymentId);
                    if (result.Success)
                    {
                        _logger.LogInformation("Successfully refunded payment {PaymentId} as compensation", paymentId);
                        return CompensationResult.Succeeded(payment.TransactionId);
                    }
                    else
                    {
                        _logger.LogError("Failed to refund payment {PaymentId}: {Error}",
                            paymentId, result.ErrorMessage);
                        return CompensationResult.Failed(result.ErrorMessage ?? "Refund failed", true);
                    }
                }

                // Payment is still pending, cancel it
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.Notes = $"{payment.Notes}\nCancelled as compensation. Reason: {reason}";
                    await _dbContext.SaveChangesAsync();
                    return CompensationResult.Succeeded();
                }

                return CompensationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compensating payment capture for PaymentId: {PaymentId}", paymentId);
                return CompensationResult.Failed(ex.Message, true);
            }
        }

        public async Task<CompensationResult> CompensateWorkerTransferAsync(string stripeTransferId, string reason)
        {
            try
            {
                _logger.LogInformation("Compensating worker transfer {TransferId}. Reason: {Reason}",
                    stripeTransferId, reason);

                if (string.IsNullOrEmpty(stripeTransferId))
                {
                    return CompensationResult.Succeeded();
                }

                // Create a reversal transfer
                var reversalOptions = new TransferReversalCreateOptions
                {
                    RefundApplicationFee = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "reason", reason },
                        { "original_transfer", stripeTransferId },
                        { "compensated_at", DateTime.UtcNow.ToString("O") }
                    }
                };

                var reversalService = new TransferReversalService();
                var reversal = await reversalService.CreateAsync(stripeTransferId, reversalOptions);

                _logger.LogInformation("Created reversal transfer {ReversalId} for {OriginalTransferId}",
                    reversal.Id, stripeTransferId);

                return CompensationResult.Succeeded(reversal.Id);
            }
            catch (StripeException ex) when (ex.Message.Contains("already reversed"))
            {
                // Transfer is already reversed, which is fine
                _logger.LogInformation("Transfer {TransferId} is already reversed", stripeTransferId);
                return CompensationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compensating worker transfer {TransferId}", stripeTransferId);
                return CompensationResult.Failed(ex.Message, true);
            }
        }

        public async Task<CompensationResult> CompensateBookingStatusChangeAsync(int bookingId, string previousStatus, string reason)
        {
            try
            {
                _logger.LogInformation("Compensating booking status change for BookingId: {BookingId}. Reason: {Reason}",
                    bookingId, reason);

                var booking = await _dbContext.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found", bookingId);
                    return CompensationResult.Succeeded();
                }

                // Revert to previous status
                if (Enum.TryParse<BookingStatus>(previousStatus, out var status))
                {
                    booking.Status = status;
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Reverted booking {BookingId} status to {Status}",
                        bookingId, previousStatus);
                }

                return CompensationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compensating booking status change for BookingId: {BookingId}", bookingId);
                return CompensationResult.Failed(ex.Message, false);
            }
        }

        public async Task<CompensationResult> CompensatePaymentHoldAsync(int paymentId, string reason)
        {
            try
            {
                _logger.LogInformation("Compensating payment hold for PaymentId: {PaymentId}. Reason: {Reason}",
                    paymentId, reason);

                var payment = await _dbContext.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    return CompensationResult.Succeeded();
                }

                // If payment is still pending/held, cancel it
                if (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Held)
                {
                    // Cancel the Stripe PaymentIntent
                    try
                    {
                        var cancellationOptions = new PaymentIntentCancelOptions
                        {
                            CancellationReason = "abandoned"
                        };
                        var paymentIntentService = new PaymentIntentService();
                        await paymentIntentService.CancelAsync(payment.TransactionId, cancellationOptions);
                    }
                    catch (StripeException ex) when (ex.Message.Contains("already canceled") ||
                                                    ex.Message.Contains("succeeded") ||
                                                    ex.Message.Contains("requires_capture"))
                    {
                        // PaymentIntent already in final state, nothing to do
                    }

                    payment.Status = PaymentStatus.Failed;
                    payment.Notes = $"{payment.Notes}\nHold cancelled as compensation. Reason: {reason}";
                    await _dbContext.SaveChangesAsync();
                }

                return CompensationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compensating payment hold for PaymentId: {PaymentId}", paymentId);
                return CompensationResult.Failed(ex.Message, false);
            }
        }

        public async Task<CompensationResult> CompensatePendingTransferAsync(Guid pendingTransferId, string reason)
        {
            try
            {
                _logger.LogInformation("Compensating pending transfer {TransferId}. Reason: {Reason}",
                    pendingTransferId, reason);

                var transfer = await _dbContext.Set<PendingTransfer>().FindAsync(pendingTransferId);
                if (transfer == null)
                {
                    return CompensationResult.Succeeded();
                }

                // Cancel the pending transfer
                transfer.Status = TransferStatus.Cancelled;
                transfer.ErrorMessage = $"Cancelled as compensation. Reason: {reason}";
                await _dbContext.SaveChangesAsync();

                // If transfer was already completed, create a reversal
                if (!string.IsNullOrEmpty(transfer.StripeTransferId))
                {
                    return await CompensateWorkerTransferAsync(transfer.StripeTransferId, reason);
                }

                return CompensationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compensating pending transfer {TransferId}", pendingTransferId);
                return CompensationResult.Failed(ex.Message, true);
            }
        }
    }
}
