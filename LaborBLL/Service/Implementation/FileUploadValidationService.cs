using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LaborBLL.Common;
using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for validating file uploads with comprehensive security checks
    /// </summary>
    public class FileUploadValidationService : IFileUploadValidationService
    {
        private readonly FileUploadSecuritySettings _settings;
        private readonly ILogger<FileUploadValidationService> _logger;
        private readonly IZipSecurityValidator? _zipValidator;
        private readonly IImageValidationService? _imageValidator;
        private readonly IUserUploadRateLimiter? _rateLimiter;
        private readonly IFileUploadAuditService? _auditService;
        private readonly IContentInspector? _contentInspector;

        // File signatures (magic numbers) for common file types
        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
        {
            [".jpg"] = new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".gif"] = new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            [".pdf"] = new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            [".zip"] = new List<byte[]>
            {
                new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // ZIP
                new byte[] { 0x50, 0x4B, 0x05, 0x06 }, // ZIP (empty)
                new byte[] { 0x50, 0x4B, 0x07, 0x08 }  // ZIP (spanned)
            },
            [".doc"] = new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
            [".docx"] = new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // DOCX is a ZIP
        };

        // Executable file signatures to block
        private static readonly List<byte[]> ExecutableSignatures = new()
        {
            new byte[] { 0x4D, 0x5A }, // EXE, DLL
            new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF (Linux)
            new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }, // Java class
            new byte[] { 0xCF, 0xFA, 0xED, 0xFE }, // macOS binary
        };

        public FileUploadValidationService(
            IOptions<FileUploadSecuritySettings> settings,
            ILogger<FileUploadValidationService> logger,
            IZipSecurityValidator? zipValidator = null,
            IImageValidationService? imageValidator = null,
            IUserUploadRateLimiter? rateLimiter = null,
            IFileUploadAuditService? auditService = null,
            IContentInspector? contentInspector = null)
        {
            _settings = settings.Value;
            _logger = logger;
            _zipValidator = zipValidator;
            _imageValidator = imageValidator;
            _rateLimiter = rateLimiter;
            _auditService = auditService;
            _contentInspector = contentInspector;
        }

        public async Task<FileValidationResult> ValidateFileAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                // Basic validation
                if (file == null || file.Length == 0)
                {
                    return await FailAndLogAsync(file, "NO_FILE", "No file uploaded.", userId, ipAddress, userAgent, correlationId, stopwatch);
                }

                // Rate limiting check
                if (_settings.EnableRateLimiting && _rateLimiter != null)
                {
                    var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(userId, ipAddress, file.Length);
                    if (!rateLimitResult.IsAllowed)
                    {
                        var ex = new UploadRateLimitExceededException(
                            rateLimitResult.ErrorMessage ?? "Rate limit exceeded",
                            rateLimitResult.RetryAfter,
                            rateLimitResult.HourlyUploads,
                            _settings.AllowedExtensions.Count > 0 ? _settings.AllowedExtensions.Count * 100 : 100,
                            userId,
                            ipAddress);

                        await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                        return FileValidationResult.Failure(rateLimitResult.ErrorMessage ?? "Rate limit exceeded.");
                    }
                }

                // Check file size
                if (file.Length > _settings.MaxFileSize)
                {
                    var maxSizeMB = _settings.MaxFileSize / (1024 * 1024);
                    return await FailAndLogAsync(file, "FILE_TOO_LARGE",
                        $"File size exceeds maximum allowed size of {maxSizeMB}MB.",
                        userId, ipAddress, userAgent, correlationId, stopwatch);
                }

                // Sanitize file name
                var sanitizedFileName = SanitizeFileName(file.FileName);
                if (string.IsNullOrEmpty(sanitizedFileName))
                {
                    return await FailAndLogAsync(file, "INVALID_FILENAME", "Invalid file name.",
                        userId, ipAddress, userAgent, correlationId, stopwatch);
                }

                // Check for null bytes in filename (null byte injection)
                if (file.FileName.Contains('\0'))
                {
                    var ex = new FileValidationException("Null byte detected in filename",
                        FileUploadViolationType.NullByteInjection, file.FileName, userId, ipAddress);
                    await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                    return FileValidationResult.Failure("Invalid characters in filename.");
                }

                // Check file extension
                if (!IsAllowedExtension(sanitizedFileName))
                {
                    var allowedExts = string.Join(", ", _settings.AllowedExtensions);
                    return await FailAndLogAsync(file, "EXTENSION_NOT_ALLOWED",
                        $"File type not allowed. Allowed types: {allowedExts}",
                        userId, ipAddress, userAgent, correlationId, stopwatch);
                }

                // Check for double extensions
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedFileName);
                var dangerousExtensions = new[] { ".exe", ".dll", ".bat", ".cmd", ".sh", ".php", ".jsp", ".asp", ".aspx" };
                if (dangerousExtensions.Any(ext => fileNameWithoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    var detectedExts = string.Join(", ", dangerousExtensions.Where(ext => fileNameWithoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
                    var ex = new DoubleExtensionDetectedException(
                        "Double extension detected - potential spoofing attempt",
                        detectedExts, file.FileName, userId, ipAddress);
                    await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                    return FileValidationResult.Failure("Double file extensions are not allowed.");
                }

                // Check MIME type
                if (!_settings.AllowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant() ?? ""))
                {
                    _logger.LogWarning("File {FileName} has disallowed MIME type: {MimeType}. User: {UserId}, IP: {IpAddress}",
                        sanitizedFileName, file.ContentType, userId, ipAddress);
                    return await FailAndLogAsync(file, "MIME_TYPE_NOT_ALLOWED", "File MIME type not allowed.",
                        userId, ipAddress, userAgent, correlationId, stopwatch);
                }

                // Validate file signature
                if (_settings.ValidateFileSignature)
                {
                    if (!await ValidateFileSignatureAsync(file))
                    {
                        return await FailAndLogAsync(file, "SIGNATURE_MISMATCH",
                            "File signature validation failed. File may be corrupted or have incorrect extension.",
                            userId, ipAddress, userAgent, correlationId, stopwatch);
                    }
                }

                // Check for executable content
                if (_settings.BlockExecutables)
                {
                    if (await IsExecutableFileAsync(file))
                    {
                        _logger.LogWarning("Executable file upload attempted: {FileName}. User: {UserId}, IP: {IpAddress}",
                            sanitizedFileName, userId, ipAddress);
                        var ex = new FileValidationException("Executable file detected",
                            FileUploadViolationType.ExecutableDetected, file.FileName, userId, ipAddress);
                        await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                        return FileValidationResult.Failure("Executable files are not allowed.");
                    }
                }

                // ZIP bomb protection
                if (_settings.ValidateZipFiles && _zipValidator != null)
                {
                    try
                    {
                        await _zipValidator.ValidateZipFileAsync(file, userId, ipAddress);
                    }
                    catch (ZipBombDetectedException ex)
                    {
                        await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                        return FileValidationResult.Failure("ZIP file failed security validation - potential zip bomb detected.");
                    }
                }

                // Image dimension validation
                int? imageWidth = null;
                int? imageHeight = null;
                if (_settings.ValidateImageDimensions && _imageValidator != null)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp")
                    {
                        try
                        {
                            var imageResult = await _imageValidator.ValidateImageAsync(file, userId, ipAddress);
                            if (!imageResult.IsValid)
                            {
                                return await FailAndLogAsync(file, "IMAGE_VALIDATION_FAILED",
                                    imageResult.ErrorMessage ?? "Image validation failed.",
                                    userId, ipAddress, userAgent, correlationId, stopwatch);
                            }
                            imageWidth = imageResult.Width;
                            imageHeight = imageResult.Height;
                        }
                        catch (ImageDimensionExceededException ex)
                        {
                            await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                            return FileValidationResult.Failure($"Image dimensions exceed maximum allowed limits.");
                        }
                        catch (PixelFloodAttackException ex)
                        {
                            await LogBlockedAsync(file, ex, userId, ipAddress, userAgent, correlationId, stopwatch);
                            return FileValidationResult.Failure("Image pixel count too high - potential pixel flood attack.");
                        }
                    }
                }

                // Deep content inspection
                List<string>? detectedThreats = null;
                if (_settings.EnableContentInspection && _contentInspector != null)
                {
                    try
                    {
                        var inspectionResult = await _contentInspector.InspectAsync(file, userId, ipAddress);
                        if (!inspectionResult.IsClean)
                        {
                            detectedThreats = inspectionResult.DetectedThreats;
                            var ex = new MaliciousContentDetectedException(
                                "Malicious content detected during deep inspection",
                                inspectionResult.DetectedThreats,
                                file.FileName, userId, ipAddress);
                            await LogBlockedAsync(file, ex, detectedThreats, userId, ipAddress, userAgent, correlationId, stopwatch);
                            return FileValidationResult.Failure($"Security threat detected: {string.Join(", ", inspectionResult.DetectedThreats)}");
                        }
                    }
                    catch (PolyglotFileDetectedException ex)
                    {
                        await LogBlockedAsync(file, ex, ex.ValidFormats, userId, ipAddress, userAgent, correlationId, stopwatch);
                        return FileValidationResult.Failure("Polyglot file detected - file is valid as multiple formats.");
                    }
                    catch (VirusDetectedException ex)
                    {
                        await LogBlockedAsync(file, ex, new List<string> { $"{ex.VirusName} ({ex.ScanEngine})" },
                            userId, ipAddress, userAgent, correlationId, stopwatch);
                        return FileValidationResult.Failure("Virus or malware detected.");
                    }
                }

                // Scan for malicious content (original implementation)
                if (_settings.ScanForMaliciousContent)
                {
                    if (!await ScanForMaliciousContentAsync(file))
                    {
                        return await FailAndLogAsync(file, "MALICIOUS_CONTENT",
                            "File contains potentially malicious content.",
                            userId, ipAddress, userAgent, correlationId, stopwatch);
                    }
                }

                // Calculate file hash
                string? fileHash = null;
                if (_settings.EnableAuditLogging && _auditService != null)
                {
                    fileHash = await CalculateFileHashAsync(file);
                }

                // Record successful upload for rate limiting
                if (_settings.EnableRateLimiting && _rateLimiter != null)
                {
                    await _rateLimiter.RecordUploadAsync(userId, ipAddress, file.Length);
                }

                // Log successful upload
                stopwatch.Stop();
                if (_settings.EnableAuditLogging && _auditService != null)
                {
                    await _auditService.LogUploadSuccessAsync(
                        file, sanitizedFileName, file.ContentType, fileHash,
                        imageWidth, imageHeight, stopwatch.ElapsedMilliseconds,
                        userId, null, ipAddress, userAgent, correlationId);
                }

                _logger.LogInformation(
                    "File {FileName} validated successfully for user {UserId} from IP {IpAddress} in {ElapsedMs}ms",
                    sanitizedFileName, userId, ipAddress, stopwatch.ElapsedMilliseconds);

                return FileValidationResult.Success(sanitizedFileName, file.ContentType ?? "application/octet-stream");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error validating file {FileName}", file?.FileName);
                return FileValidationResult.Failure("An error occurred during file validation.");
            }
        }

        public async Task<List<FileValidationResult>> ValidateFilesAsync(IEnumerable<IFormFile> files, string? userId = null, string? ipAddress = null, string? userAgent = null)
        {
            var results = new List<FileValidationResult>();
            foreach (var file in files)
            {
                results.Add(await ValidateFileAsync(file, userId, ipAddress, userAgent));
            }
            return results;
        }

        public bool IsAllowedExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.');
            return _settings.AllowedExtensions.Contains(extension);
        }

        public async Task<bool> ValidateFileSignatureAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!FileSignatures.ContainsKey(extension))
            {
                // Unknown extension, skip signature validation
                return true;
            }

            var expectedSignatures = FileSignatures[extension];

            using var stream = file.OpenReadStream();
            var headerBytes = new byte[expectedSignatures.Max(s => s.Length)];
            await stream.ReadAsync(headerBytes.AsMemory(0, headerBytes.Length));

            return expectedSignatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        public async Task<bool> ScanForMaliciousContentAsync(IFormFile file)
        {
            // Check for common malicious patterns in text-based files
            var dangerousExtensions = new[] { ".exe", ".dll", ".bat", ".cmd", ".sh", ".php", ".jsp", ".asp", ".aspx" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // For text-based files, scan for script tags and dangerous content
            if (extension == ".txt" || extension == ".html" || extension == ".xml")
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();

                var dangerousPatterns = new[]
                {
                    "<script",
                    "javascript:",
                    "onload=",
                    "onerror=",
                    "eval(",
                    "document.write",
                    "<?php",
                    "<%",
                    "<jsp:",
                    "<asp:"
                };

                if (dangerousPatterns.Any(pattern =>
                    content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _logger.LogWarning("Potentially malicious content detected in file: {FileName}", file.FileName);
                    return false;
                }
            }

            return true;
        }

        public string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // Get just the file name (no path)
            var name = Path.GetFileName(fileName);

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            name = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());

            // Remove leading/trailing dots and spaces
            name = name.Trim('.', ' ');

            // Limit length
            if (name.Length > 200)
            {
                var extension = Path.GetExtension(name);
                name = name.Substring(0, 200 - extension.Length) + extension;
            }

            // If empty after sanitization, generate a name
            if (string.IsNullOrEmpty(name))
            {
                name = $"file_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            }

            return name;
        }

        private async Task<bool> IsExecutableFileAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[4];
            var bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, 4));

            if (bytesRead < 2)
                return false;

            return ExecutableSignatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        private async Task<string?> CalculateFileHashAsync(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var sha256 = SHA256.Create();
                var hash = await sha256.ComputeHashAsync(stream);
                return Convert.ToHexString(hash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate file hash for {FileName}", file.FileName);
                return null;
            }
        }

        private async Task<FileValidationResult> FailAndLogAsync(
            IFormFile? file,
            string errorCode,
            string errorMessage,
            string? userId,
            string? ipAddress,
            string? userAgent,
            string correlationId,
            Stopwatch stopwatch)
        {
            stopwatch.Stop();

            if (file != null && _settings.EnableAuditLogging && _auditService != null)
            {
                await _auditService.LogUploadFailureAsync(
                    file, errorCode, errorMessage,
                    stopwatch.ElapsedMilliseconds,
                    userId, null, ipAddress, userAgent, correlationId);
            }

            return FileValidationResult.Failure(errorMessage);
        }

        private async Task LogBlockedAsync(
            IFormFile file,
            FileUploadSecurityException exception,
            string? userId,
            string? ipAddress,
            string? userAgent,
            string correlationId,
            Stopwatch stopwatch)
        {
            stopwatch.Stop();

            if (_settings.EnableAuditLogging && _auditService != null)
            {
                await _auditService.LogUploadBlockedAsync(
                    file, exception, null,
                    stopwatch.ElapsedMilliseconds,
                    userId, null, ipAddress, userAgent, correlationId);
            }
        }

        private async Task LogBlockedAsync(
            IFormFile file,
            FileUploadSecurityException exception,
            List<string>? detectedThreats,
            string? userId,
            string? ipAddress,
            string? userAgent,
            string correlationId,
            Stopwatch stopwatch)
        {
            stopwatch.Stop();

            if (_settings.EnableAuditLogging && _auditService != null)
            {
                await _auditService.LogUploadBlockedAsync(
                    file, exception, detectedThreats,
                    stopwatch.ElapsedMilliseconds,
                    userId, null, ipAddress, userAgent, correlationId);
            }
        }
    }
}
