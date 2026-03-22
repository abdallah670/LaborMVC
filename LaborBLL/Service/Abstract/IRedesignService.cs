using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaborBLL.Service.Abstract
{
    public interface IRedesignService
    {
        Task<RedesignProgressViewModel> GetProgressAsync();
    }

    public class RedesignProgressViewModel
    {
        public List<RedesignPhaseViewModel> Phases { get; set; } = new();
        public int OverallProgress { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }

    public class RedesignPhaseViewModel
    {
        public string Title { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public List<RedesignTaskViewModel> Tasks { get; set; } = new();
    }

    public class RedesignTaskViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public List<string> DoneCriteria { get; set; } = new();
        public bool IsCompleted => Status?.Contains("✔") == true || Status?.ToLower() == "done" || Status?.ToLower() == "completed";
    }
}
