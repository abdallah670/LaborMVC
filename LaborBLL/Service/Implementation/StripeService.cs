
using LaborDAL.Entities;
using Microsoft.Extensions.Configuration;
using Stripe;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaborBLL.Service.Implementation
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _config;

        public StripeService(IConfiguration config)
        {
            _config = config;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        public  Task CapturePaymentIntentAsync(string? transactionId)
        {
            try
            {
                var service = new PaymentIntentService();
                return service.CaptureAsync(transactionId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error capturing payment intent: {ex.Message}");
            }
        }

        public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
           double amount,
           string currency,
           string description,
            int bookingId,
          string? idempotencyKey = null)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLower(),
                Description = description,
                CaptureMethod = "manual",  // ← Escrow: hold funds
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
        {
            { "bookingId", bookingId.ToString() }
        },
                // Optional: Platform fee
                ApplicationFeeAmount = (long)(amount * 0.10 * 100), // 10% fee
            };

            var requestOptions = new RequestOptions();
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                requestOptions.IdempotencyKey = idempotencyKey;
            }

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options, requestOptions);

            return new StripePaymentIntentResult
            {
                ClientSecret = intent.ClientSecret,
                PaymentIntentId = intent.Id
            };
        }
    }
}
