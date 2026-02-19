

namespace LaborDAL.Entities
{
    public class Booking
    {

                protected Booking() { }
        public Booking(decimal agreedRate,DateTime? startTime,DateTime? endTime,int taskId,string workerId,string posterId)
        {
            AgreedRate = agreedRate;
            StartTime = startTime;
            this.PosterId = posterId;
            EndTime = endTime;
            TaskItemId = taskId;
            WorkerId = workerId;
            Status = BookingStatus.Scheduled;
            CreatedAt = DateTime.UtcNow;
        }
        public void Update(DateTime start, DateTime end,decimal rate, BookingStatus status)
        {
            this.Status = status;
            StartTime = start;
            EndTime = end;
            AgreedRate = rate;
        }

       
        public int Id { get; private set; }
        public decimal AgreedRate { get; private set; }
        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public BookingStatus Status { get;  set; }
        public int TaskItemId { get;  set; }
        public string WorkerId { get; set; }
        public string? PosterId { get; set; }
        public byte[] RowVersion { get; set; }
        public DateTime CreatedAt { get; set; }

        public AppUser Worker { get; set; }
        public AppUser Poster { get; set; }
        public TaskItem? Task { get; set; }





    }
}
