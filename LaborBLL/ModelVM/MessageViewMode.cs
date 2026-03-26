using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class MessageViewMode
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string? SenderAvatar { get; set; }
        public bool IsRead { get; set; }
    }
}