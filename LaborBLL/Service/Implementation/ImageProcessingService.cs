using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

// Use the existing ImageValidationResult from ImageValidationService
using ImageValidationResult = LaborBLL.Service.Implementation.ImageValidationResult;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for processing and resizing images using ImageSharp
    /// </summary>
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly ILogger<ImageProcessingService> _logger;
        
        // Size configurations for profile pictures
        private const int ThumbnailSize = 100;
        private const int MediumSize = 300;
        private const int FullSize = 800;
        
        // Validation limits
        private const int MinDimension = 200;
        private const int MaxDimension = 4000;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedFormats = { "JPEG", "PNG", "GIF" };

        public ImageProcessingService(ILogger<ImageProcessingService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ProcessedImageResult> ProcessProfilePictureAsync(IFormFile file)
        {
            _logger.LogInformation("Processing profile picture: {FileName}, Size: {Size} bytes", 
                file.FileName, file.Length);

            // Validate first
            var validation = await ValidateImageAsync(file);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }

            // Load image bytes
            byte[] imageBytes;
            using (var inputStream = file.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                await inputStream.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            // Process each size from original
            var format = "JPEG";
            using (var image = Image.Load(imageBytes))
            {
                format = image.Metadata.DecodedImageFormat?.Name ?? "JPEG";
            }

            // Generate thumbnail (100x100)
            byte[] thumbnail;
            using (var image = Image.Load(imageBytes))
            {
                CropToSquareInternal(image);
                thumbnail = ResizeAndEncode(image, ThumbnailSize, ThumbnailSize);
            }

            // Generate medium (300x300)
            byte[] medium;
            using (var image = Image.Load(imageBytes))
            {
                CropToSquareInternal(image);
                medium = ResizeAndEncode(image, MediumSize, MediumSize);
            }

            // Generate full (800x800)
            byte[] full;
            using (var image = Image.Load(imageBytes))
            {
                CropToSquareInternal(image);
                full = ResizeAndEncode(image, FullSize, FullSize);
            }

            _logger.LogInformation("Profile picture processed successfully. Original: {Width}x{Height}, Format: {Format}",
                validation.Width, validation.Height, format);

            return new ProcessedImageResult
            {
                Thumbnail = thumbnail,
                Medium = medium,
                Full = full,
                Format = format,
                OriginalWidth = validation.Width,
                OriginalHeight = validation.Height
            };
        }

        /// <inheritdoc />
        public Task<byte[]> ResizeAsync(byte[] imageData, int width, int height)
        {
            using var image = Image.Load(imageData);
            var result = ResizeAndEncode(image, width, height);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<byte[]> CropToSquareAsync(byte[] imageData)
        {
            using var image = Image.Load(imageData);
            CropToSquareInternal(image);
            
            using var outputStream = new MemoryStream();
            image.SaveAsJpeg(outputStream);
            return Task.FromResult(outputStream.ToArray());
        }

        /// <inheritdoc />
        public async Task<ImageValidationResult> ValidateImageAsync(IFormFile file)
        {
            // Check file size
            if (file.Length > MaxFileSize)
            {
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Image size exceeds maximum allowed size of 5MB. Current size: {file.Length / 1024 / 1024}MB"
                };
            }

            // Check content type
            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invalid image format. Allowed formats: JPEG, PNG, GIF. Current: {file.ContentType}"
                };
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

                var width = image.Width;
                var height = image.Height;
                var format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown";

                // Check dimensions
                if (width < MinDimension || height < MinDimension)
                {
                    return new ImageValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Image dimensions too small. Minimum: {MinDimension}x{MinDimension}. Current: {width}x{height}"
                    };
                }

                if (width > MaxDimension || height > MaxDimension)
                {
                    return new ImageValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Image dimensions too large. Maximum: {MaxDimension}x{MaxDimension}. Current: {width}x{height}"
                    };
                }

                // Check format
                if (!AllowedFormats.Contains(format, StringComparer.OrdinalIgnoreCase))
                {
                    return new ImageValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Unsupported image format: {format}. Allowed: {string.Join(", ", AllowedFormats)}"
                    };
                }

                return new ImageValidationResult
                {
                    IsValid = true,
                    Width = width,
                    Height = height,
                    Format = format
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating image: {FileName}", file.FileName);
                return new ImageValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid or corrupted image file."
                };
            }
        }

        /// <summary>
        /// Crop image to square from center (modifies image in-place)
        /// </summary>
        private void CropToSquareInternal(Image image)
        {
            var width = image.Width;
            var height = image.Height;

            // If already square, no need to crop
            if (width == height)
            {
                return;
            }

            // Calculate crop dimensions
            int cropSize = Math.Min(width, height);
            int x = (width - cropSize) / 2;
            int y = (height - cropSize) / 2;

            // Crop the image
            image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, cropSize, cropSize)));
        }

        /// <summary>
        /// Resize image and encode to JPEG bytes
        /// </summary>
        private byte[] ResizeAndEncode(Image image, int width, int height)
        {
            // Resize the image in-place
            image.Mutate(ctx => ctx.Resize(width, height, KnownResamplers.Lanczos3));
            
            using var outputStream = new MemoryStream();
            
            // Save as JPEG with high quality for smaller file size
            var encoder = new JpegEncoder
            {
                Quality = 90
            };
            
            image.Save(outputStream, encoder);
            return outputStream.ToArray();
        }
    }
}
