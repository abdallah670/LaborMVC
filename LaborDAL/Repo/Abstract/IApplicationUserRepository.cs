
namespace LaborDAL.Repo.Abstract
{
    public interface IAppUserRepository : IRepository<AppUser>
    {
        /// <summary>
        /// Creates a new user with the specified password
        /// </summary>
        Task<AppUser?> CreateUserAsync(AppUser user, string password);

        /// <summary>
        /// Gets a user by their email address
        /// </summary>
        Task<AppUser?> GetByEmailAsync(string email);

        /// <summary>
        /// Updates the user's rating
        /// </summary>
        Task<bool> UpdateUserRatingAsync(string userId, decimal newRating);

        /// <summary>
        /// Verifies the user's ID
        /// </summary>
        Task<bool> VerifyUserIdAsync(string userId);
    }
}
