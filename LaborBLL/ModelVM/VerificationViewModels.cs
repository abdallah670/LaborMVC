using LaborDAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace LaborBLL.ModelVM
{
    #region Email Verification

    /// <summary>
    /// Request to send email verification
    /// </summary>
    public class SendEmailVerificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email confirmation parameters
    /// </summary>
    public class ConfirmEmailViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    #endregion

    #region Phone Verification

    /// <summary>
    /// Request to send phone verification SMS
    /// </summary>
    public class SendPhoneVerificationRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string CountryCode { get; set; } = "+20"; // Default Egypt
    }

    /// <summary>
    /// Verify phone number with code
    /// </summary>
    public class VerifyPhoneRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }

    #endregion

    #region ID Verification

    /// <summary>
    /// Submit ID verification request
    /// </summary>
    public class IdVerificationRequestDto
    {
        [Required]
        public IdDocumentType DocumentType { get; set; }

        [StringLength(50)]
        public string? DocumentNumber { get; set; }

        [StringLength(100)]
        public string? DocumentCountry { get; set; }

        // File URLs will be set after upload
        public string? FrontDocumentUrl { get; set; }
        public string? BackDocumentUrl { get; set; }
        public string? SelfieUrl { get; set; }
    }

    /// <summary>
    /// ID verification status response
    /// </summary>
    public class IdVerificationStatusDto
    {
        public bool HasSubmitted { get; set; }
        public VerificationStatus? Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? RejectionReason { get; set; }
        public IdDocumentType? DocumentType { get; set; }
    }

    #endregion

    #region Verification Status

    /// <summary>
    /// Complete verification status for a user
    /// </summary>
    public class UserVerificationStatusDto
    {
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public bool IsIdVerified { get; set; }
        public VerificationTier CurrentTier { get; set; }
        public int CompletedVerifications { get; set; }
        public int TotalVerifications => 3; // Email, Phone, ID
        public double CompletionPercentage => (CompletedVerifications / (double)TotalVerifications) * 100;
    }

    #endregion
}
