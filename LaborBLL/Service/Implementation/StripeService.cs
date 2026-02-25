
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
        }
                // Note: ApplicationFeeAmount removed - requires Stripe Connect with destination account
                // Platform fee will be handled separately through transfer logic after payment capture
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

        #region Stripe Connect

        public async Task<string> CreateConnectAccountAsync(string email, string firstName, string lastName)
        {
            var options = new AccountCreateOptions
            {
                Type = "express",
                Email = email,
                BusinessType = "individual",
                Individual = new AccountIndividualOptions
                {
                    FirstName = firstName,
                    LastName = lastName,
                },
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                },
                Settings = new AccountSettingsOptions
                {
                    Payouts = new AccountSettingsPayoutsOptions
                    {
                        Schedule = new AccountSettingsPayoutsScheduleOptions
                        {
                            Interval = "manual",
                        },
                    },
                },
            };

            var service = new AccountService();
            var account = await service.CreateAsync(options);
            return account.Id;
        }

        public async Task<string> CreateAccountLinkAsync(string accountId, string refreshUrl, string returnUrl)
        {
            var options = new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            };

            var service = new AccountLinkService();
            var link = await service.CreateAsync(options);
            return link.Url;
        }

        public async Task<bool> IsAccountEnabledAsync(string accountId)
        {
            try
            {
                var service = new AccountService();
                var account = await service.GetAsync(accountId);
                return account.Capabilities.Transfers == "active" && account.ChargesEnabled;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
