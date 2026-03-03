
using LaborDAL.Entities;
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository interface for OutboxMessage operations
    /// </summary>
    public interface IOutboxMessageRepository : IRepository<OutboxMessage>
    {
        /// <summary>
        /// Get pending messages ready for processing
        /// </summary>
        Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100);

        /// <summary>
        /// Get messages by status
        /// </summary>
        Task<IEnumerable<OutboxMessage>> GetMessagesByStatusAsync(OutboxMessageStatus status, int take = 100);

        /// <summary>
        /// Get messages by aggregate type and id
        /// </summary>
        Task<IEnumerable<OutboxMessage>> GetMessagesByAggregateAsync(string aggregateType, int aggregateId);

        /// <summary>
        /// Acquire lock on a message for processing
        /// </summary>
        Task<bool> AcquireLockAsync(Guid messageId, string lockToken, TimeSpan lockDuration);

        /// <summary>
        /// Release lock on a message
        /// </summary>
        Task<bool> ReleaseLockAsync(Guid messageId, string lockToken);

        /// <summary>
        /// Mark message as completed
        /// </summary>
        Task<bool> MarkCompletedAsync(Guid messageId, string lockToken);

        /// <summary>
        /// Mark message as failed with error
        /// </summary>
        Task<bool> MarkFailedAsync(Guid messageId, string errorMessage, string? stackTrace = null);

        /// <summary>
        /// Move message to dead letter queue
        /// </summary>
        Task<bool> MoveToDeadLetterAsync(Guid messageId, string reason);

        /// <summary>
        /// Get dead letter messages
        /// </summary>
        Task<IEnumerable<OutboxMessage>> GetDeadLetterMessagesAsync(int take = 100);

        /// <summary>
        /// Get message count by status
        /// </summary>
        Task<Dictionary<OutboxMessageStatus, int>> GetMessageCountByStatusAsync();
    }
}
