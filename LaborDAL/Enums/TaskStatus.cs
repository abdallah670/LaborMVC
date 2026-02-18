namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the status of a task
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// Task is open and accepting offers
        /// </summary>
        Open = 1,

        /// <summary>
        /// Task has been assigned to a worker
        /// </summary>
        Assigned = 2,

        /// <summary>
        /// Task is in progress
        /// </summary>
        InProgress = 3,

        /// <summary>
        /// Task is completed and pending review
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Task has been cancelled
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// Task has expired without being assigned
        /// </summary>
        Expired = 6
    }
}
