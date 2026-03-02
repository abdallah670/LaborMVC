using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Entities
{
    public class ChatUsers
    {
        public int Id { get; set; }

        public string Content { get; set; } = null!;

        public string SenderId { get; set; } = null!;
        public AppUser Sender { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;
        public AppUser Receiver { get; set; } = null!;

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }
        public bool? isread { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
