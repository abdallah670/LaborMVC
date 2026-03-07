using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class AllRatingViewModel
    {
        public string id { get; set; }
        public string RaterId { get; set; }
        public int bookingId { get; set; }
        public string RatedId { get; set; }
        public string RaterName { get; set; }
        public string comment { get; set; }  
        public string RateeName { get; set; }
        public decimal Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OverallAverageRating { get; set; }
        public int TotalRatingsReceived { get; set; }
    }
}
