
namespace LaborBLL.ModelVM
{
    public class MessageViewMode
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsMyMessage { get; set; }
    }
}
