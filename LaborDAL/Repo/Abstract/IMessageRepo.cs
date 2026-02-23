

namespace LaborDAL.Repo.Abstract
{
    public interface IMessageRepo
    {
            Task AddMessageAsync(Message message);
            Task<IEnumerable<Message>> GetMessagesByBookingIdAsync(int bookingId);
            Task MarkAsReadAsync(int bookingId, string userId);
            Task<int> GetUnreadCountAsync(string userId);



    }
}
