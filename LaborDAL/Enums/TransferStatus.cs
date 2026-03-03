
namespace LaborDAL.Enums
{
    /// <summary>
    /// Status for pending transfers in the transfer queue
    /// </summary>
    public enum TransferStatus
    {
        /// <summary>
        /// Transfer is queued and waiting to be processed
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Transfer is being processed
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Transfer completed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Transfer failed and can be retried
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Transfer permanently failed after all retries
        /// </summary>
        PermanentlyFailed = 4,

        /// <summary>
        /// Transfer was cancelled
        /// </summary>
        Cancelled = 5
    }
}
