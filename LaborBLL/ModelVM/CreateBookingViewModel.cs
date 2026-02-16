
namespace LaborBLL.ModelVM
{
    public class CreateBookingViewModel
    {
        

            public decimal AgreedRate { get; set; }

            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }


            public int TaskId { get; set; }
            public int WorkerId { get; set; }

    }
}
