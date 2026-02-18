namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the status of a task application
    /// </summary>
    public enum ApplicationStatus
    {
        /// <summary>
        /// Application is pending review
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Application has been viewed by the poster
        /// </summary>
        Viewed = 2,

        /// <summary>
        /// Application has been accepted
        /// </summary>
        Accepted = 3,

        /// <summary>
        /// Application has been rejected
        /// </summary>
        Rejected = 4,

        /// <summary>
        /// Application was withdrawn by the worker
        /// </summary>
        Withdrawn = 5
    }
}
