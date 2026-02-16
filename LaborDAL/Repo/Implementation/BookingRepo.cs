

namespace LaborDAL.Repo.Implementation
{
    public class BookingRepo : Repository<Booking>, IBookingRepo
    {

        public BookingRepo(ApplicationDbContext context) : base(context)
        {
         
        }
      
       
        public Task<List<Booking>> GetBookingsByPosterIdAsync(string posterId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Booking>> GetBookingsByWorkerIdAsync(string workerId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Booking>> GetOverlappingBookingsAsync(int workerId, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

    }
}
