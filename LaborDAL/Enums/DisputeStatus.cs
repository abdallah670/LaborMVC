namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the status of a dispute
    /// </summary>
    public enum DisputeStatus
    {
        /// <summary>
        /// Dispute is open and awaiting admin review
        /// </summary>
        Open = 1,

        /// <summary>
        /// Admin is actively reviewing the dispute
        /// </summary>
        UnderReview = 2,

        /// <summary>
        /// Dispute has been resolved by admin
        /// </summary>
        Resolved = 3
    }
}
