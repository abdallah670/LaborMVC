using LaborDAL.Enums;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Represents a dispute raised against a booking
    /// </summary>
    public class Dispute
    {
        protected Dispute() { }

        public Dispute(int bookingId, string raisedBy, string reason)
        {
            BookingId = bookingId;
            RaisedBy = raisedBy;
            Reason = reason;
            Status = DisputeStatus.Open;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the booking being disputed
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Foreign key to the user who raised the dispute
        /// </summary>
        public string RaisedBy { get; set; } = string.Empty;

        /// <summary>
        /// Reason for the dispute
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the dispute
        /// </summary>
        public DisputeStatus Status { get; set; }

        /// <summary>
        /// Admin's resolution notes
        /// </summary>
        public string? Resolution { get; set; }

        /// <summary>
        /// How the dispute was resolved
        /// </summary>
        public ResolutionType? ResolutionType { get; set; }

        /// <summary>
        /// Percentage of payment the worker receives (for custom splits)
        /// </summary>
        public int? WorkerPercentage { get; set; }

        /// <summary>
        /// Foreign key to the admin who resolved the dispute
        /// </summary>
        public string? ResolvedBy { get; set; }

        /// <summary>
        /// When the dispute was resolved
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// When the dispute was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the dispute was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties

        /// <summary>
        /// The booking being disputed
        /// </summary>
        public Booking Booking { get; set; } = null!;

        /// <summary>
        /// The user who raised the dispute
        /// </summary>
        public AppUser RaisedByUser { get; set; } = null!;

        /// <summary>
        /// The admin who resolved the dispute
        /// </summary>
        public AppUser? ResolvedByUser { get; set; }
    }
}
