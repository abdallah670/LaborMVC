using LaborBLL.ModelVM.Rating;
using LaborBLL.Service.Abstract.Rating;

namespace LaborBLL.Service.Implementation.Rating
{
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork unitOfWork;

        public RatingService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public async Task<Response<bool>> SubmitOrUpdateRatingAsync(RatingViewModel model, string raterId)
        {
            var rating = await unitOfWork.Ratings.FirstOrDefaultAsync(
                r => r.RateeId == model.RatedId && r.RaterId == raterId && r.bookingId == model.bookingId);

            if (rating != null)
            {
                rating.Score = model.Score;
                rating.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.Ratings.UpdateAsync(rating);
            }
            else
            {
                var newRating = new LaborDAL.Entities.Rating
                {
                    RaterId = raterId,
                    RateeId = model.RatedId,
                    bookingId = model.bookingId,
                    Score = model.Score,
                    CreatedAt = DateTime.UtcNow
                };
                await unitOfWork.Ratings.AddAsync(newRating);
            }

            await unitOfWork.SaveAsync();

            // احسب الـ Average بعد الـ Save
            var allRatings = await unitOfWork.Ratings.FindAsync(r => r.RateeId == model.RatedId);
            var ratingsList = allRatings.ToList();

            decimal average = ratingsList.Any()
                ? (decimal)ratingsList.Average(r => r.Score)
                : model.Score;

            await unitOfWork.AppUsers.UpdateUserRatingAsync(model.RatedId, average);

            return new Response<bool>(true, true, null);
        }

        public async Task<LaborDAL.Entities.Rating> GetRatingAsync(string raterId, string rateeId, int bookingId)
        {
            return await unitOfWork.Ratings.FirstOrDefaultAsync(
                r => r.RaterId == raterId && r.RateeId == rateeId && r.bookingId == bookingId);
        }
    }
}