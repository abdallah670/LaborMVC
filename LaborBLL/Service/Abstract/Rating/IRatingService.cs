
using LaborBLL.ModelVM.Rating;

namespace LaborBLL.Service.Abstract.Rating
{
    public interface IRatingService 
    {
        Task<Response<bool>> SubmitOrUpdateRatingAsync(RatingViewModel model ,string RaterId);
        Task<LaborDAL.Entities.Rating> GetRatingAsync(string raterId, string rateeId, int bookingId);
    }
}
