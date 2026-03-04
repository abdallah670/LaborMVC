using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ILogger<FileUploadValidationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return FileValidationResult.Failure("No file uploaded.");
            }

            // Check file size
            if (file.Length > _settings.MaxFileSize)
            {
                var maxSizeMB = _settings.MaxFileSize / (1024 * 1024);
                return FileValidationResult.Failure($"File size exceeds maximum allowed size of {maxSizeMB}MB.");
            }

            // Sanitize file name
            var sanitizedFileName = SanitizeFileName(file.FileName);
            if (string.IsNullOrEmpty(sanitizedFileName))
            {
                return FileValidationResult.Failure("Invalid file name.");
            }

            // Check file extension
            if (!IsAllowedExtension(sanitizedFileName))
            {
                var allowedExts = string.Join(", ", _settings.AllowedExtensions);
                return FileValidationResult.Failure($"File type not allowed. Allowed types: {allowedExts}");
            }

            // Check MIME type
            if (!_settings.AllowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
            {
                _logger.LogWarning("File {FileName} has disallowed MIME type: {MimeType}",
                    sanitizedFileName, file.ContentType);
                return FileValidationResult.Failure("File MIME type not allowed.");
            }

            // Validate file signature
            if (_settings.ValidateFileSignature)
            {
                if (!await ValidateFileSignatureAsync(file))
                {
                    return FileValidationResult.Failure("File signature validation failed. File may be corrupted or have incorrect extension.");
                }
            }

            // Check for executable content
            if (_settings.BlockExecutables)
            {
                if (await IsExecutableFileAsync(file))
                {
                    _logger.LogWarning("Executable file upload attempted: {FileName}", sanitizedFileName);
                    return FileValidationResult.Failure("Executable files are not allowed.");
                }
            }

            // Scan for malicious content
            if (_settings.ScanForMaliciousContent)
            {
                if (!await ScanForMaliciousContentAsync(file))
                {
                    return FileValidationResult.Failure("File contains potentially malicious content.");
                }
            }

            return FileValidationResult.Success(sanitizedFileName, file.ContentType ?? "application/octet-stream");
        }

        public async Task<List<FileValidationResult>> ValidateFilesAsync(IEnumerable<IFormFile> files)
        {
            var results = new List<FileValidationResult>();
            foreach (var file in files)
            {
                results.Add(await ValidateFileAsync(file));
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
            
            // Block double extensions (e.g., file.jpg.exe)
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
            if (dangerousExtensions.Any(ext => fileNameWithoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Double extension detected in file: {FileName}", file.FileName);
                return false;
            }

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
    }
}
