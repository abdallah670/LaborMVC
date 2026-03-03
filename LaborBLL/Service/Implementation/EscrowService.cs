
using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Escrow service with distributed transaction support using Saga pattern
    /// Ensures atomic operations across payment capture and worker transfers
    /// </summary>
    public class EscrowService : IEscrowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly IDistributedTransactionService _distributedTransactionService;
        private readonly ILogger<EscrowService> _logger;

        public EscrowService(
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            IDistributedTransactionService distributedTransactionService,
            ILogger<EscrowService> logger)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _distributedTransactionService = distributedTransactionService;
            _logger = logger;
        }

        /// <summary>
        /// Hold payment for a booking using distributed transaction
        /// </summary>
        public async Task<Response<bool>> HoldPaymentAsync(int bookingId)
        {
            try
            {
                _logger.LogInformation("Initiating payment hold for BookingId: {BookingId}", bookingId);

                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new Response<bool>(false, false, "Booking not found.");
                }

                // Execute payment hold saga
                var result = await _distributedTransactionService.ExecutePaymentHoldAsync(bookingId);

                if (result.Success)
                {
                    booking.Worker.HasVisa = true;
                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation("Payment hold completed successfully for BookingId: {BookingId}", bookingId);
                }
                else
                {
                    _logger.LogError("Payment hold failed for BookingId: {BookingId}. Error: {Error}",
                        bookingId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error holding payment for BookingId: {BookingId}", bookingId);
                return new Response<bool>(false, false, $"Error holding payment: {ex.Message}");
            }
        }

        /// <summary>
        /// Release payment using distributed transaction with Saga pattern
        /// Ensures atomic payment capture and transfer to worker
        /// </summary>
        public async Task<Response<bool>> ReleasePaymentAsync(int bookingId)
        {
            try
            {
                _logger.LogInformation("Initiating payment release for BookingId: {BookingId}", bookingId);

                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new Response<bool>(false, false, "Booking not found.");
                }

                // Only allow release for completed bookings
                if (booking.Status != BookingStatus.Completed)
                {
                    return new Response<bool>(false, false,
                        "Booking must be completed by both parties before payment can be released.");
                }

                // Get worker's Stripe account ID
                string? workerStripeAccountId = null;
                if (booking.Worker != null)
                {
                    workerStripeAccountId = booking.Worker.StripeAccountId;
                }
                else
                {
                    var worker = await _unitOfWork.AppUsers.GetByIdAsync(booking.WorkerId);
                    workerStripeAccountId = worker?.StripeAccountId;
                }

                if (string.IsNullOrEmpty(workerStripeAccountId))
                {
                    return new Response<bool>(false, false,
                        "Worker does not have a connected Stripe account. Cannot process payment release.");
                }

                // Execute distributed transaction (Saga) for payment release
                var result = await _distributedTransactionService.ExecutePaymentReleaseAsync(
                    bookingId, workerStripeAccountId);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Payment release saga completed successfully for BookingId: {BookingId}",
                        bookingId);
                }
                else
                {
                    _logger.LogError(
                        "Payment release saga failed for BookingId: {BookingId}. Error: {Error}",
                        bookingId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error releasing payment for BookingId: {BookingId}", bookingId);
                return new Response<bool>(false, false, $"Error releasing payment: {ex.Message}");
            }
        }

        /// <summary>
        /// Process cancellation using distributed transaction with compensation
        /// </summary>
        public async Task<Response<bool>> ProcessCancellationAsync(int bookingId, string cancelledBy)
        {
            try
            {
                _logger.LogInformation("Initiating cancellation process for BookingId: {BookingId} by {CancelledBy}",
                    bookingId, cancelledBy);

                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new Response<bool>(false, false, "Booking not found.");
                }

                // Calculate if this is a late cancellation
                var hoursUntilStart = (booking.StartTime - DateTime.UtcNow)?.TotalHours ?? 0;
                bool isLateCancellation = (cancelledBy == booking.PosterId && hoursUntilStart < 2);

                // Execute cancellation saga
                var result = await _distributedTransactionService.ExecuteBookingCancellationAsync(
                    bookingId, cancelledBy, isLateCancellation);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Booking cancellation saga completed successfully for BookingId: {BookingId}",
                        bookingId);
                }
                else
                {
                    _logger.LogError(
                        "Booking cancellation saga failed for BookingId: {BookingId}. Error: {Error}",
                        bookingId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing cancellation for BookingId: {BookingId}", bookingId);
                return new Response<bool>(false, false, $"Error processing cancellation: {ex.Message}");
            }
        }

        /// <summary>
        /// Check the status of a payment release transaction
        /// </summary>
        public async Task<TransactionStatusResponse> GetPaymentReleaseStatusAsync(string correlationId)
        {
            return await _distributedTransactionService.GetTransactionStatusAsync(correlationId);
        }

        /// <summary>
        /// Retry a failed payment release
        /// </summary>
        public async Task<Response<bool>> RetryPaymentReleaseAsync(string correlationId)
        {
            return await _distributedTransactionService.RetryTransactionAsync(correlationId);
        }

        /// <summary>
        /// Get transfer statistics for monitoring
        /// </summary>
        public async Task<TransferStatistics> GetTransferStatisticsAsync()
        {
            return await _distributedTransactionService.GetTransferStatisticsAsync();
        }
    }
}
