
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
<<<<<<< HEAD
        IchatRepo chatrepo { get; }
=======

        // Distributed transaction repositories
        IOutboxMessageRepository OutboxMessages { get; }
        IPendingTransferRepository PendingTransfers { get; }
        ISagaRepository Sagas { get; }
>>>>>>> f88f35f9a231932f53fbe9ce53bc3c70ae192a9a
    }
}
