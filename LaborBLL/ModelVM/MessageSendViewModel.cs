
namespace LaborBLL.ModelVM
{
    public class MessageSendViewModel
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int BookingId { get; set; }  // أضف دي
        public string? OtherUserId { get; set; }

    }
}
