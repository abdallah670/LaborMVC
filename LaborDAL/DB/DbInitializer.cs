using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaborDAL.DB
{
    /// <summary>
    /// Database initializer for seeding default data
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Seeds default roles and admin user
        /// </summary>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

                // Ensure database is created
                await context.Database.MigrateAsync();

                // Seed Roles
                await SeedRolesAsync(roleManager, logger);

                // Seed Admin User
                await SeedAdminUserAsync(userManager, logger);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            // Only 3 base roles - users can be assigned to multiple roles
            var roles = new[]
            {
                "Admin",
                "Worker",
                "Poster"
            };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        logger.LogWarning("Failed to create role {RoleName}: {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager, ILogger logger)
        {
            const string adminEmail = "admin@labormarketplace.com";
            const string adminPassword = "Admin@123456";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    Role = ClientRole.Admin | ClientRole.Worker | ClientRole.Poster, // Admin with both Worker and Poster capabilities
                    CreatedAt = DateTime.UtcNow,
                    IDVerified = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    // Assign all three Identity roles
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "Worker");
                    await userManager.AddToRoleAsync(adminUser, "Poster");
                    
                    logger.LogInformation("Created admin user: {Email} with Admin, Worker, and Poster roles", adminEmail);
                }
                else
                {
                    logger.LogWarning("Failed to create admin user: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure existing admin has all three roles
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                }
                if (!await userManager.IsInRoleAsync(existingAdmin, "Worker"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Worker");
                }
                if (!await userManager.IsInRoleAsync(existingAdmin, "Poster"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Poster");
                }
                
                // Update the Role property if needed
                if (existingAdmin.Role != (ClientRole.Admin | ClientRole.Worker | ClientRole.Poster))
                {
                    existingAdmin.Role = ClientRole.Admin | ClientRole.Worker | ClientRole.Poster;
                    await userManager.UpdateAsync(existingAdmin);
                }
                
                logger.LogInformation("Admin user already exists, ensured all roles are assigned");
            }
        }
    }
}