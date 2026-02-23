
using LaborDAL.Enums;

namespace LaborDAL.Repo.Abstract
{
    public interface IPaymentRepo : IRepository<Payment>
    {
      Task <Payment> GetPaymentByBookingIdAsync(int bookingId);
       Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(string userId);
       Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(paymentStatus status);
        Task UpdatePaymentStatusAsync(int paymentId, paymentStatus status);
    }
}
