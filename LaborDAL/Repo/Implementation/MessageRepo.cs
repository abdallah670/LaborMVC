

namespace LaborDAL.Repo.Implementation
{
    public class MessageRepo : Repository<Message>, IMessageRepo
    {
        public MessageRepo(ApplicationDbContext context) : base(context)
        {
        }
        public async Task AddMessageAsync(Message message)
        {
            await _dbSet.AddAsync(message);
        }

        public async Task<IEnumerable<Message>> GetMessagesByBookingIdAsync(int bookingId)
        {
            return await _dbSet.Include(m=>m.Sender)
                .Where(m => m.bookingId == bookingId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbSet.Where(m => m.SenderId == userId && !m.IsRead).CountAsync();
            
        }

        public async Task MarkAsReadAsync(int bookingId, string userId)
        {
           var messages = await _dbSet.Where(m => m.bookingId == bookingId && m.SenderId == userId && !m.IsRead).ToListAsync();
            foreach (var message in messages)
            {
                message.IsRead = true;
            }
            _dbSet.UpdateRange(messages);
        }
    }
}
