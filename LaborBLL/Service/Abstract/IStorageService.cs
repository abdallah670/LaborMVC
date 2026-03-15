namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for storing and retrieving files
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Upload a file to storage
        /// </summary>
        /// <param name="fileData">File bytes</param>
        /// <param name="fileName">File name</param>
        /// <param name="container">Container/folder name</param>
        /// <returns>URL of the stored file</returns>
        Task<string> UploadAsync(byte[] fileData, string fileName, string container);
        
        /// <summary>
        /// Delete a file from storage
        /// </summary>
        Task<bool> DeleteAsync(string fileUrl);
        
        /// <summary>
        /// Check if file exists
        /// </summary>
        Task<bool> ExistsAsync(string fileUrl);
        
        /// <summary>
        /// Generate a unique file name
        /// </summary>
        string GenerateUniqueFileName(string originalFileName);
    }
}
