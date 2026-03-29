
namespace LaborDAL.Repo.Abstract
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        IAppUserRepository AppUsers { get; }
        IBookingRepo Bookings { get; }
        IRepository<Rating> Ratings { get; }

        ITaskRepository Tasks { get; }
        IDisputeRepo Disputes { get; }
        IPaymentRepo Payments { get; }
        IMessageRepo Messages { get; }
        IchatRepo chatrepo { get; }
        IRatingRepo RatingRepo { get; }

        // Distributed transaction repositories
        IOutboxMessageRepository OutboxMessages { get; }
        IPendingTransferRepository PendingTransfers { get; }
        ISagaRepository Sagas { get; }

        // Notification system
        INotificationRepo Notifications { get; }

        // File upload audit
        IFileUploadAuditRepo FileUploadAudits { get; }

        // ID Verification (KYC)
        IIDVerificationRepo IDVerifications { get; }

        /// <summary>
        /// Gets a soft-deleted user by email (bypasses global query filter)
        /// </summary>
        Task<AppUser?> GetDeletedUserByEmailAsync(string email);

        /// <summary>
        /// Gets a soft-deleted user by ID (bypasses global query filter)
        /// </summary>
        Task<AppUser?> GetDeletedUserByIdAsync(string userId);

        /// <summary>
        /// Gets any user by ID bypassing global query filter
        /// </summary>
        Task<AppUser?> GetUserByIdBypassFilterAsync(string userId);
    }
}
