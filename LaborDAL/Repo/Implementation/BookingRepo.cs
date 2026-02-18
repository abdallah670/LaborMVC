

namespace LaborDAL.Repo.Implementation
{
    public class BookingRepo : Repository<Booking>, IBookingRepo
    {

        public BookingRepo(ApplicationDbContext context) : base(context)
        {
         
        }
        public override async Task<Booking> GetByIdAsync(int id)
        {
            var booking = await _dbSet.Include(b => b.Task)
                .ThenInclude(t=>t.Poster)
                .Include(b=> b.Worker)
                                      .FirstOrDefaultAsync(b => b.Id == id);
            return booking;
        }
       
        public Task<List<Booking>> GetBookingsByPosterIdAsync(string posterId)
        {
            throw new NotImplementedException();
        }
        public async Task<List<Booking>> GetBookingsWithWorkerAsync(Expression<Func<Booking, bool>> predicate)
        {
            return await _dbSet.Include(b => b.Worker)
                               .Where(predicate)
                               .ToListAsync();
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
