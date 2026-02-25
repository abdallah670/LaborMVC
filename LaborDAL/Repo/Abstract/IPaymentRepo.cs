
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    public interface IPaymentRepo : IRepository<Payment>
    {
       Task <Payment> GetPaymentByBookingIdAsync(int bookingId);
       Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(string userId);
       Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
       Task UpdatePaymentStatusAsync(int paymentId, PaymentStatus status);
        Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status);
        Task<IEnumerable<Payment>> GetPaymentsPendingReleaseAsync(TimeSpan timeSpan);
       
    }
}
