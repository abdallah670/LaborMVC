using LaborBLL.Response;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for handling user verification workflows
    /// </summary>
    public interface IVerificationService
    {
        #region Email Verification

        /// <summary>
        /// Generates and sends email verification token
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if verification email sent successfully</returns>
        Task<Response<bool>> SendEmailVerificationAsync(string userId);

        /// <summary>
        /// Verifies email with the provided token
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="token">The verification token</param>
        /// <returns>True if verification successful</returns>
        Task<Response<bool>> VerifyEmailAsync(string userId, string token);

        #endregion

        #region Phone Verification

        /// <summary>
        /// Generates and sends phone verification code (SMS)
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="phoneNumber">The phone number to verify</param>
        /// <returns>True if verification code sent successfully</returns>
        Task<Response<bool>> SendPhoneVerificationAsync(string userId, string phoneNumber);

        /// <summary>
        /// Verifies phone with the provided code
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="code">The verification code</param>
        /// <returns>True if verification successful</returns>
        Task<Response<bool>> VerifyPhoneAsync(string userId, string code);

        #endregion

        #region ID Verification

        /// <summary>
        /// Submits ID document for verification
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="documentUrl">URL to the uploaded document</param>
        /// <returns>True if document submitted successfully</returns>
        Task<Response<bool>> SubmitIDDocumentAsync(string userId, string documentUrl);

        /// <summary>
        /// Approves or rejects ID document (admin only)
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="approved">Whether the document is approved</param>
        /// <param name="adminNotes">Optional notes from admin</param>
        /// <returns>True if action completed successfully</returns>
        Task<Response<bool>> ReviewIDDocumentAsync(string userId, bool approved, string? adminNotes = null);

        #endregion

        #region Verification Status

        /// <summary>
        /// Gets the current verification tier for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The verification tier</returns>
        Task<VerificationTier> GetVerificationTierAsync(string userId);

        /// <summary>
        /// Checks if user can perform tasks above the specified amount
        /// Unverified users are limited to $100 tasks
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="taskAmount">The task amount to check</param>
        /// <returns>True if user can perform the task</returns>
        Task<bool> CanPerformTaskAsync(string userId, decimal taskAmount);

        /// <summary>
        /// Updates the verification tier based on current verification status
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if tier updated successfully</returns>
        Task<bool> UpdateVerificationTierAsync(string userId);

        #endregion
    }
}
