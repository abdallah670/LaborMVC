
using LaborBLL.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens.Experimental;
using Polly;
using Polly.Retry;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace LaborBLL.Service.Implementation
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly StripeSettings _stripeSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AsyncRetryPolicy _retryPolicy;

        public StripePaymentService(IOptions<StripeSettings> stripeSettings, IUnitOfWork unitOfWork)
        {
            _stripeSettings = stripeSettings.Value;
            _unitOfWork = unitOfWork;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            // Configure Polly retry policy for transient failures
            _retryPolicy = Policy
                .Handle<StripeException>(ex => ex.StripeError?.Type == "api_connection_error" || ex.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Stripe API retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    });
        }


        public async Task<string> CreateBookingCheckoutSessionAsync(string email, int bookingId, string memberId, string actionType, string successUrl, string cancelUrl)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null) throw new Exception("Booking not found");

            return await _retryPolicy.ExecuteAsync(async () =>
            {
               
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "payment",
                    CustomerEmail = email,
                    SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = cancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "memberId", memberId },
                        { "bookingId", bookingId.ToString() },
                        { "actionType", actionType } 
                    },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(booking.Task.Budget * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{booking.Task.Title} ({actionType})",
                                    Description = "LaborMVC Booking"
                                }
                            },
                            Quantity = 1
                        }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);
                return session.Url;
            });
        }

    }
}
