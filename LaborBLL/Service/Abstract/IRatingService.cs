using LaborBLL.ModelVM.Rating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.Service.Abstract
{
    public interface IRatingService
    {
       Task<Response<bool>> SubmitOrUpdateRatingAsync(RatingViewModel model, string raterId);
        Task<LaborDAL.Entities.Rating> GetRatingAsync(string raterId, string rateeId, int bookingId);
        Task<Response<List<AllRatingViewModel>>> GetAllRatingById(string ratedId);
    }
}
