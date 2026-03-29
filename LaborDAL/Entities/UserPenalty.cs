namespace LaborDAL.Entities
{
    /// <summary>
    /// Tracks penalties applied to users (strikes, rating decreases, suspensions)
    /// </summary>
    public class UserPenalty : BaseEntity
    {
        /// <summary>
        /// ID of the user who received the penalty
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public virtual AppUser? User { get; set; }

        /// <summary>
        /// Type of penalty applied
        /// </summary>
        public PenaltyType Type { get; set; }

        /// <summary>
        /// Severity tier of the penalty
        /// </summary>
        public PenaltyTier Severity { get; set; }

        /// <summary>
        /// Human-readable reason for the penalty
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// When the penalty was applied
        /// </summary>
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the penalty expires (null for permanent)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether the penalty is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ID of the related task (if applicable)
        /// </summary>
        public int? RelatedTaskId { get; set; }

        /// <summary>
        /// For rating decreases: the amount decreased
        /// </summary>
        public decimal? RatingDecreaseAmount { get; set; }

        /// <summary>
        /// For rating decreases: the rating before penalty
        /// </summary>
        public decimal? PreviousRating { get; set; }

        /// <summary>
        /// For rating decreases: the rating after penalty
        /// </summary>
        public decimal? NewRating { get; set; }

        /// <summary>
        /// Whether the user has acknowledged/viewed this penalty
        /// </summary>
        public bool IsAcknowledged { get; set; } = false;

        /// <summary>
        /// When the user acknowledged the penalty
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// Additional metadata (JSON for flexibility)
        /// </summary>
        public string? Metadata { get; set; }
    }
}
