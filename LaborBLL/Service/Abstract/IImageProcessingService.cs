using Microsoft.AspNetCore.Http;

// Use the existing ImageValidationResult from ImageValidationService
using ImageValidationResult = LaborBLL.Service.Implementation.ImageValidationResult;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for processing and resizing images
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>
        /// Process a profile picture - crop to square and generate multiple sizes
        /// </summary>
        Task<ProcessedImageResult> ProcessProfilePictureAsync(IFormFile file);
        
        /// <summary>
        /// Resize image to specific dimensions
        /// </summary>
        Task<byte[]> ResizeAsync(byte[] imageData, int width, int height);
        
        /// <summary>
        /// Crop image to square from center
        /// </summary>
        Task<byte[]> CropToSquareAsync(byte[] imageData);
        
        /// <summary>
        /// Validate image dimensions and format
        /// </summary>
        Task<ImageValidationResult> ValidateImageAsync(IFormFile file);
    }

    /// <summary>
    /// Result of image processing
    /// </summary>
    public class ProcessedImageResult
    {
        public byte[] Thumbnail { get; set; } = null!; // 100x100
        public byte[] Medium { get; set; } = null!;    // 300x300
        public byte[] Full { get; set; } = null!;      // 800x800
        public string Format { get; set; } = null!;
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
    }
}
