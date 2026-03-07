using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Repo.Implementation
{
    public class RatingRepo : Repository<Rating>, IRatingRepo
    {
        private readonly ApplicationDbContext Context;
        public RatingRepo(ApplicationDbContext context) : base(context)
        {
            context = context;
        }

        // في RatingRepo
        public async Task<List<Rating>> GetAllRatingByUserId(string userid)
        {
            return await _dbSet
                .Where(p => p.RateeId == userid)
                .Include(p => p.Rater)        // جلب بيانات المُقيّم كاملة
                .Include(p => p.Rated)         // جلب بيانات المُقيَّم كاملة
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
       
    }
}
