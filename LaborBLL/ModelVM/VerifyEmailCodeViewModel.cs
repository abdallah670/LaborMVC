using System.ComponentModel.DataAnnotations;

namespace LaborBLL.ModelVM
{
    /// <summary>
    /// ViewModel for email verification code entry
    /// </summary>
    public class VerifyEmailCodeViewModel
    {
        /// <summary>
        /// User ID (hidden)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User email (display only)
        /// </summary>
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 6-digit verification code
        /// </summary>
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Whether to remember the user after verification
        /// </summary>
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        /// <summary>
        /// Seconds remaining until resend is allowed
        /// </summary>
        public int ResendCooldownSeconds { get; set; }
    }
}
