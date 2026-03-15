using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborDAL.Enums;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for handling user verification (email, phone, ID)
    /// </summary>
    public interface IVerificationService
    {
        #region Email Verification

        /// <summary>
        /// Send email verification link to user
        /// </summary>
        Task<Response<bool>> SendEmailVerificationAsync(string userId, string email);

        /// <summary>
        /// Confirm email verification token
        /// </summary>
        Task<Response<bool>> ConfirmEmailAsync(string userId, string token);

        /// <summary>
        /// Resend email verification with rate limiting
        /// </summary>
        Task<Response<bool>> ResendEmailVerificationAsync(string userId);

        /// <summary>
        /// Check if user can resend email verification
        /// </summary>
        Task<bool> CanResendEmailAsync(string userId);

        #endregion

        #region Phone Verification

        /// <summary>
        /// Send phone verification SMS code
        /// </summary>
        Task<Response<bool>> SendPhoneVerificationAsync(string userId, string phoneNumber, string countryCode = "+20");

        /// <summary>
        /// Verify phone number with SMS code
        /// </summary>
        Task<Response<bool>> VerifyPhoneAsync(string userId, string code);

        /// <summary>
        /// Check if user can request phone verification
        /// </summary>
        Task<bool> CanRequestPhoneVerificationAsync(string userId);

        #endregion

        #region ID Verification

        /// <summary>
        /// Submit ID documents for verification
        /// </summary>
        Task<Response<int>> SubmitIdVerificationAsync(string userId, IdVerificationRequestDto request);

        /// <summary>
        /// Check if user has pending ID verification
        /// </summary>
        Task<bool> HasPendingIdVerificationAsync(string userId);

        /// <summary>
        /// Get user's ID verification status
        /// </summary>
        Task<IdVerificationStatusDto> GetIdVerificationStatusAsync(string userId);

        /// <summary>
        /// Approve ID verification (for admin use)
        /// </summary>
        Task<Response<bool>> ApproveIdVerificationAsync(int verificationId, string adminId, string? notes = null);

        /// <summary>
        /// Reject ID verification (for admin use)
        /// </summary>
        Task<Response<bool>> RejectIdVerificationAsync(int verificationId, string adminId, string reason, string? notes = null);

        #endregion

        #region Verification Tier

        /// <summary>
        /// Update user's verification tier based on completed verifications
        /// </summary>
        Task UpdateVerificationTierAsync(string userId);

        /// <summary>
        /// Get user's current verification tier
        /// </summary>
        Task<VerificationTier> GetVerificationTierAsync(string userId);

        /// <summary>
        /// Get complete verification status for a user
        /// </summary>
        Task<UserVerificationStatusDto> GetUserVerificationStatusAsync(string userId);

        #endregion
    }
}
