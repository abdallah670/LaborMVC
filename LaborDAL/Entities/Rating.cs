using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Entities
{
    public class Rating
    {
        public int Id { get; set; }
        public string RaterId { get; set; }
        public string RateeId { get; set; }
        public int Score { get; set; }
        public int bookingId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public AppUser Rater { get; set; }
        public AppUser Rated { get; set; }
        public Booking Booking { get; set; }

    }
}
