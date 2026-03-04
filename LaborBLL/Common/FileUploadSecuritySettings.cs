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
        /// Maximum total upload size per request in bytes (default: 50MB)
        /// </summary>
        public long MaxTotalUploadSizePerRequest { get; set; } = 50 * 1024 * 1024; // 50MB

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

        /// <summary>
        /// Whether to validate ZIP files for zip bombs
        /// </summary>
        public bool ValidateZipFiles { get; set; } = true;

        /// <summary>
        /// Whether to validate image dimensions
        /// </summary>
        public bool ValidateImageDimensions { get; set; } = true;

        /// <summary>
        /// Whether to check for pixel flood attacks
        /// </summary>
        public bool CheckPixelFlood { get; set; } = true;

        /// <summary>
        /// Whether to enable audit logging
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;

        /// <summary>
        /// Whether to enable rate limiting
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;

        /// <summary>
        /// Whether to perform deep content inspection
        /// </summary>
        public bool EnableContentInspection { get; set; } = true;

        /// <summary>
        /// Whether to detect polyglot files
        /// </summary>
        public bool DetectPolyglotFiles { get; set; } = true;

        /// <summary>
        /// Whether to detect encoded payloads
        /// </summary>
        public bool DetectEncodedPayloads { get; set; } = true;
    }

    /// <summary>
    /// Settings for ZIP security validation
    /// </summary>
    public class ZipValidationSettings
    {
        /// <summary>
        /// Maximum decompressed size in bytes (default: 100MB)
        /// </summary>
        public long MaxDecompressedSize { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Maximum number of files in the archive
        /// </summary>
        public int MaxFileCount { get; set; } = 1000;

        /// <summary>
        /// Maximum compression ratio (decompressed/compressed)
        /// </summary>
        public int MaxCompressionRatio { get; set; } = 100;

        /// <summary>
        /// Maximum nested archive depth
        /// </summary>
        public int MaxNestedLevel { get; set; } = 3;

        /// <summary>
        /// Minimum file size to check for zip bomb
        /// </summary>
        public long MinFileSizeToCheck { get; set; } = 1024; // 1KB
    }

    /// <summary>
    /// Settings for image validation
    /// </summary>
    public class ImageValidationSettings
    {
        /// <summary>
        /// Maximum allowed width in pixels
        /// </summary>
        public int MaxWidth { get; set; } = 10000;

        /// <summary>
        /// Maximum allowed height in pixels
        /// </summary>
        public int MaxHeight { get; set; } = 10000;

        /// <summary>
        /// Maximum total pixel count (width * height) - prevents pixel flood attacks
        /// </summary>
        public long MaxPixels { get; set; } = 100_000_000; // 100MP

        /// <summary>
        /// Maximum file size for image validation
        /// </summary>
        public long MaxFileSizeForValidation { get; set; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Allowed image formats
        /// </summary>
        public List<string> AllowedFormats { get; set; } = new()
        {
            "jpeg", "jpg", "png", "gif", "bmp", "webp"
        };

        /// <summary>
        /// Whether to validate image dimensions
        /// </summary>
        public bool ValidateDimensions { get; set; } = true;

        /// <summary>
        /// Whether to check for pixel flood attacks
        /// </summary>
        public bool CheckPixelFlood { get; set; } = true;
    }

    /// <summary>
    /// Settings for upload rate limiting
    /// </summary>
    public class UploadRateLimitSettings
    {
        /// <summary>
        /// Maximum number of files a user can upload per hour
        /// </summary>
        public int MaxFilesPerHour { get; set; } = 100;

        /// <summary>
        /// Maximum number of files a user can upload per day
        /// </summary>
        public int MaxFilesPerDay { get; set; } = 1000;

        /// <summary>
        /// Maximum total storage per user in MB
        /// </summary>
        public int MaxStorageMBPerUser { get; set; } = 500;

        /// <summary>
        /// Maximum uploads per minute (burst protection)
        /// </summary>
        public int MaxFilesPerMinute { get; set; } = 10;

        /// <summary>
        /// Whether to track by IP address for anonymous users
        /// </summary>
        public bool TrackByIpForAnonymous { get; set; } = true;

        /// <summary>
        /// Cooldown period after rate limit is hit (in minutes)
        /// </summary>
        public int CooldownMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Settings for file upload audit logging
    /// </summary>
    public class FileUploadAuditSettings
    {
        /// <summary>
        /// Whether audit logging is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether to log successful uploads
        /// </summary>
        public bool LogSuccessfulUploads { get; set; } = true;

        /// <summary>
        /// Whether to log failed uploads
        /// </summary>
        public bool LogFailedAttempts { get; set; } = true;

        /// <summary>
        /// Data retention period for audit logs
        /// </summary>
        public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(365);

        /// <summary>
        /// Whether to calculate file hash for audit
        /// </summary>
        public bool CalculateFileHash { get; set; } = true;
    }
}
