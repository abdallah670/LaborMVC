
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

        // Distributed transaction repositories
        IOutboxMessageRepository OutboxMessages { get; }
        IPendingTransferRepository PendingTransfers { get; }
        ISagaRepository Sagas { get; }
    }
}
