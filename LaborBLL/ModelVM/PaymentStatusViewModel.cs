

namespace LaborBLL.ModelVM
{
    public class PaymentStatusViewModel
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreatAt { get; set; }
        public DateTime? RealaseAt { get; set; }
    }
}
