using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaborBLL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for detecting and preventing zip bomb attacks
    /// </summary>
    public interface IZipSecurityValidator
    {
        /// <summary>
        /// Validates a ZIP file for zip bomb attacks
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <param name="userId">Optional user ID for logging</param>
        /// <param name="ipAddress">Optional IP address for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if safe, false if zip bomb detected</returns>
        Task<bool> ValidateZipFileAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file is a ZIP archive
        /// </summary>
        Task<bool> IsZipFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Validates ZIP files to prevent zip bomb attacks
    /// </summary>
    public class ZipSecurityValidator : IZipSecurityValidator
    {
        private readonly ZipValidationSettings _settings;
        private readonly ILogger<ZipSecurityValidator> _logger;

        // ZIP file signatures
        private static readonly byte[][] ZipSignatures = new[]
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // ZIP
            new byte[] { 0x50, 0x4B, 0x05, 0x06 }, // ZIP (empty)
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }  // ZIP (spanned)
        };

        public ZipSecurityValidator(
            IOptions<ZipValidationSettings> settings,
            ILogger<ZipSecurityValidator> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> ValidateZipFileAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return true; // Nothing to validate

            if (!await IsZipFileAsync(file, cancellationToken))
                return true; // Not a zip file

            // Skip small files
            if (file.Length < _settings.MinFileSizeToCheck)
                return true;

            try
            {
                using var stream = file.OpenReadStream();
                return await ValidateZipStreamAsync(
                    stream,
                    file.FileName,
                    file.Length,
                    userId,
                    ipAddress,
                    cancellationToken);
            }
            catch (ZipBombDetectedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ZIP file {FileName}", file.FileName);
                // Fail safe - block the file if we can't validate it
                throw new ZipBombDetectedException(
                    "Unable to validate ZIP file - potential security risk",
                    fileName: file.FileName,
                    userId: userId,
                    ipAddress: ipAddress);
            }
        }

        public async Task<bool> IsZipFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length < 4)
                return false;

            using var stream = file.OpenReadStream();
            var header = new byte[4];
            await stream.ReadAsync(header.AsMemory(0, 4), cancellationToken);

            return ZipSignatures.Any(signature =>
                header.Take(signature.Length).SequenceEqual(signature));
        }

        private async Task<bool> ValidateZipStreamAsync(
            Stream stream,
            string fileName,
            long compressedSize,
            string? userId,
            string? ipAddress,
            CancellationToken cancellationToken,
            int nestingLevel = 0)
        {
            if (nestingLevel > _settings.MaxNestedLevel)
            {
                _logger.LogWarning(
                    "Nested archive depth exceeded for file {FileName}. User: {UserId}, IP: {IpAddress}",
                    fileName, userId, ipAddress);

                throw new ZipBombDetectedException(
                    $"Archive nesting level exceeds maximum allowed ({_settings.MaxNestedLevel})",
                    fileName: fileName,
                    userId: userId,
                    ipAddress: ipAddress);
            }

            long totalUncompressedSize = 0;
            int fileCount = 0;

            try
            {
                using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

                foreach (var entry in zipArchive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    fileCount++;

                    // Check file count limit
                    if (fileCount > _settings.MaxFileCount)
                    {
                        _logger.LogWarning(
                            "File count limit exceeded in ZIP {FileName}. Count: {Count}, User: {UserId}, IP: {IpAddress}",
                            fileName, fileCount, userId, ipAddress);

                        throw new ZipBombDetectedException(
                            $"Archive contains too many files (max {_settings.MaxFileCount})",
                            fileName: fileName,
                            userId: userId,
                            ipAddress: ipAddress);
                    }

                    // Skip directories
                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        continue;

                    // Check for suspicious compression ratio
                    if (entry.Length > 0 && entry.CompressedLength > 0)
                    {
                        var compressionRatio = (double)entry.Length / entry.CompressedLength;

                        if (compressionRatio > _settings.MaxCompressionRatio)
                        {
                            _logger.LogWarning(
                                "Suspicious compression ratio detected in {FileName}. " +
                                "Entry: {Entry}, Ratio: {Ratio}, User: {UserId}, IP: {IpAddress}",
                                fileName, entry.FullName, compressionRatio, userId, ipAddress);

                            throw new ZipBombDetectedException(
                                $"Suspicious compression ratio detected ({compressionRatio:F1}:1)",
                                compressionRatio: compressionRatio,
                                estimatedDecompressedSize: entry.Length,
                                fileName: fileName,
                                userId: userId,
                                ipAddress: ipAddress);
                        }
                    }

                    totalUncompressedSize += entry.Length;

                    // Check total decompressed size
                    if (totalUncompressedSize > _settings.MaxDecompressedSize)
                    {
                        _logger.LogWarning(
                            "Decompressed size limit exceeded for {FileName}. " +
                            "Size: {Size} bytes, User: {UserId}, IP: {IpAddress}",
                            fileName, totalUncompressedSize, userId, ipAddress);

                        throw new ZipBombDetectedException(
                            $"Archive would decompress to more than {_settings.MaxDecompressedSize / (1024 * 1024)}MB",
                            compressionRatio: (double)totalUncompressedSize / compressedSize,
                            estimatedDecompressedSize: totalUncompressedSize,
                            fileName: fileName,
                            userId: userId,
                            ipAddress: ipAddress);
                    }

                    // Check for nested ZIP files
                    if (IsZipEntry(entry.FullName))
                    {
                        try
                        {
                            using var entryStream = entry.Open();
                            await ValidateZipStreamAsync(
                                entryStream,
                                $"{fileName}/{entry.FullName}",
                                entry.CompressedLength,
                                userId,
                                ipAddress,
                                cancellationToken,
                                nestingLevel + 1);
                        }
                        catch (ZipBombDetectedException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Could not validate nested archive {Entry} in {FileName}",
                                entry.FullName, fileName);
                            // Continue - don't fail just because we can't read a nested archive
                        }
                    }
                }
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning(ex, "Invalid ZIP data in file {FileName}", fileName);
                // Invalid ZIP format - might be a corrupted file or attack attempt
                throw new ZipBombDetectedException(
                    "Invalid ZIP archive format",
                    fileName: fileName,
                    userId: userId,
                    ipAddress: ipAddress);
            }

            _logger.LogDebug(
                "ZIP file {FileName} validated successfully. Files: {Count}, " +
                "Total size: {Size} bytes",
                fileName, fileCount, totalUncompressedSize);

            return true;
        }

        private static bool IsZipEntry(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension is ".zip" or ".jar" or ".war" or ".ear" or ".docx" or ".xlsx" or ".pptx";
        }
    }
}
