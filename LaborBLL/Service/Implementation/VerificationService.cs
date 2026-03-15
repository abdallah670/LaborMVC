using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for handling user verification processes
    /// </summary>
    public class VerificationService : IVerificationService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<VerificationService> _logger;

        // Rate limiting settings
        private static readonly TimeSpan EmailResendCooldown = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan PhoneVerificationCooldown = TimeSpan.FromMinutes(1);
        private static readonly int MaxPhoneAttempts = 5;

        public VerificationService(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<VerificationService> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        #region Email Verification

        public async Task<Response<bool>> SendEmailVerificationAsync(string userId, string email)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Store token and expiry
                user.EmailVerificationToken = token;
                user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Email verification token generated for user {UserId}", userId);

                return new Response<bool>(true, true, "Verification email prepared successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification for user {UserId}", userId);
                return new Response<bool>(false, false, "Failed to send verification email.");
            }
        }

        public async Task<Response<bool>> ConfirmEmailAsync(string userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Check token expiry
                if (user.EmailVerificationExpiry.HasValue &&
                    user.EmailVerificationExpiry.Value < DateTime.UtcNow)
                {
                    return new Response<bool>(false, false, "Verification link has expired. Please request a new one.");
                }

                // Verify token
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    // Clear verification token
                    user.EmailVerificationToken = null;
                    user.EmailVerificationExpiry = null;
                    await _userManager.UpdateAsync(user);

                    // Update verification tier
                    await UpdateVerificationTierAsync(userId);

                    _logger.LogInformation("Email confirmed for user {UserId}", userId);
                    return new Response<bool>(true, true, "Email confirmed successfully.");
                }

                return new Response<bool>(false, false, "Invalid verification token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
                return new Response<bool>(false, false, "Failed to confirm email.");
            }
        }

        public async Task<Response<bool>> ResendEmailVerificationAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                if (user.EmailConfirmed)
                {
                    return new Response<bool>(false, false, "Email is already verified.");
                }

                // Check rate limit
                if (!await CanResendEmailAsync(userId))
                {
                    return new Response<bool>(false, false, "Please wait before requesting another verification email.");
                }

                return await SendEmailVerificationAsync(userId, user.Email!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email verification for user {UserId}", userId);
                return new Response<bool>(false, false, "Failed to resend verification email.");
            }
        }

        public async Task<bool> CanResendEmailAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if user has a recent expiry set (indicates recent send)
            if (user.EmailVerificationExpiry.HasValue)
            {
                var timeSinceExpirySet = DateTime.UtcNow.AddHours(24) - user.EmailVerificationExpiry.Value;
                if (timeSinceExpirySet < EmailResendCooldown)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Phone Verification

        public async Task<Response<bool>> SendPhoneVerificationAsync(string userId, string phoneNumber, string countryCode = "+20")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Validate phone number
                if (!_smsService.IsValidPhoneNumber(phoneNumber, countryCode))
                {
                    return new Response<bool>(false, false, "Invalid phone number format.");
                }

                var formattedNumber = _smsService.FormatPhoneNumber(phoneNumber, countryCode);

                // Check if phone is already verified
                if (user.PhoneNumberConfirmed && user.PhoneNumber == formattedNumber)
                {
                    return new Response<bool>(false, false, "Phone number is already verified.");
                }

                // Rate limiting
                if (!await CanRequestPhoneVerificationAsync(userId))
                {
                    return new Response<bool>(false, false, "Please wait 1 minute before requesting a new code.");
                }

                // Check max attempts
                if (user.PhoneVerificationAttempts >= MaxPhoneAttempts)
                {
                    return new Response<bool>(false, false, "Too many attempts. Please try again later.");
                }

                // Generate 6-digit code
                var code = new Random().Next(100000, 999999).ToString();

                // Store hashed code and expiry
                user.PhoneVerificationCode = _userManager.PasswordHasher.HashPassword(user, code);
                user.PhoneVerificationExpiry = DateTime.UtcNow.AddMinutes(10);
                user.LastPhoneVerificationAttempt = DateTime.UtcNow;
                user.PhoneVerificationAttempts++;
                user.PhoneNumber = formattedNumber;

                await _userManager.UpdateAsync(user);

                // Send SMS
                var message = $"Your Labor Marketplace verification code is: {code}. Valid for 10 minutes.";
                await _smsService.SendSmsAsync(formattedNumber, message);

                _logger.LogInformation("Phone verification code sent to user {UserId}", userId);

                return new Response<bool>(true, true, "Verification code sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending phone verification for user {UserId}", userId);
                return new Response<bool>(false, false, "Failed to send verification code.");
            }
        }

        public async Task<Response<bool>> VerifyPhoneAsync(string userId, string code)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Check if code has expired
                if (user.PhoneVerificationExpiry < DateTime.UtcNow)
                {
                    return new Response<bool>(false, false, "Verification code has expired. Please request a new one.");
                }

                // Verify code
                var result = _userManager.PasswordHasher.VerifyHashedPassword(
                    user, user.PhoneVerificationCode, code);

                if (result != PasswordVerificationResult.Success)
                {
                    return new Response<bool>(false, false, "Invalid verification code.");
                }

                // Mark phone as verified
                user.PhoneNumberConfirmed = true;
                user.PhoneVerificationCode = null;
                user.PhoneVerificationExpiry = null;
                user.PhoneVerificationAttempts = 0;

                await _userManager.UpdateAsync(user);

                // Update verification tier
                await UpdateVerificationTierAsync(userId);

                _logger.LogInformation("Phone number verified for user {UserId}", userId);

                return new Response<bool>(true, true, "Phone number verified successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying phone for user {UserId}", userId);
                return new Response<bool>(false, false, "Failed to verify phone number.");
            }
        }

        public async Task<bool> CanRequestPhoneVerificationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (user.LastPhoneVerificationAttempt.HasValue)
            {
                var timeSinceLastAttempt = DateTime.UtcNow - user.LastPhoneVerificationAttempt.Value;
                if (timeSinceLastAttempt < PhoneVerificationCooldown)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region ID Verification

        public async Task<Response<int>> SubmitIdVerificationAsync(string userId, IdVerificationRequestDto request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new Response<int>(0, false, "User not found.");
                }

                // Check if user already has ID verified
                if (user.IDVerified)
                {
                    return new Response<int>(0, false, "ID is already verified.");
                }

                // Check if there's a pending verification
                if (await _unitOfWork.IDVerifications.HasPendingVerificationAsync(userId))
                {
                    return new Response<int>(0, false, "You already have a pending ID verification request.");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.FrontDocumentUrl))
                {
                    return new Response<int>(0, false, "Front document image is required.");
                }

                // Create ID verification record
                var verification = new IDVerification
                {
                    UserId = userId,
                    DocumentType = request.DocumentType,
                    DocumentNumber = request.DocumentNumber,
                    DocumentCountry = request.DocumentCountry,
                    FrontDocumentUrl = request.FrontDocumentUrl,
                    BackDocumentUrl = request.BackDocumentUrl,
                    SelfieUrl = request.SelfieUrl,
                    Status = VerificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.IDVerifications.AddAsync(verification);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("ID verification submitted for user {UserId}", userId);

                return new Response<int>(verification.Id, true, "ID verification submitted successfully. It will be reviewed shortly.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting ID verification for user {UserId}", userId);
                return new Response<int>(0, false, "Failed to submit ID verification.");
            }
        }

        public async Task<bool> HasPendingIdVerificationAsync(string userId)
        {
            return await _unitOfWork.IDVerifications.HasPendingVerificationAsync(userId);
        }

        public async Task<IdVerificationStatusDto> GetIdVerificationStatusAsync(string userId)
        {
            var verification = await _unitOfWork.IDVerifications.GetLatestByUserIdAsync(userId);

            if (verification == null)
            {
                return new IdVerificationStatusDto { HasSubmitted = false };
            }

            return new IdVerificationStatusDto
            {
                HasSubmitted = true,
                Status = verification.Status,
                SubmittedAt = verification.CreatedAt,
                RejectionReason = verification.RejectionReason,
                DocumentType = verification.DocumentType
            };
        }

        /// <summary>
        /// Approve ID verification (for admin use)
        /// </summary>
        public async Task<Response<bool>> ApproveIdVerificationAsync(int verificationId, string adminId, string? notes = null)
        {
            try
            {
                var verification = await _unitOfWork.IDVerifications.GetByIdAsync(verificationId);
                if (verification == null)
                {
                    return new Response<bool>(false, false, "Verification request not found.");
                }

                verification.Status = VerificationStatus.Approved;
                verification.ReviewedBy = adminId;
                verification.ReviewedAt = DateTime.UtcNow;
                verification.AdminNotes = notes;

                // Mark user as ID verified
                var user = await _userManager.FindByIdAsync(verification.UserId);
                if (user != null)
                {
                    user.IDVerified = true;
                    user.IDDocumentUrl = verification.FrontDocumentUrl;
                    user.IDDocumentSubmittedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // Update verification tier
                    await UpdateVerificationTierAsync(verification.UserId);
                }

                await _unitOfWork.IDVerifications.UpdateAsync(verification);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("ID verification {VerificationId} approved by admin {AdminId}", verificationId, adminId);

                return new Response<bool>(true, true, "ID verification approved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving ID verification {VerificationId}", verificationId);
                return new Response<bool>(false, false, "Failed to approve ID verification.");
            }
        }

        /// <summary>
        /// Reject ID verification (for admin use)
        /// </summary>
        public async Task<Response<bool>> RejectIdVerificationAsync(int verificationId, string adminId, string reason, string? notes = null)
        {
            try
            {
                var verification = await _unitOfWork.IDVerifications.GetByIdAsync(verificationId);
                if (verification == null)
                {
                    return new Response<bool>(false, false, "Verification request not found.");
                }

                verification.Status = VerificationStatus.Rejected;
                verification.ReviewedBy = adminId;
                verification.ReviewedAt = DateTime.UtcNow;
                verification.RejectionReason = reason;
                verification.AdminNotes = notes;

                await _unitOfWork.IDVerifications.UpdateAsync(verification);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("ID verification {VerificationId} rejected by admin {AdminId}", verificationId, adminId);

                return new Response<bool>(true, true, "ID verification rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting ID verification {VerificationId}", verificationId);
                return new Response<bool>(false, false, "Failed to reject ID verification.");
            }
        }

        #endregion

        #region Verification Tier

        public async Task UpdateVerificationTierAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return;

                var oldTier = user.VerificationTier;
                var newTier = VerificationTier.Unverified;

                // Determine tier based on completed verifications
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

                if (newTier != oldTier)
                {
                    user.VerificationTier = newTier;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("User {UserId} verification tier updated from {OldTier} to {NewTier}",
                        userId, oldTier, newTier);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating verification tier for user {UserId}", userId);
            }
        }

        public async Task<VerificationTier> GetVerificationTierAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.VerificationTier ?? VerificationTier.Unverified;
        }

        #endregion

        /// <summary>
        /// Get complete verification status for a user
        /// </summary>
        public async Task<UserVerificationStatusDto> GetUserVerificationStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserVerificationStatusDto();
            }

            var status = new UserVerificationStatusDto
            {
                IsEmailVerified = user.EmailConfirmed,
                IsPhoneVerified = user.PhoneNumberConfirmed,
                IsIdVerified = user.IDVerified,
                CurrentTier = user.VerificationTier
            };

            status.CompletedVerifications = (status.IsEmailVerified ? 1 : 0) +
                                            (status.IsPhoneVerified ? 1 : 0) +
                                            (status.IsIdVerified ? 1 : 0);

            return status;
        }
    }
}
