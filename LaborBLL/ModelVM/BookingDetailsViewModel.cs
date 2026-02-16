using LaborDAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class BookingDetailsViewModel
    {
       

            public decimal AgreedRate { get; set; }

            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }

        public BookingStatus Status { get; set; }

        public int TaskId { get; set; }
            public int WorkerId { get; set; }

            public DateTime CreatedAt { get; set; }
        

    }
}
