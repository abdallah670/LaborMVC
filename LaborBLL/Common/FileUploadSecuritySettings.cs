namespace LaborBLL.Common
{
    /// <summary>
    /// Configuration settings for file upload security
    /// </summary>
    public class FileUploadSecuritySettings
    {
        /// <summary>
        /// Maximum file size in bytes (default: 10MB)
        /// </summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Allowed file extensions (lowercase, without dot)
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new()
        {
            "jpg", "jpeg", "png", "gif", "pdf", "doc", "docx", "txt", "zip"
        };

        /// <summary>
        /// Allowed MIME types for file upload
        /// </summary>
        public List<string> AllowedMimeTypes { get; set; } = new()
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain",
            "application/zip",
            "application/x-zip-compressed"
        };

        /// <summary>
        /// Whether to validate file signature (magic numbers)
        /// </summary>
        public bool ValidateFileSignature { get; set; } = true;

        /// <summary>
        /// Whether to scan for embedded scripts/content
        /// </summary>
        public bool ScanForMaliciousContent { get; set; } = true;

        /// <summary>
        /// Block executable file types
        /// </summary>
        public bool BlockExecutables { get; set; } = true;
    }
}
