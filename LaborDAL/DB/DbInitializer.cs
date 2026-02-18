using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskStatus = LaborDAL.Enums.TaskStatus;

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

                // Seed Sample Users
                await SeedSampleUsersAsync(userManager, logger);

                // Seed Sample Tasks
                await SeedSampleTasksAsync(context, userManager, logger);
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
                    Role = ClientRole.AdminBoth, // Admin with both Worker and Poster capabilities
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
                if (existingAdmin.Role != ClientRole.AdminBoth)
                {
                    existingAdmin.Role = ClientRole.AdminBoth;
                    await userManager.UpdateAsync(existingAdmin);
                }
                
                logger.LogInformation("Admin user already exists, ensured all roles are assigned");
            }
        }

        private static async Task SeedSampleUsersAsync(UserManager<AppUser> userManager, ILogger logger)
        {
            // One Poster User
            var poster = new
            {
                Email = "poster@labormarketplace.com",
                Password = "User@123456",
                FirstName = "John",
                LastName = "Doe",
                Country = "Egypt"
            };

            // One Worker User
            var worker = new
            {
                Email = "worker@labormarketplace.com",
                Password = "User@123456",
                FirstName = "Ahmed",
                LastName = "Hassan",
                Country = "Egypt",
                Skills = "Cleaning, Moving, Plumbing, Electrical, Gardening"
            };

            // One User with Both Roles (Poster + Worker)
            var bothRoles = new
            {
                Email = "both@labormarketplace.com",
                Password = "User@123456",
                FirstName = "Sarah",
                LastName = "Smith",
                Country = "Egypt",
                Skills = "Cleaning, Housekeeping, Organization"
            };

            // Create Poster User
            var existingPoster = await userManager.FindByEmailAsync(poster.Email);
            if (existingPoster == null)
            {
                var user = new AppUser
                {
                    UserName = poster.Email,
                    Email = poster.Email,
                    FirstName = poster.FirstName,
                    LastName = poster.LastName,
                    EmailConfirmed = true,
                    Role = ClientRole.Poster,
                    CreatedAt = DateTime.UtcNow,
                    IDVerified = true,
                    Country = poster.Country
                };

                var result = await userManager.CreateAsync(user, poster.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Poster");
                    logger.LogInformation("Created poster user: {Email}", poster.Email);
                }
            }

            // Create Worker User
            var existingWorker = await userManager.FindByEmailAsync(worker.Email);
            if (existingWorker == null)
            {
                var user = new AppUser
                {
                    UserName = worker.Email,
                    Email = worker.Email,
                    FirstName = worker.FirstName,
                    LastName = worker.LastName,
                    EmailConfirmed = true,
                    Role = ClientRole.Worker,
                    CreatedAt = DateTime.UtcNow,
                    IDVerified = true,
                    Country = worker.Country,
                    Skills = worker.Skills
                };

                var result = await userManager.CreateAsync(user, worker.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Worker");
                    logger.LogInformation("Created worker user: {Email}", worker.Email);
                }
            }

            // Create User with Both Roles
            var existingBoth = await userManager.FindByEmailAsync(bothRoles.Email);
            if (existingBoth == null)
            {
                var user = new AppUser
                {
                    UserName = bothRoles.Email,
                    Email = bothRoles.Email,
                    FirstName = bothRoles.FirstName,
                    LastName = bothRoles.LastName,
                    EmailConfirmed = true,
                    Role = ClientRole.Both, // Both roles
                    CreatedAt = DateTime.UtcNow,
                    IDVerified = true,
                    Country = bothRoles.Country,
                    Skills = bothRoles.Skills
                };

                var result = await userManager.CreateAsync(user, bothRoles.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Poster");
                    await userManager.AddToRoleAsync(user, "Worker");
                    logger.LogInformation("Created user with both Poster and Worker roles: {Email}", bothRoles.Email);
                }
            }
        }

        private static async Task SeedSampleTasksAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger logger)
        {
            // Check if tasks already exist
            if (await context.Tasks.AnyAsync())
            {
                logger.LogInformation("Tasks already exist, skipping task seeding");
                return;
            }

            // Get poster users (using new simplified user structure)
            var posterUser = await userManager.FindByEmailAsync("poster@labormarketplace.com");
            var bothUser = await userManager.FindByEmailAsync("both@labormarketplace.com");
            var adminUser = await userManager.FindByEmailAsync("admin@labormarketplace.com");

            var tasks = new List<TaskItem>
            {
                new TaskItem
                {
                    Title = "Deep House Cleaning",
                    Description = "Need a thorough cleaning of my 3-bedroom apartment. Includes kitchen, bathrooms, living room, and bedrooms. All cleaning supplies provided.",
                    Category = TaskCategory.Cleaning,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 500,
                    Location = "Maadi, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(7),
                    IsUrgent = true
                },
                new TaskItem
                {
                    Title = "Furniture Moving - 2nd Floor Apartment",
                    Description = "Moving furniture from a 2nd floor apartment to a new location. Includes sofa, dining table, beds, and boxes. Need 2 workers.",
                    Category = TaskCategory.Moving,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Hourly,
                    Budget = 100,
                    EstimatedHours = 4,
                    Location = "Heliopolis, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 2,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(14)
                },
                new TaskItem
                {
                    Title = "Fix Leaking Kitchen Faucet",
                    Description = "Kitchen faucet is leaking and needs repair or replacement. I have the new faucet ready, just need installation.",
                    Category = TaskCategory.Plumbing,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 200,
                    Location = "Zamalek, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(3),
                    IsUrgent = true
                },
                new TaskItem
                {
                    Title = "Garden Maintenance and Landscaping",
                    Description = "Need someone to maintain my garden - mowing, trimming hedges, planting new flowers, and general cleanup. Garden is about 200 sqm.",
                    Category = TaskCategory.Gardening,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Hourly,
                    Budget = 80,
                    EstimatedHours = 6,
                    Location = "Sheikh Zayed, Giza",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    DueDate = DateTime.UtcNow.AddDays(10)
                },
                new TaskItem
                {
                    Title = "Wall Painting - Living Room",
                    Description = "Need to paint my living room (approx 40 sqm). Walls need to be prepared and painted with 2 coats. Paint will be provided.",
                    Category = TaskCategory.Painting,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 800,
                    Location = "Nasr City, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(21)
                },
                new TaskItem
                {
                    Title = "Electrical Wiring Check and Repair",
                    Description = "Some outlets in my apartment are not working. Need an electrician to check the wiring and fix any issues.",
                    Category = TaskCategory.Electrical,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Hourly,
                    Budget = 150,
                    EstimatedHours = 3,
                    Location = "Dokki, Giza",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(5),
                    IsUrgent = true
                },
                new TaskItem
                {
                    Title = "AC Unit Installation",
                    Description = "Need to install a new split AC unit in my bedroom. The unit is ready, just need professional installation.",
                    Category = TaskCategory.Repair,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 400,
                    Location = "Mohandessin, Giza",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    DueDate = DateTime.UtcNow.AddDays(7)
                },
                new TaskItem
                {
                    Title = "Virtual Assistant for Data Entry",
                    Description = "Need someone to help with data entry tasks. Can be done remotely. Approximately 10 hours of work spread over a week.",
                    Category = TaskCategory.Other,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Hourly,
                    Budget = 50,
                    EstimatedHours = 10,
                    IsRemote = true,
                    WorkersNeeded = 1,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(14)
                },
                new TaskItem
                {
                    Title = "Event Setup Help - Birthday Party",
                    Description = "Need help setting up for a birthday party - decorating, arranging tables, and general setup. About 4 hours of work.",
                    Category = TaskCategory.EventHelp,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 300,
                    Location = "Smouha, Alexandria",
                    City = "Alexandria",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 2,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(3),
                    IsUrgent = true
                },
                new TaskItem
                {
                    Title = "Furniture Assembly - IKEA Wardrobe",
                    Description = "Need someone to assemble a large IKEA wardrobe. All parts and instructions are ready.",
                    Category = TaskCategory.Assembly,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 250,
                    Location = "New Cairo, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(10)
                }
            };

            context.Tasks.AddRange(tasks);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} sample tasks", tasks.Count);
        }
    }
}
