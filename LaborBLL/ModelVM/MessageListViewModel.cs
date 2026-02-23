using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class MessageListViewModel
    {
        public int BookingId { get; set; }
        public List<MessageViewMode> Messages { get; set; }

    }
}
