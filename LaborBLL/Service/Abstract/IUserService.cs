
using LaborBLL.ModelVM;
using LaborBLL.Response;

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
        Task <bool> LogoutAsync();
    }
}
