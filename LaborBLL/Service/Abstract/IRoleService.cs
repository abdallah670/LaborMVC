using LaborDAL.Enums;

namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for managing user roles
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// Gets the current roles for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The user's current ClientRole flags</returns>
        Task<ClientRole> GetUserRolesAsync(string userId);

        /// <summary>
        /// Checks if a user has a specific role
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="role">The role to check</param>
        /// <returns>True if the user has the role</returns>
        Task<bool> HasRoleAsync(string userId, ClientRole role);

        /// <summary>
        /// Adds a role to a user (preserves existing roles)
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="role">The role to add</param>
        /// <returns>True if successful</returns>
        Task<bool> AddRoleAsync(string userId, ClientRole role);

        /// <summary>
        /// Removes a role from a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="role">The role to remove</param>
        /// <returns>True if successful</returns>
        Task<bool> RemoveRoleAsync(string userId, ClientRole role);

        /// <summary>
        /// Sets the user's roles (replaces all existing roles)
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="roles">The new roles to set</param>
        /// <returns>True if successful</returns>
        Task<bool> SetRolesAsync(string userId, ClientRole roles);

        /// <summary>
        /// Gets all users with a specific role
        /// </summary>
        /// <param name="role">The role to filter by</param>
        /// <returns>List of user IDs with the specified role</returns>
        Task<IEnumerable<string>> GetUsersInRoleAsync(ClientRole role);

        /// <summary>
        /// Syncs the Identity roles with the ClientRole property
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if successful</returns>
        Task<bool> SyncIdentityRolesAsync(string userId);
    }
}
