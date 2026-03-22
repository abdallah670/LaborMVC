namespace LaborBLL.ModelVM
{
    public class BookingDashboardViewModel
    {
        public int Id { get; set; } // BookingId
        public string UserName { get; set; }
        public string TaskTitle { get; set; }
        public string WorkerName { get; set; }
        public string PosterName { get; set; }
        public decimal AgreedRate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<BookingListViewModel> Bookings { get; set; } = new List<BookingListViewModel>();

        public BookingStatus Status { get; set; }
        public string PaymentStatus { get; set; }  // Held, Pending, Released, Refunded


        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int DisputedCount { get; set; }
       public string PosterId { get; set; }
        public string WorkerId { get; set; }
        public bool WokerHasVisa { get; set; }
        public bool PosterHasVisa { get; set; }
    }




}
