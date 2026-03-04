using Microsoft.AspNetCore.Http;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Result of file upload validation
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SanitizedFileName { get; set; }
        public string? DetectedMimeType { get; set; }

        public static FileValidationResult Success(string sanitizedFileName, string detectedMimeType)
        {
            return new FileValidationResult
            {
                IsValid = true,
                SanitizedFileName = sanitizedFileName,
                DetectedMimeType = detectedMimeType
            };
        }

        public static FileValidationResult Failure(string errorMessage)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Service for validating file uploads for security
    /// </summary>
    public interface IFileUploadValidationService
    {
        /// <summary>
        /// Validate a single file upload with security context
        /// </summary>
        Task<FileValidationResult> ValidateFileAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Validate multiple file uploads
        /// </summary>
        Task<List<FileValidationResult>> ValidateFilesAsync(
            IEnumerable<IFormFile> files,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Check if file extension is allowed
        /// </summary>
        bool IsAllowedExtension(string fileName);

        /// <summary>
        /// Validate file signature (magic numbers)
        /// </summary>
        Task<bool> ValidateFileSignatureAsync(IFormFile file);

        /// <summary>
        /// Scan file content for malicious patterns
        /// </summary>
        Task<bool> ScanForMaliciousContentAsync(IFormFile file);

        /// <summary>
        /// Sanitize file name to prevent path traversal attacks
        /// </summary>
        string SanitizeFileName(string fileName);
    }
}
