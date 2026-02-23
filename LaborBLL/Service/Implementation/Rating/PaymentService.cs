using Microsoft.Extensions.Configuration;
using Stripe;

namespace LaborBLL.Service.Implementation
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.configuration = configuration;
            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        }

        public async Task<Response<string>> CreatePaymentIntentAsync(int bookingId, decimal amount)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
                return new Response<string>(null, false, "Booking not found.");
            
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = "usd",
                CaptureMethod = "manual",
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", bookingId.ToString() }
                }
            };

            var service = new PaymentIntentService();
            try
            {
                var paymentIntent = await service.CreateAsync(options);
                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = amount,
                    StripePaymentIntentId = paymentIntent.Id,
                    Status = paymentStatus.Held
                };
                booking.Poster.HasVisa = true;

                await unitOfWork.Payments.AddAsync(payment);
                await unitOfWork.SaveAsync();
                return new Response<string>(paymentIntent.ClientSecret, true, null);
            }
            catch (Exception ex)
            {
                return new Response<string>(null, false, $"Error creating payment intent: {ex.Message}");
            }
        }

        public async Task<Response<bool>> CapturePaymentAsync(int bookingId)
        {
            var payment = unitOfWork.Payments.Get(p => p.BookingId == bookingId).FirstOrDefault();
            if (payment == null)
                return new Response<bool>(false, false, "Payment not found.");

            var service = new PaymentIntentService();
            try
            {
                await service.CaptureAsync(payment.StripePaymentIntentId);
                await unitOfWork.Payments.UpdatePaymentStatusAsync(payment.Id, paymentStatus.Released);
                await unitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error capturing payment: {ex.Message}");
            }
        }

        public async Task<Response<bool>> RefundPaymentAsync(int bookingId)
        {
            var payment = unitOfWork.Payments.Get(p => p.BookingId == bookingId).FirstOrDefault();
            if (payment == null)
                return new Response<bool>(false, false, "Payment not found.");

            var options = new RefundCreateOptions
            {
                PaymentIntent = payment.StripePaymentIntentId
            };

            var service = new RefundService();
            try
            {
                await service.CreateAsync(options);
                await unitOfWork.Payments.UpdatePaymentStatusAsync(payment.Id, paymentStatus.Refunded);
                await unitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error processing refund: {ex.Message}");
            }
        }

        public async Task<Response<bool>> PartialRefundAsync(int bookingId, decimal amount)
        {
            var payment = unitOfWork.Payments.Get(p => p.BookingId == bookingId).FirstOrDefault();
            if (payment == null)
                return new Response<bool>(false, false, "Payment not found.");

            var options = new RefundCreateOptions
            {
                PaymentIntent = payment.StripePaymentIntentId,
                Amount = (long)(amount * 100)
            };

            var service = new RefundService();
            try
            {
                await service.CreateAsync(options);
                await unitOfWork.Payments.UpdatePaymentStatusAsync(payment.Id, paymentStatus.PartiallyRefunded);
                await unitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error processing refund: {ex.Message}");
            }
        }
    }
}