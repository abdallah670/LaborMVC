

namespace LaborBLL.Service.Abstract
{
    public interface IEscrowService
    {


        Task<Response<bool>> HoldPaymentAsync(int bookingId);
        Task<Response<bool>> ReleasePaymentAsync(int bookingId);
        Task<Response<bool>> ProcessCancellationAsync(int bookingId, string cancelledBy);
    }

}
