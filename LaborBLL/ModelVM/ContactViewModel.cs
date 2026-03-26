

namespace LaborBLL.ModelVM
{
    public class ContactViewModel
    {
        public string OtherUserId { get; set; } = string.Empty;

        public string bookingId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public bool IsAdmin { get; set; } // ✅ أضف ده
        public string? ProfilePictureUrl { get; set; } // ✅ أضف ده

        public DateTime? LastSeen { get; set; }
        public string FullName { get; set; }
    }
}
