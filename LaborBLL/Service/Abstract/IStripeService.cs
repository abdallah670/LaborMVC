using System.Threading.Tasks;

namespace LaborBLL.Service.Abstract
{
    public interface IStripeService
    {
        Task CapturePaymentIntentAsync(string? transactionId);
        Task<StripePaymentIntentResult> CreatePaymentIntentAsync(double amount, string currency, 
            string description, int bookingId ,string? idempotencyKey);
    }
}
