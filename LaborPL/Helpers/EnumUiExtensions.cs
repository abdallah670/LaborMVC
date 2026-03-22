using LaborDAL.Enums;

namespace LaborPL.Helpers
{
    public static class EnumUiExtensions
    {
        public static string GetBadgeColor(this BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Scheduled => "primary",
                BookingStatus.InProgress => "warning",
                BookingStatus.Completed => "success",
                BookingStatus.Cancelled => "danger",
                BookingStatus.Disputed => "dark",
                _ => "secondary"
            };
        }

        public static string GetBadgeColor(this ApplicationStatus status)
        {
            return status switch
            {
                ApplicationStatus.Pending => "warning",
                ApplicationStatus.Accepted => "success",
                ApplicationStatus.Rejected => "danger",
                ApplicationStatus.Withdrawn => "secondary",
                _ => "secondary"
            };
        }

        public static string GetBadgeColor(this TaskCategory category)
        {
            return category switch
            {
                TaskCategory.Cleaning => "success",
                TaskCategory.Moving => "primary",
                TaskCategory.Repair => "warning",
                TaskCategory.Gardening => "info",
                TaskCategory.Painting => "danger",
                TaskCategory.Plumbing => "primary",
                TaskCategory.Electrical => "warning",
                _ => "secondary"
            };
        }
    }
}
