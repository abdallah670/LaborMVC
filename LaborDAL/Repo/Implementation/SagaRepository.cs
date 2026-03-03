
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for SagaInstance
    /// </summary>
    public class SagaRepository : Repository<SagaInstance>, ISagaRepository
    {
        private readonly ApplicationDbContext _context;

        public SagaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SagaInstance?> GetByCorrelationIdAsync(string correlationId)
        {
            return await _context.Set<SagaInstance>()
                .Include(s => s.Steps)
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);
        }

        public async Task<IEnumerable<SagaInstance>> GetByStatusAsync(SagaStatus status, int take = 100)
        {
            return await _context.Set<SagaInstance>()
                .Include(s => s.Steps)
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<SagaInstance>> GetByAggregateAsync(string aggregateType, int aggregateId)
        {
            return await _context.Set<SagaInstance>()
                .Include(s => s.Steps)
                .Where(s => s.AggregateType == aggregateType && s.AggregateId == aggregateId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SagaInstance>> GetSagasNeedingRecoveryAsync(TimeSpan timeout)
        {
            var cutoffTime = DateTime.UtcNow.Add(-timeout);
            return await _context.Set<SagaInstance>()
                .Include(s => s.Steps)
                .Where(s => (s.Status == SagaStatus.Running || s.Status == SagaStatus.Compensating) &&
                           (s.LockExpiryAt == null || s.LockExpiryAt < DateTime.UtcNow) &&
                           s.CreatedAt < cutoffTime)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AcquireLockAsync(Guid sagaId, string lockToken, TimeSpan lockDuration)
        {
            var saga = await _context.Set<SagaInstance>().FindAsync(sagaId);
            if (saga == null || saga.IsLocked())
                return false;

            saga.LockToken = lockToken;
            saga.LockExpiryAt = DateTime.UtcNow.Add(lockDuration);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseLockAsync(Guid sagaId, string lockToken)
        {
            var saga = await _context.Set<SagaInstance>().FindAsync(sagaId);
            if (saga == null || saga.LockToken != lockToken)
                return false;

            saga.LockToken = null;
            saga.LockExpiryAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SagaStep> AddStepAsync(Guid sagaId, SagaStep step)
        {
            step.SagaInstanceId = sagaId;
            _context.Set<SagaStep>().Add(step);
            await _context.SaveChangesAsync();
            return step;
        }

        public async Task<bool> MarkStepExecutedAsync(Guid stepId, object? result = null)
        {
            var step = await _context.Set<SagaStep>().FindAsync(stepId);
            if (step == null)
                return false;

            step.MarkExecuted(result);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkStepCompensatedAsync(Guid stepId)
        {
            var step = await _context.Set<SagaStep>().FindAsync(stepId);
            if (step == null)
                return false;

            step.MarkCompensated();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(Guid sagaId, SagaStatus status, string? errorMessage = null)
        {
            var saga = await _context.Set<SagaInstance>().FindAsync(sagaId);
            if (saga == null)
                return false;

            saga.Status = status;
            
            switch (status)
            {
                case SagaStatus.Running:
                    if (saga.StartedAt == null)
                        saga.MarkStarted();
                    break;
                case SagaStatus.Completed:
                    saga.MarkCompleted();
                    break;
                case SagaStatus.Compensating:
                    saga.MarkCompensating();
                    break;
                case SagaStatus.Compensated:
                    saga.MarkCompensated();
                    break;
                case SagaStatus.Failed:
                    saga.MarkFailed(errorMessage ?? "Unknown error");
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SagaInstance?> GetWithStepsAsync(Guid sagaId)
        {
            return await _context.Set<SagaInstance>()
                .Include(s => s.Steps.OrderBy(step => step.StepOrder))
                .FirstOrDefaultAsync(s => s.Id == sagaId);
        }
    }
}
