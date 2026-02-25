using System.Threading.Tasks;

namespace LaborBLL.Service.Abstract
{
    public interface IStripeService
    {
        Task CapturePaymentIntentAsync(string? transactionId);
        Task<StripePaymentIntentResult> CreatePaymentIntentAsync(double amount, string currency, 
            string description, int bookingId ,string? idempotencyKey);
        
        // Stripe Connect methods
        Task<string> CreateConnectAccountAsync(string email, string firstName, string lastName);
        Task<string> CreateAccountLinkAsync(string accountId, string refreshUrl, string returnUrl);
        Task<bool> IsAccountEnabledAsync(string accountId);
    }
}
