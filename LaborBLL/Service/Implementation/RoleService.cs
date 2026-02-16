namespace LaborBLL.Service
{
    /// <summary>
    /// Service for managing user roles
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            UserManager<AppUser> userManager,
            ILogger<RoleService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ClientRole> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return ClientRole.None;
            }

            return user.Role;
        }

        /// <inheritdoc />
        public async Task<bool> HasRoleAsync(string userId, ClientRole role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return user.Role.HasFlag(role);
        }

        /// <inheritdoc />
        public async Task<bool> AddRoleAsync(string userId, ClientRole role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            // Add the role flag
            user.Role |= role;

            // Update the user
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to update user role: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Sync Identity roles
            await SyncIdentityRolesAsync(userId);

            _logger.LogInformation("Added role {Role} to user {UserId}", role, userId);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveRoleAsync(string userId, ClientRole role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            // Remove the role flag
            user.Role &= ~role;

            // Update the user
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to update user role: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Sync Identity roles
            await SyncIdentityRolesAsync(userId);

            _logger.LogInformation("Removed role {Role} from user {UserId}", role, userId);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> SetRolesAsync(string userId, ClientRole roles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            // Set the new roles
            user.Role = roles;

            // Update the user
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to update user roles: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Sync Identity roles
            await SyncIdentityRolesAsync(userId);

            _logger.LogInformation("Set roles {Roles} for user {UserId}", roles, userId);
            return true;
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetUsersInRoleAsync(ClientRole role)
        {
            var users = _userManager.Users
                .Where(u => (u.Role & role) == role)
                .Select(u => u.Id)
                .AsEnumerable();

            return Task.FromResult(users);
        }

        /// <inheritdoc />
        public async Task<bool> SyncIdentityRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            // Get current Identity roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Determine what roles the user should have based on ClientRole
            var targetRoles = new List<string>();
            if (user.Role.HasFlag(ClientRole.Admin))
            {
                targetRoles.Add("Admin");
            }
            if (user.Role.HasFlag(ClientRole.Worker))
            {
                targetRoles.Add("Worker");
            }
            if (user.Role.HasFlag(ClientRole.Poster))
            {
                targetRoles.Add("Poster");
            }

            // Roles to add
            var rolesToAdd = targetRoles.Except(currentRoles).ToList();
            // Roles to remove
            var rolesToRemove = currentRoles.Except(targetRoles).ToList();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add Identity roles: {Errors}", 
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return false;
                }
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove Identity roles: {Errors}", 
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return false;
                }
            }

            _logger.LogInformation("Synced Identity roles for user {UserId}", userId);
            return true;
        }
    }
}
