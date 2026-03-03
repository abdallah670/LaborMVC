
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using System.Text.Json;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Background job for processing outbox messages
    /// Implements the Outbox pattern for reliable message delivery
    /// </summary>
    public interface IOutboxProcessorJob
    {
        Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default);
        Task ProcessDeadLetterMessagesAsync(CancellationToken cancellationToken = default);
    }

    public class OutboxProcessorJob : IOutboxProcessorJob
    {
        private readonly IOutboxMessageRepository _outboxRepository;
        private readonly ILogger<OutboxProcessorJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Lock duration for message processing
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);

        public OutboxProcessorJob(
            IOutboxMessageRepository outboxRepository,
            ILogger<OutboxProcessorJob> logger,
            IServiceProvider serviceProvider)
        {
            _outboxRepository = outboxRepository;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Process pending outbox messages
        /// </summary>
        public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting outbox message processing");

                var messages = await _outboxRepository.GetPendingMessagesAsync(batchSize: 50);
                var processedCount = 0;
                var failedCount = 0;

                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var lockToken = Guid.NewGuid().ToString();
                    
                    try
                    {
                        // Try to acquire lock
                        if (!await _outboxRepository.AcquireLockAsync(message.Id, lockToken, LockDuration))
                        {
                            _logger.LogDebug("Could not acquire lock on message {MessageId}", message.Id);
                            continue;
                        }

                        // Process the message
                        var success = await ProcessMessageAsync(message, cancellationToken);

                        if (success)
                        {
                            await _outboxRepository.MarkCompletedAsync(message.Id, lockToken);
                            processedCount++;
                            _logger.LogInformation("Successfully processed outbox message {MessageId} of type {MessageType}",
                                message.Id, message.MessageType);
                        }
                        else
                        {
                            await _outboxRepository.MarkFailedAsync(message.Id, "Processing returned false", null);
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                        await _outboxRepository.MarkFailedAsync(message.Id, ex.Message, ex.StackTrace);
                        failedCount++;
                    }
                }

                if (processedCount > 0 || failedCount > 0)
                {
                    _logger.LogInformation(
                        "Outbox processing complete. Processed: {Processed}, Failed: {Failed}",
                        processedCount, failedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox message processing batch");
            }
        }

        /// <summary>
        /// Process a single outbox message
        /// </summary>
        private async Task<bool> ProcessMessageAsync(LaborDAL.Entities.OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing message {MessageId} of type {MessageType}",
                    message.Id, message.MessageType);

                // Route to appropriate handler based on message type
                switch (message.MessageType)
                {
                    case "PaymentCaptured":
                        return await HandlePaymentCapturedAsync(message, cancellationToken);

                    case "TransferCreated":
                        return await HandleTransferCreatedAsync(message, cancellationToken);

                    case "BookingCompleted":
                        return await HandleBookingCompletedAsync(message, cancellationToken);

                    case "PaymentReleased":
                        return await HandlePaymentReleasedAsync(message, cancellationToken);

                    case "RefundProcessed":
                        return await HandleRefundProcessedAsync(message, cancellationToken);

                    default:
                        _logger.LogWarning("Unknown message type: {MessageType}", message.MessageType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", message.Id);
                return false;
            }
        }

        /// <summary>
        /// Handle PaymentCaptured message - typically send notification
        /// </summary>
        private async Task<bool> HandlePaymentCapturedAsync(LaborDAL.Entities.OutboxMessage message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            if (payload == null) return false;

            _logger.LogInformation("Payment captured event processed for PaymentId: {PaymentId}",
                payload.GetValueOrDefault("PaymentId"));

            // Here you would:
            // 1. Send notification to worker
            // 2. Update analytics
            // 3. Trigger other downstream processes

            return true;
        }

        /// <summary>
        /// Handle TransferCreated message
        /// </summary>
        private async Task<bool> HandleTransferCreatedAsync(LaborDAL.Entities.OutboxMessage message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            if (payload == null) return false;

            _logger.LogInformation("Transfer created event processed for TransferId: {TransferId}",
                payload.GetValueOrDefault("TransferId"));

            // Could trigger notification to worker about incoming transfer
            return true;
        }

        /// <summary>
        /// Handle BookingCompleted message
        /// </summary>
        private async Task<bool> HandleBookingCompletedAsync(LaborDAL.Entities.OutboxMessage message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            if (payload == null) return false;

            _logger.LogInformation("Booking completed event processed for BookingId: {BookingId}",
                payload.GetValueOrDefault("BookingId"));

            // Could trigger review requests, notifications, etc.
            return true;
        }

        /// <summary>
        /// Handle PaymentReleased message
        /// </summary>
        private async Task<bool> HandlePaymentReleasedAsync(LaborDAL.Entities.OutboxMessage message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            if (payload == null) return false;

            _logger.LogInformation("Payment released event processed for PaymentId: {PaymentId}",
                payload.GetValueOrDefault("PaymentId"));

            return true;
        }

        /// <summary>
        /// Handle RefundProcessed message
        /// </summary>
        private async Task<bool> HandleRefundProcessedAsync(LaborDAL.Entities.OutboxMessage message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            if (payload == null) return false;

            _logger.LogInformation("Refund processed event processed for PaymentId: {PaymentId}",
                payload.GetValueOrDefault("PaymentId"));

            return true;
        }

        /// <summary>
        /// Process dead letter messages - typically for alerting/monitoring
        /// </summary>
        public async Task ProcessDeadLetterMessagesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var deadLetters = await _outboxRepository.GetDeadLetterMessagesAsync(take: 100);
                
                foreach (var message in deadLetters)
                {
                    _logger.LogError(
                        "Dead letter message detected: {MessageId}, Type: {MessageType}, Error: {Error}",
                        message.Id, message.MessageType, message.ErrorMessage);

                    // Here you could:
                    // 1. Send alert to monitoring system
                    // 2. Create support ticket
                    // 3. Trigger manual review workflow
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dead letter messages");
            }
        }
    }
}
