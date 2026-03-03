
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Stripe;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Background job for processing pending transfers
    /// Implements retry mechanism with exponential backoff for Stripe transfers
    /// </summary>
    public interface ITransferProcessorJob
    {
        Task ProcessPendingTransfersAsync(CancellationToken cancellationToken = default);
        Task ProcessPermanentlyFailedTransfersAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<string, object>> GetTransferMetricsAsync();
    }

    public class TransferProcessorJob : ITransferProcessorJob
    {
        private readonly IPendingTransferRepository _transferRepository;
        private readonly ILogger<TransferProcessorJob> _logger;
        private readonly ICompensationService _compensationService;
        private readonly TransferService _stripeTransferService;

        // Lock duration for transfer processing
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(3);

        public TransferProcessorJob(
            IPendingTransferRepository transferRepository,
            ILogger<TransferProcessorJob> logger,
            ICompensationService compensationService)
        {
            _transferRepository = transferRepository;
            _logger = logger;
            _compensationService = compensationService;
            _stripeTransferService = new TransferService();
        }

        /// <summary>
        /// Process pending transfers
        /// </summary>
        public async Task ProcessPendingTransfersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting pending transfer processing");

                var transfers = await _transferRepository.GetPendingTransfersAsync(batchSize: 50);
                var processedCount = 0;
                var failedCount = 0;

                foreach (var transfer in transfers)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var lockToken = Guid.NewGuid().ToString();

                    try
                    {
                        // Try to acquire lock
                        if (!await _transferRepository.AcquireLockAsync(transfer.Id, lockToken, LockDuration))
                        {
                            _logger.LogDebug("Could not acquire lock on transfer {TransferId}", transfer.Id);
                            continue;
                        }

                        // Process the transfer
                        var success = await ProcessTransferAsync(transfer, lockToken, cancellationToken);

                        if (success)
                        {
                            processedCount++;
                        }
                        else
                        {
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing transfer {TransferId}", transfer.Id);
                        await _transferRepository.MarkFailedAsync(transfer.Id, ex.Message, lockToken);
                        failedCount++;
                    }
                }

                if (processedCount > 0 || failedCount > 0)
                {
                    _logger.LogInformation(
                        "Transfer processing complete. Processed: {Processed}, Failed: {Failed}",
                        processedCount, failedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pending transfer processing batch");
            }
        }

        /// <summary>
        /// Process a single transfer with Stripe
        /// </summary>
        private async Task<bool> ProcessTransferAsync(
            LaborDAL.Entities.PendingTransfer transfer,
            string lockToken,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Processing transfer {TransferId} for PaymentId: {PaymentId}, Amount: {Amount}, Worker: {WorkerId}",
                    transfer.Id, transfer.PaymentId, transfer.Amount, transfer.WorkerStripeAccountId);

                // Validate worker has Stripe account
                if (string.IsNullOrEmpty(transfer.WorkerStripeAccountId))
                {
                    _logger.LogError("Worker Stripe account ID is empty for transfer {TransferId}", transfer.Id);
                    await _transferRepository.MarkPermanentlyFailedAsync(
                        transfer.Id, "Worker Stripe account ID is empty", lockToken);
                    return false;
                }

                // Create Stripe transfer
                var transferOptions = new TransferCreateOptions
                {
                    Amount = (long)(transfer.Amount * 100), // Convert to cents
                    Currency = transfer.Currency,
                    Destination = transfer.WorkerStripeAccountId,
                    TransferGroup = transfer.TransferGroup,
                    Description = transfer.Description,
                    Metadata = new Dictionary<string, string>
                    {
                        { "pending_transfer_id", transfer.Id.ToString() },
                        { "payment_id", transfer.PaymentId.ToString() },
                        { "booking_id", transfer.BookingId.ToString() },
                        { "platform_fee", (transfer.PlatformFeeAmount * 100).ToString("F0") }
                    }
                };

                var stripeTransfer = await _stripeTransferService.CreateAsync(transferOptions);

                if (stripeTransfer == null || string.IsNullOrEmpty(stripeTransfer.Id))
                {
                    throw new Exception("Stripe transfer returned null or empty ID");
                }

                // Mark transfer as completed
                await _transferRepository.MarkCompletedAsync(transfer.Id, stripeTransfer.Id, lockToken);

                _logger.LogInformation(
                    "Successfully created Stripe transfer {StripeTransferId} for pending transfer {TransferId}",
                    stripeTransfer.Id, transfer.Id);

                return true;
            }
            catch (StripeException ex) when (ex.Message.Contains("insufficient funds"))
            {
                _logger.LogError(ex,
                    "Insufficient funds for transfer {TransferId}. Amount: {Amount}",
                    transfer.Id, transfer.Amount);

                // This is a critical error - we captured payment but don't have funds to transfer
                // This could indicate a timing issue or accounting error
                await _transferRepository.MarkFailedAsync(
                    transfer.Id,
                    $"Insufficient funds: {ex.Message}. This requires immediate investigation.",
                    lockToken);

                // Could trigger alert here
                return false;
            }
            catch (StripeException ex) when (ex.Message.Contains("destination"))
            {
                _logger.LogError(ex,
                    "Invalid destination account for transfer {TransferId}. WorkerId: {WorkerId}",
                    transfer.Id, transfer.WorkerStripeAccountId);

                // Worker's Stripe account may be invalid or no longer active
                await _transferRepository.MarkPermanentlyFailedAsync(
                    transfer.Id,
                    $"Invalid destination account: {ex.Message}. Worker needs to verify Stripe account.",
                    lockToken);

                return false;
            }
            catch (StripeException ex) when ((int)ex.HttpStatusCode == 429)
            {
                _logger.LogWarning(ex,
                    "Rate limited by Stripe for transfer {TransferId}. Will retry.",
                    transfer.Id);

                // Rate limit - will be retried with exponential backoff
                await _transferRepository.ScheduleRetryAsync(transfer.Id, lockToken);
                return false;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe error processing transfer {TransferId}: {Message}",
                    transfer.Id, ex.Message);

                await _transferRepository.MarkFailedAsync(
                    transfer.Id,
                    $"Stripe error: {ex.Message}",
                    lockToken);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing transfer {TransferId}", transfer.Id);

                await _transferRepository.MarkFailedAsync(
                    transfer.Id,
                    $"Unexpected error: {ex.Message}",
                    lockToken);
                return false;
            }
        }

        /// <summary>
        /// Handle permanently failed transfers
        /// </summary>
        public async Task ProcessPermanentlyFailedTransfersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var failedTransfers = await _transferRepository.GetTransfersByStatusAsync(
                    TransferStatus.PermanentlyFailed, take: 100);

                foreach (var transfer in failedTransfers)
                {
                    _logger.LogError(
                        "Permanently failed transfer detected: {TransferId}, PaymentId: {PaymentId}, Error: {Error}",
                        transfer.Id, transfer.PaymentId, transfer.ErrorMessage);

                    // Here you could:
                    // 1. Send alert to operations team
                    // 2. Create manual payment workflow
                    // 3. Notify the worker about the issue
                    // 4. Escalate to support ticket
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing permanently failed transfers");
            }
        }

        /// <summary>
        /// Get transfer metrics for monitoring
        /// </summary>
        public async Task<Dictionary<string, object>> GetTransferMetricsAsync()
        {
            var counts = await _transferRepository.GetTransferCountByStatusAsync();
            var pendingAmount = await _transferRepository.GetTotalPendingAmountAsync();

            return new Dictionary<string, object>
            {
                { "pending_count", counts.GetValueOrDefault(TransferStatus.Pending, 0) },
                { "processing_count", counts.GetValueOrDefault(TransferStatus.Processing, 0) },
                { "completed_count", counts.GetValueOrDefault(TransferStatus.Completed, 0) },
                { "failed_count", counts.GetValueOrDefault(TransferStatus.Failed, 0) },
                { "permanently_failed_count", counts.GetValueOrDefault(TransferStatus.PermanentlyFailed, 0) },
                { "cancelled_count", counts.GetValueOrDefault(TransferStatus.Cancelled, 0) },
                { "total_pending_amount_usd", pendingAmount }
            };
        }
    }
}
