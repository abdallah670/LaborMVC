using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    /// <summary>
    /// Controller for handling file uploads
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IImageProcessingService _imageProcessing;
        private readonly IStorageService _storage;
        private readonly IFileUploadValidationService _fileValidation;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<UploadController> _logger;

        public UploadController(
            IImageProcessingService imageProcessing,
            IStorageService storage,
            IFileUploadValidationService fileValidation,
            UserManager<AppUser> userManager,
            ILogger<UploadController> logger)
        {
            _imageProcessing = imageProcessing;
            _storage = storage;
            _fileValidation = fileValidation;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Upload and process profile picture
        /// </summary>
        [HttpPost("profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded." });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            try
            {
                // Step 1: File security validation
                var validationResult = await _fileValidation.ValidateFileAsync(
                    file, userId, Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("File validation failed for user {UserId}: {Reason}",
                        userId, validationResult.ErrorMessage);
                    return BadRequest(new { success = false, message = validationResult.ErrorMessage });
                }

                // Step 2: Image-specific validation and processing
                var imageValidation = await _imageProcessing.ValidateImageAsync(file);
                if (!imageValidation.IsValid)
                {
                    return BadRequest(new { success = false, message = imageValidation.ErrorMessage });
                }

                // Step 3: Process image (crop to square, generate multiple sizes)
                var processedImage = await _imageProcessing.ProcessProfilePictureAsync(file);

                // Step 4: Generate unique file names
                var baseFileName = _storage.GenerateUniqueFileName("profile.jpg");
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
                
                var thumbnailName = $"{fileNameWithoutExt}_thumb.jpg";
                var mediumName = $"{fileNameWithoutExt}_medium.jpg";
                var fullName = $"{fileNameWithoutExt}_full.jpg";

                // Step 5: Upload to storage
                var thumbnailUrl = await _storage.UploadAsync(
                    processedImage.Thumbnail, thumbnailName, "profile-pictures");
                var mediumUrl = await _storage.UploadAsync(
                    processedImage.Medium, mediumName, "profile-pictures");
                var fullUrl = await _storage.UploadAsync(
                    processedImage.Full, fullName, "profile-pictures");

                // Step 6: Update user record
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    await _storage.DeleteAsync(user.ProfilePictureUrl);
                }

                // Save medium size as primary profile picture
                user.ProfilePictureUrl = mediumUrl;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user profile picture for user {UserId}", userId);
                    return StatusCode(500, new { success = false, message = "Failed to update profile." });
                }

                _logger.LogInformation("Profile picture uploaded successfully for user {UserId}", userId);

                return Ok(new
                {
                    success = true,
                    message = "Profile picture uploaded successfully.",
                    data = new
                    {
                        thumbnailUrl,
                        mediumUrl,
                        fullUrl,
                        originalWidth = processedImage.OriginalWidth,
                        originalHeight = processedImage.OriginalHeight,
                        format = processedImage.Format
                    }
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid image file uploaded by user {UserId}", userId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing the image." });
            }
        }

        /// <summary>
        /// Delete user's profile picture
        /// </summary>
        [HttpDelete("profile-picture")]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    return BadRequest(new { success = false, message = "No profile picture to delete." });
                }

                // Delete from storage
                await _storage.DeleteAsync(user.ProfilePictureUrl);

                // Update user record
                user.ProfilePictureUrl = null;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Profile picture deleted for user {UserId}", userId);

                return Ok(new { success = true, message = "Profile picture deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile picture for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the profile picture." });
            }
        }

        /// <summary>
        /// Validate image without uploading (for preview)
        /// </summary>
        [HttpPost("validate-image")]
        public async Task<IActionResult> ValidateImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded." });
            }

            var validation = await _imageProcessing.ValidateImageAsync(file);
            
            if (!validation.IsValid)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = validation.ErrorMessage 
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    width = validation.Width,
                    height = validation.Height,
                    format = validation.Format
                }
            });
        }

        /// <summary>
        /// Upload ID verification documents (front, back, selfie)
        /// </summary>
        [HttpPost("id-documents")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadIdDocuments(
            IFormFile frontDocument,
            IFormFile? backDocument = null,
            IFormFile? selfie = null)
        {
            if (frontDocument == null || frontDocument.Length == 0)
            {
                return BadRequest(new { success = false, message = "Front document is required." });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            try
            {
                var frontUrl = await UploadIdDocumentFile(frontDocument, userId, "front");
                string? backUrl = null;
                string? selfieUrl = null;

                if (backDocument != null && backDocument.Length > 0)
                {
                    backUrl = await UploadIdDocumentFile(backDocument, userId, "back");
                }

                if (selfie != null && selfie.Length > 0)
                {
                    selfieUrl = await UploadIdDocumentFile(selfie, userId, "selfie");
                }

                _logger.LogInformation("ID documents uploaded successfully for user {UserId}", userId);

                return Ok(new
                {
                    success = true,
                    message = "Documents uploaded successfully.",
                    data = new
                    {
                        frontUrl,
                        backUrl,
                        selfieUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading ID documents for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "An error occurred while uploading documents." });
            }
        }

        private async Task<string> UploadIdDocumentFile(IFormFile file, string userId, string documentType)
        {
            // Validate file
            var validationResult = await _fileValidation.ValidateFileAsync(
                file, userId, Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent);

            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.ErrorMessage);
            }

            // Generate unique filename
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"{userId}_{documentType}_{timestamp}{Path.GetExtension(file.FileName)}";

            // Read file to byte array
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Upload to storage
            var url = await _storage.UploadAsync(fileBytes, fileName, "id-documents");

            return url;
        }
    }
}
