
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
        ITaskRepository Tasks { get; }
        IDisputeRepo Disputes { get; }
    }
}
