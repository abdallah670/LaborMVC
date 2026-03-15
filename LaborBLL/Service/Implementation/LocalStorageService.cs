using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Local file storage implementation for development
    /// </summary>
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalStorageService> _logger;
        private const string UploadFolder = "uploads";

        public LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Upload a file to local storage
        /// </summary>
        public async Task<string> UploadAsync(byte[] fileData, string fileName, string container)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, UploadFolder, container);
            
            // Ensure directory exists
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, fileName);
            
            // Write file
            await File.WriteAllBytesAsync(filePath, fileData);
            
            _logger.LogInformation("File uploaded: {FilePath}", filePath);
            
            // Return relative URL
            return $"/uploads/{container}/{fileName}";
        }

        /// <summary>
        /// Delete a file from storage
        /// </summary>
        public Task<bool> DeleteAsync(string fileUrl)
        {
            try
            {
                // Convert URL to file path
                var relativePath = fileUrl.TrimStart('/');
                var filePath = Path.Combine(_environment.WebRootPath, relativePath);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted: {FilePath}", filePath);
                    return Task.FromResult(true);
                }
                
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Check if file exists
        /// </summary>
        public Task<bool> ExistsAsync(string fileUrl)
        {
            var relativePath = fileUrl.TrimStart('/');
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);
            return Task.FromResult(File.Exists(filePath));
        }

        /// <summary>
        /// Generate a unique file name with GUID prefix
        /// </summary>
        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            return fileName;
        }
    }
}
