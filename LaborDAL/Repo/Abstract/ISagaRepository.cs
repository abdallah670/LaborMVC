
using LaborDAL.Entities;
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    /// <summary>
    /// Repository interface for SagaInstance operations
    /// </summary>
    public interface ISagaRepository : IRepository<SagaInstance>
    {
        /// <summary>
        /// Get saga by correlation ID
        /// </summary>
        Task<SagaInstance?> GetByCorrelationIdAsync(string correlationId);

        /// <summary>
        /// Get sagas by status
        /// </summary>
        Task<IEnumerable<SagaInstance>> GetByStatusAsync(SagaStatus status, int take = 100);

        /// <summary>
        /// Get sagas by aggregate type and ID
        /// </summary>
        Task<IEnumerable<SagaInstance>> GetByAggregateAsync(string aggregateType, int aggregateId);

        /// <summary>
        /// Get sagas that need recovery (failed or stuck)
        /// </summary>
        Task<IEnumerable<SagaInstance>> GetSagasNeedingRecoveryAsync(TimeSpan timeout);

        /// <summary>
        /// Acquire lock on a saga for processing
        /// </summary>
        Task<bool> AcquireLockAsync(Guid sagaId, string lockToken, TimeSpan lockDuration);

        /// <summary>
        /// Release lock on a saga
        /// </summary>
        Task<bool> ReleaseLockAsync(Guid sagaId, string lockToken);

        /// <summary>
        /// Add a step to a saga
        /// </summary>
        Task<SagaStep> AddStepAsync(Guid sagaId, SagaStep step);

        /// <summary>
        /// Mark step as executed
        /// </summary>
        Task<bool> MarkStepExecutedAsync(Guid stepId, object? result = null);

        /// <summary>
        /// Mark step as compensated
        /// </summary>
        Task<bool> MarkStepCompensatedAsync(Guid stepId);

        /// <summary>
        /// Update saga status
        /// </summary>
        Task<bool> UpdateStatusAsync(Guid sagaId, SagaStatus status, string? errorMessage = null);

        /// <summary>
        /// Get saga with steps included
        /// </summary>
        Task<SagaInstance?> GetWithStepsAsync(Guid sagaId);
    }
}
