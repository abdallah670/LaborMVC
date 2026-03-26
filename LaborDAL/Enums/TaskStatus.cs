namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the status of a task in the lifecycle
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// Task is created and open for applications
        /// </summary>
        Created = 0,

        /// <summary>
        /// Task is open and accepting offers
        /// </summary>
        Open = 1,

        /// <summary>
        /// Task has been accepted by a worker (application accepted)
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// Task has been scheduled with a specific start time
        /// </summary>
        Scheduled = 3,

        /// <summary>
        /// Task is in progress (worker has started)
        /// </summary>
        InProgress = 4,

        /// <summary>
        /// Task is completed and pending review
        /// </summary>
        Completed = 5,

        /// <summary>
        /// Task has been cancelled
        /// </summary>
        Cancelled = 6,

        /// <summary>
        /// Task resulted in no-show (client or worker)
        /// </summary>
        NoShow = 7,

        /// <summary>
        /// Task has expired without being assigned
        /// </summary>
        Expired = 8
    }

    /// <summary>
    /// Defines the type of cancellation
    /// </summary>
    public enum CancellationType
    {
        /// <summary>
        /// Cancelled by the client
        /// </summary>
        ClientCancellation = 1,

        /// <summary>
        /// Cancelled by the worker
        /// </summary>
        WorkerCancellation = 2,

        /// <summary>
        /// System-initiated cancellation (e.g., expired)
        /// </summary>
        SystemCancellation = 3
    }

    /// <summary>
    /// Defines the reason for task cancellation
    /// </summary>
    public enum CancellationReason
    {
        /// <summary>
        /// No specific reason provided
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Schedule conflict
        /// </summary>
        ScheduleConflict = 1,

        /// <summary>
        /// Change of mind
        /// </summary>
        ChangeOfMind = 2,

        /// <summary>
        /// Emergency situation
        /// </summary>
        Emergency = 3,

        /// <summary>
        /// Found alternative solution
        /// </summary>
        FoundAlternative = 4,

        /// <summary>
        /// Task requirements changed
        /// </summary>
        RequirementsChanged = 5,

        /// <summary>
        /// Communication issues
        /// </summary>
        CommunicationIssues = 6,

        /// <summary>
        /// Worker no-show
        /// </summary>
        WorkerNoShow = 7,

        /// <summary>
        /// Client no-show
        /// </summary>
        ClientNoShow = 8,

        /// <summary>
        /// Both parties no-show
        /// </summary>
        MutualNoShow = 9,

        /// <summary>
        /// Task expired
        /// </summary>
        Expired = 10
    }

    /// <summary>
    /// Defines the penalty severity tier
    /// </summary>
    public enum PenaltyTier
    {
        /// <summary>
        /// No penalty applied
        /// </summary>
        None = 0,

        /// <summary>
        /// Minor penalty (warning, note on record)
        /// </summary>
        Minor = 1,

        /// <summary>
        /// Moderate penalty (rating decrease, strike)
        /// </summary>
        Moderate = 2,

        /// <summary>
        /// Severe penalty (suspension, significant rating hit)
        /// </summary>
        Severe = 3
    }
}
