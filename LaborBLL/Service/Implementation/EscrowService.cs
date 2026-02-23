
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
            var result=await paymentService.CreatePaymentIntentAsync(bookingId, booking.AgreedRate);
            if (!result.Success)
            {
                return new Response<bool>(false, false, $"Failed to hold payment: {result.ErrorMessage}");
            }
            booking.Worker.HasVisa = true;
            return new Response<bool>(true, true, null);
        }

        public async Task<Response<bool>> ProcessCancellationAsync(int bookingId, string cancelledBy)
        {
            var booking =await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found.");
            }
            var HourUnitStart=(booking.StartTime -DateTime.UtcNow)?.TotalHours??0;
            if (cancelledBy == booking.PosterId && HourUnitStart < 2)
            {
                var refundAmount = booking.AgreedRate * 0.5m;
                var refundResult = await paymentService.PartialRefundAsync(bookingId, refundAmount);
                if (!refundResult.Success)
                {
                    return new Response<bool>(false, false, $"Failed to process cancellation: {refundResult.ErrorMessage}");
                }
            }
            else
            {
                var refuned = await paymentService.RefundPaymentAsync(bookingId);
                if (!refuned.Success)
                {
                    return new Response<bool>(false, false, $"Failed to process cancellation: {refuned.ErrorMessage}");
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
