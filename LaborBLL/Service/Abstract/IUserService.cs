using LaborBLL.ModelVM;
using LaborBLL.Response;
using System.Threading.Tasks;

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
        /// <returns>Response containing the user ID if successful</returns>
        Task<Response<string>> RegisterAsync(RegisterViewModel model);

        /// <summary>
        /// Authenticates a user
        /// </summary>
        Task<Response<UserProfileDisplayViewModel>> LoginAsync(LoginViewModel model);

        /// <summary>
        /// Gets the profile of a user by their ID
        /// </summary>
        Task<UserProfileDisplayViewModel?> GetProfileAsync(string userId);

        /// <summary>
        /// Gets the profile of a user with all details including ratings and statistics
        /// </summary>
        /// <param name="userId">The ID of the user to get profile for</param>
        /// <param name="viewerId">Optional viewer ID to customize response based on viewer</param>
        Task<UserProfileDisplayViewModel?> GetProfileWithDetailsAsync(string userId, string? viewerId = null);

        /// <summary>
        /// Updates a user's profile
        /// </summary>
        Task<Response<bool>> UpdateProfileAsync(UserProfileUpdateModel model);

        /// <summary>
        /// Gets a user by their email
        /// </summary>
        Task<UserProfileDisplayViewModel?> GetByEmailAsync(string email);

        /// <summary>
        /// Checks if an email is already registered
        /// </summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// Restores a soft-deleted user by admin action
        /// </summary>
        Task<Response<bool>> RestoreUserAsync(string userId);

        Task<bool> LogoutAsync();

        /// <summary>
        /// Generates a password reset token and sends reset email
        /// </summary>
        Task<Response<bool>> ForgotPasswordAsync(string email);

        /// <summary>
        /// Resets user password with token
        /// </summary>
        Task<Response<bool>> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
