
    namespace LaborBLL.ModelVM
    {
        public class CreateBookingViewModel
        {
        

                public decimal AgreedRate { get; set; }

                public DateTime StartTime { get; set; }= DateTime.Now;
            public DateTime EndTime { get; set; }= DateTime.Now.AddHours(1);


            public int TaskId { get; set; }
                public string WorkerId { get; set; }

        }
    }
