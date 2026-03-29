using System.Threading.Tasks;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service interface for Stripe payment operations
    /// </summary>
    public interface IStripeService
    {
        /// <summary>
        /// Captures a previously authorized payment intent
        /// </summary>
        Task CapturePaymentIntentAsync(string? transactionId);

        /// <summary>
        /// Creates a new payment intent for escrow hold
        /// </summary>
        Task<StripePaymentIntentResult> CreatePaymentIntentAsync(double amount, string currency,
            string description, int bookingId, string? idempotencyKey);

        /// <summary>
        /// Refunds a payment partially or fully
        /// </summary>
        Task<RefundResult> RefundPaymentAsync(string paymentIntentId, decimal? amount = null, string? reason = null);

        /// <summary>
        /// Transfers funds to a connected account (worker payout)
        /// </summary>
        Task<TransferResult> TransferToWorkerAsync(string stripeAccountId, decimal amount, string description, string? idempotencyKey = null);

        // Stripe Connect methods

        /// <summary>
        /// Creates a new Stripe Connect Express account
        /// </summary>
        Task<string> CreateConnectAccountAsync(string email, string firstName, string lastName);

        /// <summary>
        /// Creates an onboarding link for a Connect account
        /// </summary>
        Task<string> CreateAccountLinkAsync(string accountId, string refreshUrl, string returnUrl);

        /// <summary>
        /// Checks if a Connect account is enabled for transfers
        /// </summary>
        Task<bool> IsAccountEnabledAsync(string accountId);

        /// <summary>
        /// Gets the payment intent ID associated with a booking
        /// </summary>
        Task<string?> GetPaymentIntentForBookingAsync(int bookingId);
    }

    /// <summary>
    /// Result of a refund operation
    /// </summary>
    public class RefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public string? Status { get; set; }
        public decimal AmountRefunded { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a transfer operation
    /// </summary>
    public class TransferResult
    {
        public bool Success { get; set; }
        public string? TransferId { get; set; }
        public string? Status { get; set; }
        public decimal AmountTransferred { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
