namespace LaborBLL.ModelVM
{
    public class BookingDashboardViewModel
    {
        public int Id { get; set; } // BookingId
        public string UserName { get; set; }
        public decimal AgreedRate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<BookingListViewModel> Bookings { get; set; } = new List<BookingListViewModel>();

        public BookingStatus Status { get; set; }

        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int DisputedCount { get; set; }
       public string PosterId { get; set; }
        public string WorkerId { get; set; }
    }




}
