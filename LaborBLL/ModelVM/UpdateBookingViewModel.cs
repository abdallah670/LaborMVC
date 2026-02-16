using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class UpdateBookingViewModel
    {
       
            public int Id { get; set; }

            public decimal AgreedRate { get; set; }

            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }

            public string Status { get; set; }

            public byte[] RowVersion { get; set; }

    }
}
