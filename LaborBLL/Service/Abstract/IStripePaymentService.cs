using System.Threading.Tasks;

namespace LaborBLL.Service.Abstract
{
    public interface IStripePaymentService
    {
        /// <summary>
        /// Creates a Stripe Checkout Session for the given email and Booking.
        /// Returns the Stripe Checkout URL.
        /// </summary>
   
        Task<string> CreateBookingCheckoutSessionAsync(string email, int bookingId, string userId, string actionType, string successUrl, string cancelUrl);
    }
}
