
namespace LaborBLL.Service.Abstract
{
    public interface IPaymentService
    {
     
        Task<Response<bool>> CapturePaymentAsync(int bookingId);
        Task<Response<bool>> PartialRefundAsync(int Id, decimal amount);
        Task<Response<PaymentVM>> GetPaymentByBookingIdAsync(int bookingId);
        Task<Response<PaymentVM>> CreateAsync(PaymentVM model);
        Task<Response<PaymentVM>> GetByIdAsync(int id);
        Task<Response<List<PaymentVM>>> GetAllAsync();
        Task<Response<List<PaymentVM>>> GetByUserIdAsync(string userId);
        Task<Response<List<PaymentVM>>> GetByStatusAsync(string status);
        Task<Response<PaymentVM>> UpdateAsync(PaymentVM model);
        Task<Response<bool>> ProcessPaymentAsync(int id);
        Task<Response<bool>> RefundPaymentAsync(int id);
        Task<Response<bool>> DeleteAsync(int id);
        Task<Response<bool>> TransferToWorkerAsync(int paymentId, string workerStripeAccountId);
        Task<Response<PaymentVM>> GetPaymentStatusAsync(int bookingId);
       



    }
}
