


namespace LaborBLL.Service.Abstract
{
    public interface IMessageService
    {
        Task<Response<bool>> SendMessageAsync(int bookingId, string SenderId, string message);
        Task<Response<IEnumerable<MessageViewMode>>> GetMessagesByBookingIdAsync(int bookingId, string userId);
        Task<Response<bool>> MarkAsReadAsync(int bookingId, string userId);
        Task<Response<int>> GetUnreadCountAsync(string userId);
    }
}
