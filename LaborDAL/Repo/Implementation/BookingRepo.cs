

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
                .IgnoreQueryFilters()
                                      .FirstOrDefaultAsync(b => b.Id == id);
            return booking;
        }

        public async Task<List<Booking>> GetBookingsWithPosterAsync(string posterId)
        {
            return await _dbSet
                .Include(b => b.Task)        // جلب بيانات المهمة
                .Include(b => b.Poster)      // جلب بيانات البوستر
                .Where(b => b.Task.PosterId == posterId)
                .IgnoreQueryFilters()
                .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsWithWorkerAsync(Expression<Func<Booking, bool>> predicate)
        {
            return await _dbSet.Include(b => b.Worker)
                               .Where(predicate)
                               .IgnoreQueryFilters()
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

        public Task<List<Booking>> GetBookingsByPosterIdAsync(string posterId)
        {
            throw new NotImplementedException();
        }
    }
}
