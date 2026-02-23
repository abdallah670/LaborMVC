

namespace LaborDAL.DB
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int bookingId { get; set; }
        public string SenderId { get; set; }
        public DateTime SentAt { get; set; }= DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public Booking Booking { get; set; }
        public AppUser Sender { get; set; }
    }
}
