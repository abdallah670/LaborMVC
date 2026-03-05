using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaborBLL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Result of image validation
    /// </summary>
    public class ImageValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long TotalPixels { get; set; }
        public string? Format { get; set; }

        public static ImageValidationResult Success(int width, int height, string format)
        {
            return new ImageValidationResult
            {
                IsValid = true,
                Width = width,
                Height = height,
                TotalPixels = (long)width * height,
                Format = format
            };
        }

        public static ImageValidationResult Failure(string errorMessage)
        {
            return new ImageValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Service for validating image files
    /// </summary>
    public interface IImageValidationService
    {
        /// <summary>
        /// Validates image dimensions and checks for pixel flood attacks
        /// </summary>
        Task<ImageValidationResult> ValidateImageAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file is an image
        /// </summary>
        Task<bool> IsImageAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets image dimensions without loading the full image
        /// </summary>
        Task<(int width, int height)?> GetImageDimensionsAsync(
            Stream stream,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Validates image files for security threats including pixel flood attacks
    /// </summary>
    public class ImageValidationService : IImageValidationService
    {
        private readonly ImageValidationSettings _settings;
        private readonly ILogger<ImageValidationService> _logger;

        // Image file signatures (magic numbers)
        private static readonly Dictionary<string, byte[][]> ImageSignatures = new()
        {
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".gif"] = new[]
            {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }  // GIF89a
            },
            [".bmp"] = new[] { new byte[] { 0x42, 0x4D } },
            [".webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } // RIFF header
        };

        public ImageValidationService(
            IOptions<ImageValidationSettings> settings,
            ILogger<ImageValidationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ImageValidationResult> ValidateImageAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return ImageValidationResult.Failure("No file provided");
            }

            if (!await IsImageAsync(file, cancellationToken))
            {
                return ImageValidationResult.Failure("File is not a valid image");
            }

            // Skip large files to avoid memory issues
            if (file.Length > _settings.MaxFileSizeForValidation)
            {
                _logger.LogWarning(
                    "Image file {FileName} exceeds validation size limit ({Size} bytes). " +
                    "User: {UserId}, IP: {IpAddress}",
                    file.FileName, file.Length, userId, ipAddress);

                // Still try to validate dimensions without loading full image
                return await ValidateDimensionsOnlyAsync(file, userId, ipAddress, cancellationToken);
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream, cancellationToken);

                var width = image.Width;
                var height = image.Height;
                var totalPixels = (long)width * height;
                var format = image.Metadata.DecodedImageFormat?.Name ?? "unknown";

                // Check dimensions
                if (_settings.ValidateDimensions)
                {
                    if (width > _settings.MaxWidth || height > _settings.MaxHeight)
                    {
                        _logger.LogWarning(
                            "Image dimensions exceeded for {FileName}. " +
                            "Dimensions: {Width}x{Height}, Max: {MaxWidth}x{MaxHeight}. " +
                            "User: {UserId}, IP: {IpAddress}",
                            file.FileName, width, height, _settings.MaxWidth, _settings.MaxHeight,
                            userId, ipAddress);

                        throw new ImageDimensionExceededException(
                            $"Image dimensions ({width}x{height}) exceed maximum allowed " +
                            $"({_settings.MaxWidth}x{_settings.MaxHeight})",
                            width, height, _settings.MaxWidth, _settings.MaxHeight,
                            fileName: file.FileName,
                            userId: userId,
                            ipAddress: ipAddress);
                    }
                }

                // Check for pixel flood attacks
                if (_settings.CheckPixelFlood)
                {
                    if (totalPixels > _settings.MaxPixels)
                    {
                        _logger.LogWarning(
                            "Pixel flood attack detected in {FileName}. " +
                            "Pixels: {Pixels}, Max: {MaxPixels}. " +
                            "User: {UserId}, IP: {IpAddress}",
                            file.FileName, totalPixels, _settings.MaxPixels,
                            userId, ipAddress);

                        throw new PixelFloodAttackException(
                            $"Image pixel count ({totalPixels:N0}) exceeds maximum allowed ({_settings.MaxPixels:N0}). " +
                            "This may be a pixel flood attack.",
                            totalPixels, _settings.MaxPixels,
                            fileName: file.FileName,
                            userId: userId,
                            ipAddress: ipAddress);
                    }
                }

                _logger.LogDebug(
                    "Image {FileName} validated successfully. Dimensions: {Width}x{Height}, " +
                    "Format: {Format}",
                    file.FileName, width, height, format);

                return ImageValidationResult.Success(width, height, format);
            }
            catch (UnknownImageFormatException ex)
            {
                _logger.LogWarning(ex, "Unknown image format for file {FileName}", file.FileName);
                return ImageValidationResult.Failure("Unknown or unsupported image format");
            }
            catch (ImageFormatException ex)
            {
                _logger.LogWarning(ex, "Invalid image format for file {FileName}", file.FileName);
                return ImageValidationResult.Failure("Invalid image format or corrupted file");
            }
            catch (FileUploadSecurityException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating image {FileName}", file.FileName);
                return ImageValidationResult.Failure($"Error validating image: {ex.Message}");
            }
        }

        public Task<bool> IsImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return Task.FromResult(false);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!ImageSignatures.ContainsKey(extension))
                return Task.FromResult(false);

            // Validate signature
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[12];
            stream.Read(headerBytes, 0, Math.Min(12, (int)stream.Length));

            var signatures = ImageSignatures[extension];
            var isValid = signatures.Any(sig =>
                headerBytes.Take(sig.Length).SequenceEqual(sig));

            return Task.FromResult(isValid);
        }

        public async Task<(int width, int height)?> GetImageDimensionsAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var info = await Image.IdentifyAsync(stream, cancellationToken);
                return info != null ? (info.Width, info.Height) : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<ImageValidationResult> ValidateDimensionsOnlyAsync(
            IFormFile file,
            string? userId,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var dimensions = await GetImageDimensionsAsync(stream, cancellationToken);

                if (!dimensions.HasValue)
                {
                    return ImageValidationResult.Failure("Could not determine image dimensions");
                }

                var (width, height) = dimensions.Value;
                var totalPixels = (long)width * height;

                // Check dimensions
                if (_settings.ValidateDimensions)
                {
                    if (width > _settings.MaxWidth || height > _settings.MaxHeight)
                    {
                        throw new ImageDimensionExceededException(
                            $"Image dimensions ({width}x{height}) exceed maximum allowed " +
                            $"({_settings.MaxWidth}x{_settings.MaxHeight})",
                            width, height, _settings.MaxWidth, _settings.MaxHeight,
                            fileName: file.FileName,
                            userId: userId,
                            ipAddress: ipAddress);
                    }
                }

                // Check for pixel flood attacks
                if (_settings.CheckPixelFlood && totalPixels > _settings.MaxPixels)
                {
                    throw new PixelFloodAttackException(
                        $"Image pixel count ({totalPixels:N0}) exceeds maximum allowed ({_settings.MaxPixels:N0})",
                        totalPixels, _settings.MaxPixels,
                        fileName: file.FileName,
                        userId: userId,
                        ipAddress: ipAddress);
                }

                return ImageValidationResult.Success(width, height, "unknown");
            }
            catch (FileUploadSecurityException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating image dimensions for {FileName}", file.FileName);
                return ImageValidationResult.Failure("Error validating image dimensions");
            }
        }
    }
}
