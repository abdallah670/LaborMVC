


namespace LaborDAL.Repo.Implementation
{
    public class PaymentRepo : Repository<Payment>, IPaymentRepo  
    {
        private readonly ApplicationDbContext Context;
        public PaymentRepo(ApplicationDbContext Context) : base(Context)
        {
            this.Context = Context;

        }
        public async Task<Payment> GetPaymentByBookingIdAsync(int bookingId)
        {
            var Payment = await Context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);

            return Payment;

        }
       

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            var payments =await _dbSet.Where(p => p.Status == status).ToListAsync();
            return payments;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status)
        {
            var payments = await _dbSet.Where(p => p.Status.ToString() == status).ToListAsync();
            return payments;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(string userId)
        {
            var payments =await _dbSet.Include(p => p.Booking)
                .Where(p => p.Booking.WorkerId == userId||p.Booking.PosterId==userId)
                .ToListAsync();
            return payments;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsPendingReleaseAsync(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);
            var payments = await _dbSet.Where(p => p.Status == PaymentStatus.Held && p.CreatedAt <= cutoffTime).ToListAsync();
            return payments;
        }

        public async Task UpdatePaymentStatusAsync(int paymentId, PaymentStatus status)
        {
           var payment =await _dbSet.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment != null)
            {
                payment.Status = status;
                payment.UpdatedAt = DateTime.UtcNow;
                if (status == PaymentStatus.Released)
                {
                    payment.ReleasedAt = DateTime.UtcNow;
                }
                _dbSet.Update(payment);
            }
        }
    }
}
