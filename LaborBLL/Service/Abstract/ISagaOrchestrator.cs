
using LaborDAL.Entities;
using LaborDAL.Enums;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Defines a Saga step that can be executed and compensated
    /// </summary>
    public interface ISagaStep
    {
        string StepName { get; }
        Task<object?> ExecuteAsync(SagaContext context);
        Task CompensateAsync(SagaContext context, object? executionResult);
    }

    /// <summary>
    /// Context passed to saga steps during execution
    /// </summary>
    public class SagaContext
    {
        public Guid SagaId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public CancellationToken CancellationToken { get; set; }
        public IServiceProvider ServiceProvider { get; set; } = null!;

        public T Get<T>(string key)
        {
            return (T)Data[key];
        }

        public void Set<T>(string key, T value)
        {
            Data[key] = value!;
        }

        public bool ContainsKey(string key)
        {
            return Data.ContainsKey(key);
        }
    }

    /// <summary>
    /// Saga orchestrator interface for managing distributed transactions
    /// Implements the Saga pattern for long-running transactions
    /// </summary>
    public interface ISagaOrchestrator
    {
        /// <summary>
        /// Start a new saga
        /// </summary>
        Task<SagaInstance> StartSagaAsync(string sagaType, string correlationId, Dictionary<string, object> initialData,
            string? aggregateType = null, int? aggregateId = null);

        /// <summary>
        /// Execute a saga with the given steps
        /// </summary>
        Task<SagaResult> ExecuteSagaAsync(Guid sagaId, IEnumerable<ISagaStep> steps, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compensate a failed saga
        /// </summary>
        Task<SagaResult> CompensateSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get saga status
        /// </summary>
        Task<SagaInstance?> GetSagaAsync(Guid sagaId);

        /// <summary>
        /// Get saga by correlation ID
        /// </summary>
        Task<SagaInstance?> GetSagaByCorrelationIdAsync(string correlationId);

        /// <summary>
        /// Recover stuck sagas
        /// </summary>
        Task<int> RecoverStuckSagasAsync(TimeSpan timeout);
    }

    /// <summary>
    /// Result of a saga execution
    /// </summary>
    public class SagaResult
    {
        public bool Success { get; set; }
        public SagaStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid SagaId { get; set; }
        public Dictionary<string, object>? Data { get; set; }

        public static SagaResult Succeeded(Guid sagaId, Dictionary<string, object>? data = null)
        {
            return new SagaResult
            {
                Success = true,
                Status = SagaStatus.Completed,
                SagaId = sagaId,
                Data = data
            };
        }

        public static SagaResult Failed(Guid sagaId, string errorMessage, SagaStatus status = SagaStatus.Failed)
        {
            return new SagaResult
            {
                Success = false,
                Status = status,
                ErrorMessage = errorMessage,
                SagaId = sagaId
            };
        }
    }
}
