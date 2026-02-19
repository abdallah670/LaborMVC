using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for Dispute entities
    /// </summary>
    public class DisputeRepo : Repository<Dispute>, IDisputeRepo
    {
        public DisputeRepo(ApplicationDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<Dispute?> GetByBookingIdAsync(int bookingId)
        {
            return await _dbSet
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Task)
                .Include(d => d.RaisedByUser)
                .Include(d => d.ResolvedByUser)
                .FirstOrDefaultAsync(d => d.BookingId == bookingId);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Dispute>> GetDisputesByStatusAsync(DisputeStatus status)
        {
            return await _dbSet
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Task)
                .Include(d => d.RaisedByUser)
                .Where(d => d.Status == status)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Dispute>> GetDisputesByUserAsync(string userId)
        {
            return await _dbSet
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Task)
                .Include(d => d.RaisedByUser)
                .Where(d => d.RaisedBy == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Dispute>> GetAllWithIncludesAsync()
        {
            return await _dbSet
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Task)
                        .ThenInclude(t => t.Poster)
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Worker)
                .Include(d => d.RaisedByUser)
                .Include(d => d.ResolvedByUser)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Dispute?> GetByIdWithIncludesAsync(int id)
        {
            return await _dbSet
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Task)
                        .ThenInclude(t => t.Poster)
                .Include(d => d.Booking)
                    .ThenInclude(b => b.Worker)
                .Include(d => d.RaisedByUser)
                .Include(d => d.ResolvedByUser)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsForBookingAsync(int bookingId)
        {
            return await _dbSet.AnyAsync(d => d.BookingId == bookingId);
        }

        /// <inheritdoc />
        public async Task<int> GetCountByStatusAsync(DisputeStatus status)
        {
            return await _dbSet.CountAsync(d => d.Status == status);
        }
    }
}
