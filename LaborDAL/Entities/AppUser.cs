

namespace LaborDAL.Entities
{
    /// <summary>
    /// Extended user entity inheriting from ASP.NET Core Identity User
    /// </summary>
    public class AppUser : IdentityUser
    {
        /// <summary>
        /// User's first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User's last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Indicates if the user's ID has been verified
        /// </summary>
        public bool IDVerified { get; set; } = false;

        /// <summary>
        /// Current verification tier level
        /// </summary>
        public VerificationTier VerificationTier { get; set; } = VerificationTier.Unverified;

        /// <summary>
        /// URL to ID document (for verification)
        /// </summary>
        public string? IDDocumentUrl { get; set; }

        /// <summary>
        /// Date when ID document was submitted
        /// </summary>
        public DateTime? IDDocumentSubmittedAt { get; set; }

        /// <summary>
        /// Phone verification code
        /// </summary>
        public string? PhoneVerificationCode { get; set; }

        /// <summary>
        /// Phone verification code expiry
        /// </summary>
        public DateTime? PhoneVerificationExpiry { get; set; }

        /// <summary>
        /// Email verification token
        /// </summary>
        public string? EmailVerificationToken { get; set; }

        /// <summary>
        /// Email verification token expiry
        /// </summary>
        public DateTime? EmailVerificationExpiry { get; set; }

        /// <summary>
        /// User's average rating from reviews
        /// </summary>
        public decimal? AverageRating { get; set; }
        public ICollection<Rating> RatingsGiven { get; set; }
        public ICollection<Rating> RatingsReceived { get; set; }
        /// <summary>
        /// User's location URL (map link)
        /// </summary>
        public string? LocationUrl { get; set; }

        /// <summary>
        /// User's location (address)
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// User's geographic latitude (-90 to 90)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// User's geographic longitude (-180 to 180)
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// User's country
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// URL to user's profile picture
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// User's bio/description
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// User's skills (comma-separated)
        /// </summary>
        public string? Skills { get; set; }

        /// <summary>
        /// User role (Admin, Worker, Poster, or combinations using flags)
        /// Examples: Admin, Worker, Poster, Admin|Worker, Admin|Poster, Worker|Poster, Admin|Worker|Poster
        /// </summary>
        public ClientRole Role { get; set; } = ClientRole.Worker;

        /// <summary>
        /// Account creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Deletion timestamp
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// ID of user who deleted this record
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// ID of user who last updated this record
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// ID of user who created this record
        /// </summary>
        public string? CreatedBy { get; set; }
        public virtual ICollection<Booking> PostedBookings { get; set; } = new List<Booking>();


        // Navigation properties

        /// <summary>
        /// Tasks posted by this user
        /// </summary>
        public virtual ICollection<TaskItem> PostedTasks { get; set; } = new List<TaskItem>();

        /// <summary>
        /// Tasks assigned to this user (as worker)
        /// </summary>
        public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();

        /// <summary>
        /// Task applications submitted by this user
        /// </summary>
        public virtual ICollection<TaskApplication> Applications { get; set; } = new List<TaskApplication>();
    }
}
