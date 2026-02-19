

namespace LaborDAL.Repo.Abstract
{
    public interface IBookingRepo : IRepository<Booking>
    {
        Task<List<Booking>> GetBookingsByWorkerIdAsync(string workerId);
        Task<List<Booking>> GetBookingsByPosterIdAsync(string posterId);
   

        Task<List<Booking>> GetOverlappingBookingsAsync(int workerId, DateTime start, DateTime end);
        Task<List<Booking>> GetBookingsWithWorkerAsync(Expression<Func<Booking, bool>> predicate);
        Task<List<Booking>> GetBookingsWithPosterAsync(string PosterId);


    }
}
