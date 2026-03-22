using System.Collections.Generic;

namespace LaborBLL.ModelVM
{
    public class WorkerDashboardViewModel
    {
        // Statistics
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int PendingApplications { get; set; }
        public int TotalApplications { get; set; }

        // Data Lists
        public List<BookingDashboardViewModel> RecentBookings { get; set; } = new();
        public List<TaskApplicationViewModel> RecentApplications { get; set; } = new();
        public List<TaskListViewModel> AvailableTasks { get; set; } = new();
    }
}
