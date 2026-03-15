using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for ID verification submissions
    /// </summary>
    public class IDVerificationRepo : Repository<IDVerification>, IIDVerificationRepo
    {
        private readonly ApplicationDbContext _context;

        public IDVerificationRepo(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Get verification by user ID
        /// </summary>
        public async Task<IDVerification?> GetByUserIdAsync(string userId)
        {
            return await _context.IDVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.UserId == userId);
        }

        /// <summary>
        /// Get latest verification by user ID
        /// </summary>
        public async Task<IDVerification?> GetLatestByUserIdAsync(string userId)
        {
            return await _context.IDVerifications
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Check if user has pending verification
        /// </summary>
        public async Task<bool> HasPendingVerificationAsync(string userId)
        {
            return await _context.IDVerifications
                .AnyAsync(v => v.UserId == userId &&
                    (v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InReview));
        }

        /// <summary>
        /// Get all pending verifications
        /// </summary>
        public async Task<IEnumerable<IDVerification>> GetPendingVerificationsAsync()
        {
            return await _context.IDVerifications
                .Include(v => v.User)
                .Where(v => v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InReview)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get verifications by status
        /// </summary>
        public async Task<IEnumerable<IDVerification>> GetByStatusAsync(VerificationStatus status)
        {
            return await _context.IDVerifications
                .Include(v => v.User)
                .Where(v => v.Status == status)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
    }
}
