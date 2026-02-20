
namespace LaborBLL.ModelVM.Rating
{
    public class RatingViewModel
    {
        public int bookingId { get; set; }

        public string RatedId { get; set; }
        [Range (1,5)]
        public int Score { get; set; }
    }
}
