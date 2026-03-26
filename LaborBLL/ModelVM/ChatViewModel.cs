using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class ChatViewModel
    {
        public int BookingId { get; set; }      // ✅ أضف ده
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public List<MessageViewMode> Messages { get; set; }

        public ChatViewModel()
        {
            OtherUserId = string.Empty;
            OtherUserName = string.Empty;
            Messages = new List<MessageViewMode>();
        }
    }
}