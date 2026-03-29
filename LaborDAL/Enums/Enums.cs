
namespace LaborDAL.Enums
{
    public enum BookingStatus
    {
            Scheduled = 0,
            InProgress = 1,
            CompletedfromWorker = 2,

            Completed = 3,
            Cancelled = 4,
            Disputed = 5

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
