
namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Interface for user-related business operations
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        Task<Response<bool>> RegisterAsync(RegisterViewModel model);

        /// <summary>
        /// Authenticates a user
        /// </summary>
        Task<Response<ProfileViewModel>> LoginAsync(LoginViewModel model);

        /// <summary>
        /// Gets the profile of a user by their ID
        /// </summary>
        Task<ProfileViewModel?> GetProfileAsync(string userId);

        /// <summary>
        /// Gets the profile of a user with all details including ratings and statistics
        /// </summary>
        /// <param name="userId">The ID of the user to get profile for</param>
        /// <param name="viewerId">Optional viewer ID to customize response based on viewer</param>
        Task<ProfileViewModel?> GetProfileWithDetailsAsync(string userId, string? viewerId = null);

        /// <summary>
        /// Updates a user's profile
        /// </summary>
        Task<Response<bool>> UpdateProfileAsync(ProfileViewModel model);

        /// <summary>
        /// Gets a user by their email
        /// </summary>
        Task<ProfileViewModel?> GetByEmailAsync(string email);

        /// <summary>
        /// Checks if an email is already registered
        /// </summary>
        Task<bool> EmailExistsAsync(string email);
        Task<bool> LogoutAsync();
    }
}
