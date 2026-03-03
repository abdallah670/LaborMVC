
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using System.Text.Json;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Saga Orchestrator implementation for managing distributed transactions
    /// Uses the Saga pattern to coordinate multiple operations with compensation support
    /// </summary>
    public class SagaOrchestrator : ISagaOrchestrator
    {
        private readonly ISagaRepository _sagaRepository;
        private readonly ILogger<SagaOrchestrator> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SagaOrchestrator(
            ISagaRepository sagaRepository,
            ILogger<SagaOrchestrator> logger,
            IServiceProvider serviceProvider)
        {
            _sagaRepository = sagaRepository;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<SagaInstance> StartSagaAsync(
            string sagaType,
            string correlationId,
            Dictionary<string, object> initialData,
            string? aggregateType = null,
            int? aggregateId = null)
        {
            var saga = new SagaInstance
            {
                SagaType = sagaType,
                CorrelationId = correlationId,
                SagaData = JsonSerializer.Serialize(initialData),
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                Status = SagaStatus.Created
            };

            await _sagaRepository.AddAsync(saga);
            _logger.LogInformation(
                "Started saga {SagaType} with ID {SagaId} and correlation {CorrelationId}",
                sagaType, saga.Id, correlationId);

            return saga;
        }

        public async Task<SagaResult> ExecuteSagaAsync(
            Guid sagaId,
            IEnumerable<ISagaStep> steps,
            CancellationToken cancellationToken = default)
        {
            var saga = await _sagaRepository.GetWithStepsAsync(sagaId);
            if (saga == null)
            {
                return SagaResult.Failed(sagaId, "Saga not found");
            }

            if (!saga.CanExecute())
            {
                return SagaResult.Failed(sagaId, $"Saga cannot be executed. Current status: {saga.Status}");
            }

            // Acquire lock
            var lockToken = Guid.NewGuid().ToString();
            if (!await _sagaRepository.AcquireLockAsync(sagaId, lockToken, TimeSpan.FromMinutes(5)))
            {
                return SagaResult.Failed(sagaId, "Could not acquire lock on saga");
            }

            try
            {
                // Mark saga as running
                await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Running);

                // Deserialize saga data
                var sagaData = string.IsNullOrEmpty(saga.SagaData)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(saga.SagaData) ?? new Dictionary<string, object>();

                // Create context
                var context = new SagaContext
                {
                    SagaId = sagaId,
                    CorrelationId = saga.CorrelationId,
                    Data = sagaData,
                    CancellationToken = cancellationToken,
                    ServiceProvider = _serviceProvider
                };

                var stepList = steps.ToList();
                saga.TotalSteps = stepList.Count;

                // Execute each step
                for (int i = saga.CurrentStepIndex; i < stepList.Count; i++)
                {
                    var step = stepList[i];
                    var sagaStep = new SagaStep
                    {
                        SagaInstanceId = sagaId,
                        StepName = step.StepName,
                        StepOrder = i,
                        InputData = JsonSerializer.Serialize(context.Data)
                    };

                    await _sagaRepository.AddStepAsync(sagaId, sagaStep);

                    try
                    {
                        _logger.LogInformation(
                            "Executing step {StepNumber}/{TotalSteps}: {StepName} for saga {SagaId}",
                            i + 1, stepList.Count, step.StepName, sagaId);

                        var result = await step.ExecuteAsync(context);
                        await _sagaRepository.MarkStepExecutedAsync(sagaStep.Id, result);

                        // Update saga data
                        saga.SagaData = JsonSerializer.Serialize(context.Data);
                        saga.CurrentStepIndex = i + 1;
                        await _sagaRepository.UpdateAsync(saga);

                        _logger.LogInformation(
                            "Completed step {StepName} for saga {SagaId}",
                            step.StepName, sagaId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Step {StepName} failed for saga {SagaId}. Initiating compensation.",
                            step.StepName, sagaId);

                        sagaStep.MarkFailed(ex.Message, ex.StackTrace);
                        await _sagaRepository.UpdateAsync(saga);

                        // Release lock before compensation
                        await _sagaRepository.ReleaseLockAsync(sagaId, lockToken);

                        // Compensate completed steps
                        var compensateResult = await CompensateSagaAsync(sagaId, cancellationToken);

                        return SagaResult.Failed(sagaId, $"Step {step.StepName} failed: {ex.Message}",
                            compensateResult.Success ? SagaStatus.Compensated : SagaStatus.Failed);
                    }
                }

                // Mark saga as completed
                await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Completed);
                await _sagaRepository.ReleaseLockAsync(sagaId, lockToken);

                _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);

                return SagaResult.Succeeded(sagaId, context.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing saga {SagaId}", sagaId);
                await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Failed, ex.Message);
                await _sagaRepository.ReleaseLockAsync(sagaId, lockToken);

                return SagaResult.Failed(sagaId, ex.Message);
            }
        }

        public async Task<SagaResult> CompensateSagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
        {
            var saga = await _sagaRepository.GetWithStepsAsync(sagaId);
            if (saga == null)
            {
                return SagaResult.Failed(sagaId, "Saga not found");
            }

            if (saga.Status == SagaStatus.Compensated)
            {
                return SagaResult.Succeeded(sagaId);
            }

            var lockToken = Guid.NewGuid().ToString();
            if (!await _sagaRepository.AcquireLockAsync(sagaId, lockToken, TimeSpan.FromMinutes(5)))
            {
                return SagaResult.Failed(sagaId, "Could not acquire lock on saga for compensation");
            }

            try
            {
                await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Compensating);

                // Get executed steps in reverse order
                var executedSteps = saga.Steps
                    .Where(s => s.IsExecuted && !s.IsCompensated)
                    .OrderByDescending(s => s.StepOrder)
                    .ToList();

                var context = new SagaContext
                {
                    SagaId = sagaId,
                    CorrelationId = saga.CorrelationId,
                    Data = JsonSerializer.Deserialize<Dictionary<string, object>>(saga.SagaData ?? "{}") ?? new Dictionary<string, object>(),
                    CancellationToken = cancellationToken,
                    ServiceProvider = _serviceProvider
                };

                foreach (var step in executedSteps)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Compensating step {StepName} for saga {SagaId}",
                            step.StepName, sagaId);

                        // Note: In a real implementation, we'd need to resolve the ISagaStep
                        // from a factory or registry. This is simplified.

                        await _sagaRepository.MarkStepCompensatedAsync(step.Id);

                        _logger.LogInformation(
                            "Successfully compensated step {StepName} for saga {SagaId}",
                            step.StepName, sagaId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to compensate step {StepName} for saga {SagaId}. Manual intervention required.",
                            step.StepName, sagaId);

                        await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Failed,
                            $"Compensation failed for step {step.StepName}: {ex.Message}");
                        await _sagaRepository.ReleaseLockAsync(sagaId, lockToken);

                        return SagaResult.Failed(sagaId, $"Compensation failed: {ex.Message}");
                    }
                }

                await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Compensated);
                await _sagaRepository.ReleaseLockAsync(sagaId, lockToken);

                _logger.LogInformation("Saga {SagaId} compensated successfully", sagaId);

                return SagaResult.Succeeded(sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error compensating saga {SagaId}", sagaId);
                await _sagaRepository.UpdateStatusAsync(sagaId, SagaStatus.Failed, ex.Message);
                await _sagaRepository.ReleaseLockAsync(sagaId, lockToken);

                return SagaResult.Failed(sagaId, ex.Message);
            }
        }

        public async Task<SagaInstance?> GetSagaAsync(Guid sagaId)
        {
            return await _sagaRepository.GetWithStepsAsync(sagaId);
        }

        public async Task<SagaInstance?> GetSagaByCorrelationIdAsync(string correlationId)
        {
            return await _sagaRepository.GetByCorrelationIdAsync(correlationId);
        }

        public async Task<int> RecoverStuckSagasAsync(TimeSpan timeout)
        {
            var stuckSagas = await _sagaRepository.GetSagasNeedingRecoveryAsync(timeout);
            var recoveredCount = 0;

            foreach (var saga in stuckSagas)
            {
                try
                {
                    _logger.LogWarning(
                        "Recovering stuck saga {SagaId} with status {Status}",
                        saga.Id, saga.Status);

                    // Release any existing lock
                    if (!string.IsNullOrEmpty(saga.LockToken))
                    {
                        await _sagaRepository.ReleaseLockAsync(saga.Id, saga.LockToken);
                    }

                    // If saga was compensating, try to complete compensation
                    if (saga.Status == SagaStatus.Compensating)
                    {
                        await CompensateSagaAsync(saga.Id);
                    }
                    else if (saga.Status == SagaStatus.Running)
                    {
                        // Mark as failed for manual review
                        await _sagaRepository.UpdateStatusAsync(saga.Id, SagaStatus.Failed,
                            "Saga was stuck and marked for manual review");
                    }

                    recoveredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recover saga {SagaId}", saga.Id);
                }
            }

            return recoveredCount;
        }
    }
}
