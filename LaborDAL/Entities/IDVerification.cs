using LaborDAL.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborDAL.Entities
{
    /// <summary>
    /// ID verification submission for KYC (Know Your Customer)
    /// </summary>
    public class IDVerification : BaseEntity
    {
        /// <summary>
        /// Foreign key to the user who submitted the verification
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } = null!;

        /// <summary>
        /// Type of ID document submitted
        /// </summary>
        [Required]
        public IdDocumentType DocumentType { get; set; }

        /// <summary>
        /// Document number (e.g., passport number, ID card number)
        /// </summary>
        [StringLength(50)]
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Country that issued the document
        /// </summary>
        [StringLength(100)]
        public string? DocumentCountry { get; set; }

        /// <summary>
        /// URL to front side of ID document
        /// </summary>
        [Required]
        public string FrontDocumentUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL to back side of ID document
        /// </summary>
        public string? BackDocumentUrl { get; set; }

        /// <summary>
        /// URL to selfie photo for face matching
        /// </summary>
        public string? SelfieUrl { get; set; }

        /// <summary>
        /// Current status of the verification
        /// </summary>
        [Required]
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        /// <summary>
        /// Reason for rejection (if rejected)
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Admin who reviewed the submission
        /// </summary>
        public string? ReviewedBy { get; set; }

        /// <summary>
        /// When the submission was reviewed
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Additional notes from admin
        /// </summary>
        public string? AdminNotes { get; set; }

        /// <summary>
        /// Face matching score (0-100) if face matching was performed
        /// </summary>
        public int? FaceMatchScore { get; set; }

        /// <summary>
        /// Whether face matching passed
        /// </summary>
        public bool? FaceMatchPassed { get; set; }
    }
}
