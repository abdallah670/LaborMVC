
namespace LaborBLL.Service.Abstract
{
    public interface IPaymentService
    {
        Task<Response<string>> CreatePaymentIntentAsync(int bookingId, decimal Amount);
        Task<Response<bool>> CapturePaymentAsync(int bookingId);
        Task<Response<bool>> RefundPaymentAsync(int bookingId);
        Task<Response<bool>> PartialRefundAsync(int bookingId, decimal amount);
    


}
}
