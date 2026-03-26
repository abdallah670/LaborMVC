

namespace LaborBLL.ModelVM
{
    public class BookingDetailViewModel
    {
        public int Id { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public string PosterId { get; set; }
        public string PosterName { get; set; }
  
        public string WorkerName { get; set; }
        public string? CurrentUserAvatar { get; set; }
        public string? TargetUserAvatar { get; set; }
        public string? WorkerAvatar { get; set; }
        public string? PosterAvatar { get; set; }
        public decimal AgreedRate { get; set; }
        public DateTime lastUpdateRate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public BookingStatus Status { get; set; }
        public int TaskId { get; set; }
        public string WorkerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
