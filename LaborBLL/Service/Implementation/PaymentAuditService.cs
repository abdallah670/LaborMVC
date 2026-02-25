using LaborDAL.DB;
using LaborDAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for logging payment status changes for audit trail
    /// </summary>
    public interface IPaymentAuditService
    {
        Task LogPaymentStatusChangeAsync(int paymentId, PaymentStatus oldStatus, PaymentStatus newStatus,
            string changedBy, string reason, string? transactionId = null, string? idempotencyKey = null,
            object? additionalData = null);

        Task<IEnumerable<PaymentAuditLog>> GetPaymentHistoryAsync(int paymentId);
        Task<IEnumerable<PaymentAuditLog>> GetRecentAuditLogsAsync(int count = 100);
    }

    public class PaymentAuditService : IPaymentAuditService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PaymentAuditService> _logger;

        public PaymentAuditService(ApplicationDbContext dbContext, ILogger<PaymentAuditService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogPaymentStatusChangeAsync(int paymentId, PaymentStatus oldStatus,
            PaymentStatus newStatus, string changedBy, string reason, string? transactionId = null,
            string? idempotencyKey = null, object? additionalData = null)
        {
            try
            {
                var auditLog = new PaymentAuditLog
                {
                    PaymentId = paymentId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = changedBy ?? "System",
                    Reason = reason,
                    TransactionId = transactionId,
                    IdempotencyKey = idempotencyKey,
                    AdditionalData = additionalData != null
                        ? JsonSerializer.Serialize(additionalData)
                        : null
                };

                _dbContext.PaymentAuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Payment {PaymentId} status changed from {OldStatus} to {NewStatus} by {ChangedBy}. Reason: {Reason}",
                    paymentId, oldStatus, newStatus, changedBy, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log payment audit for PaymentId: {PaymentId}", paymentId);
                // Don't throw - audit failure shouldn't break payment processing
            }
        }

        public async Task<IEnumerable<PaymentAuditLog>> GetPaymentHistoryAsync(int paymentId)
        {
            return await _dbContext.PaymentAuditLogs
                .Where(x => x.PaymentId == paymentId)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentAuditLog>> GetRecentAuditLogsAsync(int count = 100)
        {
            return await _dbContext.PaymentAuditLogs
                .OrderByDescending(x => x.ChangedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
