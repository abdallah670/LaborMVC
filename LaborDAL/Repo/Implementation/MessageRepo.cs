

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

        public async Task<List<Message>> GetConversationAsync(string userId, string otherUserId)
        {
            return await _dbSet
                .Include(m => m.Sender)
                .Include(m => m.Booking)
                .Where(m =>
                    (m.Booking.PosterId == userId && m.Booking.WorkerId == otherUserId) ||
                    (m.Booking.PosterId == otherUserId && m.Booking.WorkerId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        public async Task<int> GetLastBookingIdAsync(string userId, string otherUserId)
        {
            var booking = await _dbSet
                .Include(m => m.Booking)
                .Where(m =>
                    (m.Booking.PosterId == userId && m.Booking.WorkerId == otherUserId) ||
                    (m.Booking.PosterId == otherUserId && m.Booking.WorkerId == userId))
                .OrderByDescending(m => m.SentAt)
                .Select(m => m.bookingId)
                .FirstOrDefaultAsync();

            return booking;
        }

        public async Task<IEnumerable<Message>> GetMessagesByBookingIdAsync(int bookingId)
        {
            return await _dbSet
                .Include(m => m.Sender)
                .Where(m => m.bookingId == bookingId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        public async Task<List<Message>> GetMessagesUserIdAsync(string userId)
        {
            var messages = await _dbSet
                .Where(m => m.SenderId == userId || m.Booking.PosterId == userId || m.Booking.WorkerId == userId)
                .Include(m => m.Sender)
                .Include(m => m.Booking)
                    .ThenInclude(b => b.Poster)
                .Include(m => m.Booking)
                    .ThenInclude(b => b.Worker)
                .ToListAsync();

            return messages;
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
