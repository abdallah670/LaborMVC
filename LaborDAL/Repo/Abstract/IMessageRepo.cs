
namespace LaborDAL.Repo.Abstract
{
    public interface IMessageRepo
    {
            Task AddMessageAsync(Message message);
        Task<IEnumerable<Message>> GetMessagesByBookingIdAsync(int bookingId);
        Task<List<Message>> GetMessagesUserIdAsync(string UserId);
        Task MarkAsReadAsync(int bookingId, string userId);
            Task<int> GetUnreadCountAsync(string userId);
        Task <List<Message>> GetConversationAsync (string userId,string otheruserid);
        Task<int> GetLastBookingIdAsync(string userId, string otherUserId);




    }
}
