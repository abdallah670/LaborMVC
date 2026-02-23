
namespace LaborBLL.ModelVM
{
    public class CheckoutViewModel
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string ClientSecret { get; set; }
        public string PubishableKey { get; set; }
    }
}
