


namespace LaborDAL.Repo.Implementation
{
    public class PaymentRepo : Repository<Payment>, IPaymentRepo  
    {
        public PaymentRepo(ApplicationDbContext Context) : base(Context)
        {
           
        }
        public async Task<Payment> GetPaymentByBookingIdAsync(int bookingId)
        {
            var Payment = await _dbSet.Include(p=>p.Booking).FirstOrDefaultAsync(p => p.BookingId == bookingId);
           
            return Payment;

        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(paymentStatus status)
        {
        var payments =await _dbSet.Where(p => p.Status == status).ToListAsync();
            return payments;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(string userId)
        {
            var payments =await _dbSet.Include(p => p.Booking)
                .Where(p => p.Booking.WorkerId == userId||p.Booking.PosterId==userId)
                .ToListAsync();
            return payments;
        }

        public async Task UpdatePaymentStatusAsync(int paymentId, paymentStatus status)
        {
           var payment =await _dbSet.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment != null)
            {
                payment.Status = status;
                payment.UpdatedAt = DateTime.UtcNow;
                if (status == paymentStatus.Released)
                {
                    payment.ReleasedAt = DateTime.UtcNow;
                }
                _dbSet.Update(payment);
            }
        }
    }
}
