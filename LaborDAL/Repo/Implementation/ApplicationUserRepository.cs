using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace LaborDAL.Repo.Implementation
{
    public class AppUserRepository : Repository<AppUser>, IAppUserRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AppUserRepository> _logger;

        public AppUserRepository(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IMapper mapper,
            ILogger<AppUserRepository> logger) : base(context, mapper)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new user with the specified password
        /// </summary>
        public async Task<AppUser?> CreateUserAsync(AppUser user, string password)
        {
            try
            {
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created successfully: {Email}", user.Email);
                    return user;
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("User creation failed: {Error}", error.Description);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", user.Email);
                return null;
            }
        }

        /// <summary>
        /// Gets a user by their email address
        /// </summary>
        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            try
            {
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Updates the user's rating
        /// </summary>
        public async Task<bool> UpdateUserRatingAsync(string userId, decimal newRating)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for rating update: {UserId}", userId);
                    return false;
                }

                user.AverageRating = newRating;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User rating updated: {UserId}, NewRating: {Rating}", userId, newRating);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user rating: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Verifies the user's ID
        /// </summary>
        public async Task<bool> VerifyUserIdAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for verification: {UserId}", userId);
                    return false;
                }

                user.IDVerified = true;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User ID verified: {UserId}", userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying user ID: {UserId}", userId);
                return false;
            }
        }
    }
}
