
using Hangfire.Server;

namespace LaborBLL.Service.Implementation
{
    public class EscrowService : IEscrowService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IPaymentService paymentService;

        public EscrowService( IUnitOfWork unitOfWork ,IPaymentService paymentService)
        {
            this.unitOfWork = unitOfWork;
            this.paymentService = paymentService;
        }
        public async Task<Response<bool>> HoldPaymentAsync(int bookingId)
        {
           var booking =await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool> (  false, false, "Booking not found." );
            }
            var payment = new PaymentVM
            {
                BookingId = bookingId,
                Amount = booking.AgreedRate,
                UserId = booking.PosterId,  // Add UserId
                Status = PaymentStatus.Pending.ToString(),  // Start as Pending
                PaymentType = "Booking",
                Description = $"Payment for booking #{bookingId}",
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            var result=await paymentService.CreateAsync(payment);
            if (!result.Success)
            {
                return new Response<bool>(false, false, $"Failed to hold payment: {result.ErrorMessage}");
            }
            booking.Worker.HasVisa = true;
            return new Response<bool>(true, true, null);
        }

        public async Task<Response<bool>> ProcessCancellationAsync(int bookingId, string cancelledBy)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found.");
            }

            // Get the payment first!
            var payment = await unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
            if (payment == null)
            {
                return new Response<bool>(false, false, "Payment not found.");
            }

            var hoursUntilStart = (booking.StartTime - DateTime.UtcNow)?.TotalHours ?? 0;

            if (cancelledBy == booking.PosterId && hoursUntilStart < 2)
            {
                // Late cancellation: 50% refund
                var refundAmount = booking.AgreedRate * 0.5m;
                var refundResult = await paymentService.PartialRefundAsync(payment.Id, refundAmount);  // ✓ Use payment.Id
                if (!refundResult.Success)
                {
                    return new Response<bool>(false, false, $"Failed to process cancellation: {refundResult.ErrorMessage}");
                }
            }
            else
            {
                // Full refund
                var refundResult = await paymentService.RefundPaymentAsync(payment.Id);  // ✓ Use payment.Id
                if (!refundResult.Success)
                {
                    return new Response<bool>(false, false, $"Failed to process cancellation: {refundResult.ErrorMessage}");
                }
            }

            return new Response<bool>(true, true, null);
        }

        public async Task<Response<bool>> ReleasePaymentAsync(int bookingId)
        {
            var booking =await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return (new Response<bool>(false, false, "Booking not found."));
            }
            if (booking.Status != BookingStatus.Completed)
            {
                return new Response<bool>(false, false, "Booking must be completed by both parties");
            }
            var result=await paymentService.CapturePaymentAsync(bookingId);
            if (!result.Success)
            {
                return new Response<bool>(false, false, $"Failed to release payment: {result.ErrorMessage}");
            }
            return new Response<bool>(true, true, null);

        }
    }
}
