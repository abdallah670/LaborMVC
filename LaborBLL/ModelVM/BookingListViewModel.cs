using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.ModelVM
{
    public class BookingListViewModel
    {
        public string Id { get; set; }
        public string TaskTitle { get; set; }
        public string WorkerName { get; set; }
        public decimal AgreedRate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public BookingStatus Status { get; set; }
    }
}
