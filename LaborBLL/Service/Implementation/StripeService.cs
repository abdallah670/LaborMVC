

namespace LaborBLL.Service.Implementation
{
    using LaborBLL.Service.Abstract;
    using LaborDAL.DB;
    using LaborDAL.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Stripe;

    /// <summary>
    /// Service for handling Stripe payment operations
    /// </summary>
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext? _context;

        public StripeService(IConfiguration config, ApplicationDbContext? context = null)
        {
            _config = config;
            _context = context;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"]!;
        }

        /// <inheritdoc />
        public async Task CapturePaymentIntentAsync(string? transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID is required", nameof(transactionId));
            }

            try
            {
                var service = new PaymentIntentService();
                await service.CaptureAsync(transactionId);
            }
            catch (StripeException ex)
            {
                throw new InvalidOperationException($"Failed to capture payment: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task<RefundResult> RefundPaymentAsync(string paymentIntentId, decimal? amount = null, string? reason = null)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Reason = reason switch
                    {
                        "requested_by_customer" => "requested_by_customer",
                        "duplicate" => "duplicate",
                        "fraudulent" => "fraudulent",
                        _ => null
                    }
                };

                if (amount.HasValue)
                {
                    options.Amount = (long)(amount.Value * 100); // Convert to cents
                }

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                return new RefundResult
                {
                    Success = refund.Status == "succeeded" || refund.Status == "pending",
                    RefundId = refund.Id,
                    Status = refund.Status,
                    AmountRefunded = (decimal)refund.Amount / 100, // Convert from cents
                    ErrorMessage = null
                };
            }
            catch (StripeException ex)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Refund failed: {ex.Message}"
                };
            }
        }

        /// <inheritdoc />
        public async Task<TransferResult> TransferToWorkerAsync(string stripeAccountId, decimal amount, string description, string? idempotencyKey = null)
        {
            try
            {
                var options = new TransferCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = "usd", // TODO: Make configurable
                    Destination = stripeAccountId,
                    Description = description
                };

                var requestOptions = new RequestOptions();
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    requestOptions.IdempotencyKey = idempotencyKey;
                }

                var service = new TransferService();
                var transfer = await service.CreateAsync(options, requestOptions);

                return new TransferResult
                {
                    Success = true,
                    TransferId = transfer.Id,
                    Status = transfer.Reversed ? "reversed" : "transferred",
                    AmountTransferred = amount,
                    ErrorMessage = null
                };
            }
            catch (StripeException ex)
            {
                return new TransferResult
                {
                    Success = false,
                    ErrorMessage = $"Transfer failed: {ex.Message}"
                };
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetPaymentIntentForBookingAsync(int bookingId)
        {
            if (_context == null)
            {
                return null;
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            return payment?.TransactionId;
        }

        #region Stripe Connect

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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
