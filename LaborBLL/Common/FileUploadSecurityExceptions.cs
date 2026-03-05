using System;

namespace LaborBLL.Common
{
    /// <summary>
    /// Types of file upload security violations
    /// </summary>
    public enum FileUploadViolationType
    {
        Unknown,
        InvalidExtension,
        InvalidMimeType,
        InvalidFileSignature,
        ExecutableDetected,
        MaliciousContent,
        VirusDetected,
        ZipBomb,
        ImageDimensionExceeded,
        PixelFloodAttack,
        PolyglotFile,
        EncodedPayload,
        RateLimitExceeded,
        QuotaExceeded,
        PathTraversal,
        DoubleExtension,
        NullByteInjection
    }

    /// <summary>
    /// Base exception for all file upload security violations
    /// </summary>
    public abstract class FileUploadSecurityException : Exception
    {
        /// <summary>
        /// Unique error code for the violation type
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Type of security violation
        /// </summary>
        public FileUploadViolationType ViolationType { get; }

        /// <summary>
        /// Name of the file that caused the violation
        /// </summary>
        public string? FileName { get; }

        /// <summary>
        /// ID of the user who attempted the upload
        /// </summary>
        public string? UserId { get; }

        /// <summary>
        /// Timestamp when the violation occurred
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// IP address of the request
        /// </summary>
        public string? IpAddress { get; }

        /// <summary>
        /// Risk severity level
        /// </summary>
        public RiskLevel RiskLevel { get; }

        protected FileUploadSecurityException(
            string message,
            string errorCode,
            FileUploadViolationType violationType,
            RiskLevel riskLevel = RiskLevel.Medium,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            ViolationType = violationType;
            RiskLevel = riskLevel;
            FileName = fileName;
            UserId = userId;
            IpAddress = ipAddress;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Risk severity levels for security violations
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Exception thrown when file validation fails
    /// </summary>
    public class FileValidationException : FileUploadSecurityException
    {
        public FileValidationException(
            string message,
            FileUploadViolationType violationType = FileUploadViolationType.Unknown,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "FILE_VALIDATION_ERROR",
                violationType,
                RiskLevel.Medium,
                fileName,
                userId,
                ipAddress)
        {
        }
    }

    /// <summary>
    /// Exception thrown when malicious content is detected in a file
    /// </summary>
    public class MaliciousContentDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// List of detected threats
        /// </summary>
        public List<string> DetectedThreats { get; }

        public MaliciousContentDetectedException(
            string message,
            List<string>? detectedThreats = null,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "MALICIOUS_CONTENT_DETECTED",
                FileUploadViolationType.MaliciousContent,
                RiskLevel.High,
                fileName,
                userId,
                ipAddress)
        {
            DetectedThreats = detectedThreats ?? new List<string>();
        }
    }

    /// <summary>
    /// Exception thrown when a virus or malware is detected
    /// </summary>
    public class VirusDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// Name of the virus/malware detected
        /// </summary>
        public string? VirusName { get; }

        /// <summary>
        /// Scan engine that detected the virus
        /// </summary>
        public string? ScanEngine { get; }

        public VirusDetectedException(
            string message,
            string? virusName = null,
            string? scanEngine = null,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "VIRUS_DETECTED",
                FileUploadViolationType.VirusDetected,
                RiskLevel.Critical,
                fileName,
                userId,
                ipAddress)
        {
            VirusName = virusName;
            ScanEngine = scanEngine;
        }
    }

    /// <summary>
    /// Exception thrown when a zip bomb is detected
    /// </summary>
    public class ZipBombDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// Compression ratio detected
        /// </summary>
        public double CompressionRatio { get; }

        /// <summary>
        /// Estimated decompressed size
        /// </summary>
        public long EstimatedDecompressedSize { get; }

        public ZipBombDetectedException(
            string message,
            double compressionRatio = 0,
            long estimatedDecompressedSize = 0,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "ZIP_BOMB_DETECTED",
                FileUploadViolationType.ZipBomb,
                RiskLevel.Critical,
                fileName,
                userId,
                ipAddress)
        {
            CompressionRatio = compressionRatio;
            EstimatedDecompressedSize = estimatedDecompressedSize;
        }
    }

    /// <summary>
    /// Exception thrown when image dimensions exceed limits
    /// </summary>
    public class ImageDimensionExceededException : FileUploadSecurityException
    {
        /// <summary>
        /// Actual width of the image
        /// </summary>
        public int ActualWidth { get; }

        /// <summary>
        /// Actual height of the image
        /// </summary>
        public int ActualHeight { get; }

        /// <summary>
        /// Maximum allowed width
        /// </summary>
        public int MaxAllowedWidth { get; }

        /// <summary>
        /// Maximum allowed height
        /// </summary>
        public int MaxAllowedHeight { get; }

        public ImageDimensionExceededException(
            string message,
            int actualWidth,
            int actualHeight,
            int maxAllowedWidth,
            int maxAllowedHeight,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "IMAGE_DIMENSION_EXCEEDED",
                FileUploadViolationType.ImageDimensionExceeded,
                RiskLevel.High,
                fileName,
                userId,
                ipAddress)
        {
            ActualWidth = actualWidth;
            ActualHeight = actualHeight;
            MaxAllowedWidth = maxAllowedWidth;
            MaxAllowedHeight = maxAllowedHeight;
        }
    }

    /// <summary>
    /// Exception thrown when a pixel flood attack is detected
    /// </summary>
    public class PixelFloodAttackException : FileUploadSecurityException
    {
        /// <summary>
        /// Total pixel count
        /// </summary>
        public long TotalPixels { get; }

        /// <summary>
        /// Maximum allowed pixels
        /// </summary>
        public long MaxAllowedPixels { get; }

        public PixelFloodAttackException(
            string message,
            long totalPixels,
            long maxAllowedPixels,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "PIXEL_FLOOD_ATTACK",
                FileUploadViolationType.PixelFloodAttack,
                RiskLevel.Critical,
                fileName,
                userId,
                ipAddress)
        {
            TotalPixels = totalPixels;
            MaxAllowedPixels = maxAllowedPixels;
        }
    }

    /// <summary>
    /// Exception thrown when a polyglot file is detected
    /// </summary>
    public class PolyglotFileDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// List of formats the file is valid as
        /// </summary>
        public List<string> ValidFormats { get; }

        public PolyglotFileDetectedException(
            string message,
            List<string>? validFormats = null,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "POLYGLOT_FILE_DETECTED",
                FileUploadViolationType.PolyglotFile,
                RiskLevel.High,
                fileName,
                userId,
                ipAddress)
        {
            ValidFormats = validFormats ?? new List<string>();
        }
    }

    /// <summary>
    /// Exception thrown when an encoded payload is detected
    /// </summary>
    public class EncodedPayloadDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// Type of encoding detected
        /// </summary>
        public string? EncodingType { get; }

        public EncodedPayloadDetectedException(
            string message,
            string? encodingType = null,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "ENCODED_PAYLOAD_DETECTED",
                FileUploadViolationType.EncodedPayload,
                RiskLevel.High,
                fileName,
                userId,
                ipAddress)
        {
            EncodingType = encodingType;
        }
    }

    /// <summary>
    /// Exception thrown when upload rate limit is exceeded
    /// </summary>
    public class UploadRateLimitExceededException : FileUploadSecurityException
    {
        /// <summary>
        /// Time when the rate limit will reset
        /// </summary>
        public DateTime? RetryAfter { get; }

        /// <summary>
        /// Number of uploads attempted
        /// </summary>
        public int AttemptedUploads { get; }

        /// <summary>
        /// Maximum allowed uploads
        /// </summary>
        public int MaxAllowedUploads { get; }

        public UploadRateLimitExceededException(
            string message,
            DateTime? retryAfter = null,
            int attemptedUploads = 0,
            int maxAllowedUploads = 0,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "RATE_LIMIT_EXCEEDED",
                FileUploadViolationType.RateLimitExceeded,
                RiskLevel.Medium,
                null,
                userId,
                ipAddress)
        {
            RetryAfter = retryAfter;
            AttemptedUploads = attemptedUploads;
            MaxAllowedUploads = maxAllowedUploads;
        }
    }

    /// <summary>
    /// Exception thrown when user's storage quota is exceeded
    /// </summary>
    public class UploadQuotaExceededException : FileUploadSecurityException
    {
        /// <summary>
        /// Current storage used in bytes
        /// </summary>
        public long CurrentUsage { get; }

        /// <summary>
        /// Maximum allowed storage in bytes
        /// </summary>
        public long MaxQuota { get; }

        public UploadQuotaExceededException(
            string message,
            long currentUsage,
            long maxQuota,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "QUOTA_EXCEEDED",
                FileUploadViolationType.QuotaExceeded,
                RiskLevel.Medium,
                null,
                userId,
                ipAddress)
        {
            CurrentUsage = currentUsage;
            MaxQuota = maxQuota;
        }
    }

    /// <summary>
    /// Exception thrown when path traversal is detected
    /// </summary>
    public class PathTraversalDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// The detected path pattern
        /// </summary>
        public string? DetectedPath { get; }

        public PathTraversalDetectedException(
            string message,
            string? detectedPath = null,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "PATH_TRAVERSAL_DETECTED",
                FileUploadViolationType.PathTraversal,
                RiskLevel.High,
                fileName,
                userId,
                ipAddress)
        {
            DetectedPath = detectedPath;
        }
    }

    /// <summary>
    /// Exception thrown when double extension is detected
    /// </summary>
    public class DoubleExtensionDetectedException : FileUploadSecurityException
    {
        /// <summary>
        /// The detected extensions
        /// </summary>
        public string? DetectedExtensions { get; }

        public DoubleExtensionDetectedException(
            string message,
            string? detectedExtensions = null,
            string? fileName = null,
            string? userId = null,
            string? ipAddress = null)
            : base(
                message,
                "DOUBLE_EXTENSION_DETECTED",
                FileUploadViolationType.DoubleExtension,
                RiskLevel.High,
                fileName,
                userId,
                ipAddress)
        {
            DetectedExtensions = detectedExtensions;
        }
    }
}
