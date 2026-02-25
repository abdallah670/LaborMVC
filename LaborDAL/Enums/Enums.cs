
namespace LaborDAL.Enums
{
    public enum BookingStatus
    {
            Scheduled,
            InProgress,
            CompletedfromWorker,

            Completed,
            Cancelled,
            Disputed

    }
    public enum PaymentStatus
    {
        Pending,
        Held,
        Released,
        Refunded,
        PartiallyRefunded,
        Failed
    }
}
