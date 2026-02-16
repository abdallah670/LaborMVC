using LaborDAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Entities
{
    public class Booking
    {

                protected Booking() { }
        public Booking(decimal agreedRate,DateTime? startTime,DateTime? endTime,int taskId,int workerId)
        {
            AgreedRate = agreedRate;
            StartTime = startTime;
            EndTime = endTime;
            TaskId = taskId;
            WorkerId = workerId;
            Status = BookingStatus.Cancelled;
            CreatedAt = DateTime.UtcNow;
        }
        public void Update(DateTime start, DateTime end,decimal rate)
        {
            StartTime = start;
            EndTime = end;
            AgreedRate = rate;
        }

       
        public int Id { get; private set; }
        public decimal AgreedRate { get; private set; }
        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public BookingStatus Status { get; private set; }
        public int TaskId { get; private set; }
        public int WorkerId { get; private set; }
        public byte[] RowVersion { get; set; }
        public DateTime CreatedAt { get; private set; }





    }
}
