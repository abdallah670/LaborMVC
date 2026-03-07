using LaborBLL.ModelVM.Rating;
using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LaborBLL.Service.Implementation.Rating
{
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public RatingService(IUnitOfWork unitOfWork,IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public async Task<Response<bool>> SubmitOrUpdateRatingAsync(RatingViewModel model, string raterId)
        {
            var rating = await unitOfWork.Ratings.FirstOrDefaultAsync(
                r => r.RateeId == model.RatedId && r.RaterId == raterId && r.bookingId == model.bookingId);

            if (rating != null)
            {
                rating.Score = model.Score;
                rating.Comment = model.comment;
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
                    Comment=model.comment,
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

        public async Task<Response<List<AllRatingViewModel>>> GetAllRatingById(string ratedId)
        {
            try
            {
                // استخدم اسم الدالة الصحيح
                var ratings = await unitOfWork.RatingRepo.GetAllRatingByUserId(ratedId);

                if (ratings == null || !ratings.Any())
                {
                    return new Response<List<AllRatingViewModel>>(
                        new List<AllRatingViewModel>(),
                        true,
                        "لا توجد تقييمات"
                    );
                }

                // AutoMapper هيجيب الأسماء تلقائياً من Rater و Rated
                var rate = mapper.Map<List<AllRatingViewModel>>(ratings);

                return new Response<List<AllRatingViewModel>>(rate, true, "تم جلب البيانات بنجاح");
            }
            catch (Exception ex)
            {
                // سجل الخطأ هنا
                return new Response<List<AllRatingViewModel>>(null, false, ex.Message);
            }
        }

    }
}