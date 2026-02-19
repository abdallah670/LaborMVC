

namespace LaborBLL.ModelVM
{
    public class UpdateBookingViewModel
    {
       
            public int Id { get; set; }

            public decimal AgreedRate { get; set; }

            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }

            public BookingStatus Status { get; set; }

            public byte[] RowVersion { get; set; }

    }
}
