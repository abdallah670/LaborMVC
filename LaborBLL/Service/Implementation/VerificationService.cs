namespace LaborBLL.Service
{
    /// <summary>
    /// Service for handling user verification workflows
    /// </summary>
    public class VerificationService : IVerificationService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<VerificationService> _logger;
        private const decimal UnverifiedTaskLimit = 100m;

        public VerificationService(
            UserManager<AppUser> userManager,
            ILogger<VerificationService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        #region Email Verification

        /// <inheritdoc />
        public async Task<Response<bool>> SendEmailVerificationAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Generate verification token
                var token = Guid.NewGuid().ToString("N");
                user.EmailVerificationToken = token;
                user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new Response<bool>(false, false, "Failed to generate verification token.");
                }

                // TODO: Send actual email with verification link
                // For now, we'll just log it
                _logger.LogInformation("Email verification token for user {UserId}: {Token}", userId, token);

                // In production, you would send an email like:
                // await _emailService.SendVerificationEmailAsync(user.Email, token);

                return new Response<bool>(true, true, "Verification email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification for user {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred while sending verification email.");
            }
        }

        /// <inheritdoc />
        public async Task<Response<bool>> VerifyEmailAsync(string userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                if (user.EmailVerificationToken != token)
                {
                    return new Response<bool>(false, false, "Invalid verification token.");
                }

                if (user.EmailVerificationExpiry < DateTime.UtcNow)
                {
                    return new Response<bool>(false, false, "Verification token has expired.");
                }

                // Mark email as confirmed
                user.EmailConfirmed = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationExpiry = null;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new Response<bool>(false, false, "Failed to verify email.");
                }

                // Update verification tier
                await UpdateVerificationTierAsync(userId);

                _logger.LogInformation("Email verified for user {UserId}", userId);
                return new Response<bool>(true, true, "Email verified successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for user {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred during email verification.");
            }
        }

        #endregion

        #region Phone Verification

        /// <inheritdoc />
        public async Task<Response<bool>> SendPhoneVerificationAsync(string userId, string phoneNumber)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Generate 6-digit verification code
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();

                user.PhoneNumber = phoneNumber;
                user.PhoneVerificationCode = code;
                user.PhoneVerificationExpiry = DateTime.UtcNow.AddMinutes(10);

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new Response<bool>(false, false, "Failed to generate verification code.");
                }

                // TODO: Send actual SMS with verification code
                // For now, we'll just log it
                _logger.LogInformation("Phone verification code for user {UserId}: {Code}", userId, code);

                // In production, you would send an SMS like:
                // await _smsService.SendVerificationSmsAsync(phoneNumber, code);

                return new Response<bool>(true, true, "Verification code sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending phone verification for user {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred while sending verification code.");
            }
        }

        /// <inheritdoc />
        public async Task<Response<bool>> VerifyPhoneAsync(string userId, string code)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                if (user.PhoneVerificationCode != code)
                {
                    return new Response<bool>(false, false, "Invalid verification code.");
                }

                if (user.PhoneVerificationExpiry < DateTime.UtcNow)
                {
                    return new Response<bool>(false, false, "Verification code has expired.");
                }

                // Mark phone as confirmed
                user.PhoneNumberConfirmed = true;
                user.PhoneVerificationCode = null;
                user.PhoneVerificationExpiry = null;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new Response<bool>(false, false, "Failed to verify phone.");
                }

                // Update verification tier
                await UpdateVerificationTierAsync(userId);

                _logger.LogInformation("Phone verified for user {UserId}", userId);
                return new Response<bool>(true, true, "Phone verified successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying phone for user {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred during phone verification.");
            }
        }

        #endregion

        #region ID Verification

        /// <inheritdoc />
        public async Task<Response<bool>> SubmitIDDocumentAsync(string userId, string documentUrl)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                user.IDDocumentUrl = documentUrl;
                user.IDDocumentSubmittedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new Response<bool>(false, false, "Failed to submit ID document.");
                }

                _logger.LogInformation("ID document submitted for user {UserId}", userId);
                return new Response<bool>(true, true, "ID document submitted successfully. It will be reviewed shortly.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting ID document for user {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred while submitting ID document.");
            }
        }

        /// <inheritdoc />
        public async Task<Response<bool>> ReviewIDDocumentAsync(string userId, bool approved, string? adminNotes = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                if (string.IsNullOrEmpty(user.IDDocumentUrl))
                {
                    return new Response<bool>(false, false, "No ID document found for this user.");
                }

                if (approved)
                {
                    user.IDVerified = true;
                    // Update verification tier
                    await UpdateVerificationTierAsync(userId);
                    _logger.LogInformation("ID document approved for user {UserId}", userId);
                }
                else
                {
                    // Reset document for re-submission
                    user.IDDocumentUrl = null;
                    user.IDDocumentSubmittedAt = null;
                    _logger.LogInformation("ID document rejected for user {UserId}. Reason: {Reason}", userId, adminNotes);
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new Response<bool>(false, false, "Failed to update ID verification status.");
                }

                return new Response<bool>(true, true, approved ? "ID document approved." : "ID document rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing ID document for user {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred during ID document review.");
            }
        }

        #endregion

        #region Verification Status

        /// <inheritdoc />
        public async Task<VerificationTier> GetVerificationTierAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return VerificationTier.Unverified;
            }

            return user.VerificationTier;
        }

        /// <inheritdoc />
        public async Task<bool> CanPerformTaskAsync(string userId, decimal taskAmount)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Unverified users are limited to $100 tasks
            if (user.VerificationTier == VerificationTier.Unverified)
            {
                return taskAmount <= UnverifiedTaskLimit;
            }

            // Verified users have no limits
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateVerificationTierAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Determine tier based on verification status
                VerificationTier newTier;
                if (user.IDVerified)
                {
                    newTier = VerificationTier.IDVerified;
                }
                else if (user.PhoneNumberConfirmed)
                {
                    newTier = VerificationTier.PhoneVerified;
                }
                else if (user.EmailConfirmed)
                {
                    newTier = VerificationTier.EmailVerified;
                }
                else
                {
                    newTier = VerificationTier.Unverified;
                }

                if (user.VerificationTier != newTier)
                {
                    user.VerificationTier = newTier;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Verification tier updated for user {UserId} to {Tier}", userId, newTier);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating verification tier for user {UserId}", userId);
                return false;
            }
        }

        #endregion
    }
}
