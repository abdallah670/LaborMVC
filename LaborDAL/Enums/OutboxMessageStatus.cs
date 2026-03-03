
namespace LaborDAL.Enums
{
    /// <summary>
    /// Status for outbox messages in the Outbox pattern
    /// </summary>
    public enum OutboxMessageStatus
    {
        /// <summary>
        /// Message is pending processing
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Message is being processed
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Message processed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Message processing failed
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Message permanently failed after all retries
        /// </summary>
        DeadLetter = 4
    }
}
