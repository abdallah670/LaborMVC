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

                await context.Database.MigrateAsync();

                await SeedRolesAsync(roleManager, logger);
                await SeedAdminUserAsync(userManager, logger);
                await SeedSampleUsersAsync(userManager, logger);
                await SeedAdditionalWorkersAsync(userManager, logger);
                await SeedSampleTasksAsync(context, userManager, logger);
                await SeedApplicationsAsync(context, userManager, logger);
                await SeedBookingsAsync(context, userManager, logger);
                await SeedPaymentsAsync(context, logger);
                await SeedRatingsAsync(context, userManager, logger);
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
            var roles = new[] { "Admin", "Worker", "Poster" };

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
                    Role = ClientRole.AdminBoth,
                    CreatedAt = DateTime.UtcNow,
                    IDVerified = true,
                    AverageRating = 5.0m
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "Worker");
                    await userManager.AddToRoleAsync(adminUser, "Poster");
                    logger.LogInformation("Created admin user: {Email}", adminEmail);
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                if (!await userManager.IsInRoleAsync(existingAdmin, "Worker"))
                    await userManager.AddToRoleAsync(existingAdmin, "Worker");
                if (!await userManager.IsInRoleAsync(existingAdmin, "Poster"))
                    await userManager.AddToRoleAsync(existingAdmin, "Poster");
                
                if (existingAdmin.Role != ClientRole.AdminBoth)
                {
                    existingAdmin.Role = ClientRole.AdminBoth;
                    await userManager.UpdateAsync(existingAdmin);
                }
                
                logger.LogInformation("Admin user already exists");
            }
        }

        private static async Task SeedSampleUsersAsync(UserManager<AppUser> userManager, ILogger logger)
        {
            var poster = new { Email = "poster@labormarketplace.com", Password = "User@123456", FirstName = "John", LastName = "Doe", Country = "Egypt" };
            var worker = new { Email = "worker@labormarketplace.com", Password = "User@123456", FirstName = "Ahmed", LastName = "Hassan", Country = "Egypt", Skills = "Cleaning, Moving, Plumbing, Electrical, Gardening" };
            var bothRoles = new { Email = "both@labormarketplace.com", Password = "User@123456", FirstName = "Sarah", LastName = "Smith", Country = "Egypt", Skills = "Cleaning, Housekeeping, Organization" };

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
                    Country = poster.Country,
                    AverageRating = 4.8m
                };

                var result = await userManager.CreateAsync(user, poster.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Poster");
                    logger.LogInformation("Created poster user: {Email}", poster.Email);
                }
            }

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
                    Skills = worker.Skills,
                    AverageRating = 4.5m
                };

                var result = await userManager.CreateAsync(user, worker.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Worker");
                    logger.LogInformation("Created worker user: {Email}", worker.Email);
                }
            }

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
                    Role = ClientRole.Both,
                    CreatedAt = DateTime.UtcNow,
                    IDVerified = true,
                    Country = bothRoles.Country,
                    Skills = bothRoles.Skills,
                    AverageRating = 4.2m
                };

                var result = await userManager.CreateAsync(user, bothRoles.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Poster");
                    await userManager.AddToRoleAsync(user, "Worker");
                    logger.LogInformation("Created both user: {Email}", bothRoles.Email);
                }
            }
        }

        private static async Task SeedAdditionalWorkersAsync(UserManager<AppUser> userManager, ILogger logger)
        {
            var workers = new[]
            {
                new { Email = "mohamed@labormarketplace.com", Password = "User@123456", FirstName = "Mohamed", LastName = "Ibrahim", Skills = "Plumbing, Electrical, Repair", Rating = 4.7m },
                new { Email = "fatma@labormarketplace.com", Password = "User@123456", FirstName = "Fatma", LastName = "Ali", Skills = "Cleaning, Housekeeping, Organization", Rating = 4.9m },
                new { Email = "omar@labormarketplace.com", Password = "User@123456", FirstName = "Omar", LastName = "Hussein", Skills = "Moving, Delivery, Assembly", Rating = 4.3m },
                new { Email = "layla@labormarketplace.com", Password = "User@123456", FirstName = "Layla", LastName = "Youssef", Skills = "Painting, Decorating, Carpentry", Rating = 4.6m },
                new { Email = "hossam@labormarketplace.com", Password = "User@123456", FirstName = "Hossam", LastName = "Ahmed", Skills = "Gardening, Landscaping, Painting", Rating = 4.4m }
            };

            foreach (var w in workers)
            {
                var existing = await userManager.FindByEmailAsync(w.Email);
                if (existing == null)
                {
                    var user = new AppUser
                    {
                        UserName = w.Email,
                        Email = w.Email,
                        FirstName = w.FirstName,
                        LastName = w.LastName,
                        EmailConfirmed = true,
                        Role = ClientRole.Worker,
                        CreatedAt = DateTime.UtcNow,
                        IDVerified = true,
                        Country = "Egypt",
                        Skills = w.Skills,
                        AverageRating = w.Rating
                    };

                    var result = await userManager.CreateAsync(user, w.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Worker");
                        logger.LogInformation("Created worker: {Email}", w.Email);
                    }
                }
            }
        }

        private static async Task SeedSampleTasksAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger logger)
        {
            if (await context.Tasks.AnyAsync())
            {
                logger.LogInformation("Tasks already exist, skipping task seeding");
                return;
            }

            var posterUser = await userManager.FindByEmailAsync("poster@labormarketplace.com");
            var bothUser = await userManager.FindByEmailAsync("both@labormarketplace.com");
            var adminUser = await userManager.FindByEmailAsync("admin@labormarketplace.com");

            var tasks = new List<TaskItem>
            {
                // Open tasks (6)
                new TaskItem
                {
                    Title = "Deep House Cleaning",
                    Description = "Need a thorough cleaning of my 3-bedroom apartment. Includes kitchen, bathrooms, living room, and bedrooms.",
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
                    IsUrgent = true,
                    ViewCount = 15
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
                    DueDate = DateTime.UtcNow.AddDays(14),
                    ViewCount = 8
                },
                new TaskItem
                {
                    Title = "Fix Leaking Kitchen Faucet",
                    Description = "Kitchen faucet is leaking and needs repair or replacement.",
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
                    IsUrgent = true,
                    ViewCount = 22
                },
                new TaskItem
                {
                    Title = "Garden Maintenance",
                    Description = "Need someone to maintain my garden - mowing, trimming hedges, planting new flowers.",
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
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    DueDate = DateTime.UtcNow.AddDays(10),
                    ViewCount = 5
                },
                new TaskItem
                {
                    Title = "Wall Painting - Living Room",
                    Description = "Need to paint my living room (approx 40 sqm). Walls need to be prepared and painted with 2 coats.",
                    Category = TaskCategory.Painting,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 800,
                    Location = "Nasr City, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(21),
                    ViewCount = 12
                },
                new TaskItem
                {
                    Title = "AC Unit Installation",
                    Description = "Need to install a new split AC unit in my bedroom.",
                    Category = TaskCategory.Repair,
                    Status = TaskStatus.Open,
                    BudgetType = BudgetType.Fixed,
                    Budget = 400,
                    Location = "Mohandessin, Giza",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    DueDate = DateTime.UtcNow.AddDays(7),
                    ViewCount = 18
                },

                // In Progress tasks (4)
                new TaskItem
                {
                    Title = "Electrical Wiring Check",
                    Description = "Some outlets in my apartment are not working. Need an electrician.",
                    Category = TaskCategory.Electrical,
                    Status = TaskStatus.InProgress,
                    BudgetType = BudgetType.Hourly,
                    Budget = 150,
                    EstimatedHours = 3,
                    Location = "Dokki, Giza",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    DueDate = DateTime.UtcNow.AddDays(1),
                    IsUrgent = true,
                    AssignedAt = DateTime.UtcNow.AddDays(-2),
                    ViewCount = 30
                },
                new TaskItem
                {
                    Title = "Furniture Assembly - IKEA Wardrobe",
                    Description = "Need someone to assemble a large IKEA wardrobe.",
                    Category = TaskCategory.Assembly,
                    Status = TaskStatus.InProgress,
                    BudgetType = BudgetType.Fixed,
                    Budget = 250,
                    Location = "New Cairo, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    DueDate = DateTime.UtcNow.AddDays(2),
                    AssignedAt = DateTime.UtcNow.AddDays(-1),
                    ViewCount = 25
                },
                new TaskItem
                {
                    Title = "Data Entry Work",
                    Description = "Need help with data entry tasks. Can be done remotely.",
                    Category = TaskCategory.Other,
                    Status = TaskStatus.InProgress,
                    BudgetType = BudgetType.Hourly,
                    Budget = 50,
                    EstimatedHours = 10,
                    IsRemote = true,
                    WorkersNeeded = 1,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    DueDate = DateTime.UtcNow.AddDays(4),
                    AssignedAt = DateTime.UtcNow.AddDays(-3),
                    ViewCount = 40
                },
                new TaskItem
                {
                    Title = "Pet Care - Dog Walking",
                    Description = "Need someone to walk my dog twice a day for a week.",
                    Category = TaskCategory.PetCare,
                    Status = TaskStatus.InProgress,
                    BudgetType = BudgetType.Fixed,
                    Budget = 300,
                    Location = "Maadi, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(4),
                    AssignedAt = DateTime.UtcNow.AddDays(-1),
                    ViewCount = 16
                },

                // Completed tasks (5)
                new TaskItem
                {
                    Title = "Bathroom Renovation Help",
                    Description = "Need help with bathroom renovation - tiling and painting.",
                    Category = TaskCategory.Painting,
                    Status = TaskStatus.Completed,
                    BudgetType = BudgetType.Fixed,
                    Budget = 1200,
                    Location = "Giza, Cairo",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 2,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    DueDate = DateTime.UtcNow.AddDays(-5),
                    CompletedAt = DateTime.UtcNow.AddDays(-6),
                    AssignedAt = DateTime.UtcNow.AddDays(-15),
                    ViewCount = 45
                },
                new TaskItem
                {
                    Title = "Office Move",
                    Description = "Moving office equipment from one building to another.",
                    Category = TaskCategory.Moving,
                    Status = TaskStatus.Completed,
                    BudgetType = BudgetType.Fixed,
                    Budget = 2500,
                    Location = "Downtown Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 4,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    DueDate = DateTime.UtcNow.AddDays(-10),
                    CompletedAt = DateTime.UtcNow.AddDays(-11),
                    AssignedAt = DateTime.UtcNow.AddDays(-20),
                    ViewCount = 60
                },
                new TaskItem
                {
                    Title = "Deep Carpet Cleaning",
                    Description = "Need professional carpet cleaning for entire office.",
                    Category = TaskCategory.Cleaning,
                    Status = TaskStatus.Completed,
                    BudgetType = BudgetType.Fixed,
                    Budget = 800,
                    Location = "Smart Village, Giza",
                    City = "Giza",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 2,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    DueDate = DateTime.UtcNow.AddDays(-15),
                    CompletedAt = DateTime.UtcNow.AddDays(-16),
                    AssignedAt = DateTime.UtcNow.AddDays(-25),
                    ViewCount = 35
                },
                new TaskItem
                {
                    Title = "Kitchen Plumbing Fix",
                    Description = "Fix kitchen sink drain and install new garbage disposal.",
                    Category = TaskCategory.Plumbing,
                    Status = TaskStatus.Completed,
                    BudgetType = BudgetType.Fixed,
                    Budget = 350,
                    Location = "Helwan, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 1,
                    PosterId = bothUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    DueDate = DateTime.UtcNow.AddDays(-3),
                    CompletedAt = DateTime.UtcNow.AddDays(-4),
                    AssignedAt = DateTime.UtcNow.AddDays(-10),
                    ViewCount = 28
                },
                new TaskItem
                {
                    Title = "Garden Landscaping",
                    Description = "Design and implement new garden landscaping.",
                    Category = TaskCategory.Gardening,
                    Status = TaskStatus.Completed,
                    BudgetType = BudgetType.Fixed,
                    Budget = 3000,
                    Location = "Al-Mokattam, Cairo",
                    City = "Cairo",
                    Country = "Egypt",
                    IsRemote = false,
                    WorkersNeeded = 3,
                    PosterId = posterUser?.Id ?? adminUser?.Id ?? "",
                    CreatedAt = DateTime.UtcNow.AddDays(-40),
                    DueDate = DateTime.UtcNow.AddDays(-20),
                    CompletedAt = DateTime.UtcNow.AddDays(-21),
                    AssignedAt = DateTime.UtcNow.AddDays(-35),
                    ViewCount = 55
                }
            };

            context.Tasks.AddRange(tasks);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} sample tasks", tasks.Count);
        }

        private static async Task SeedApplicationsAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger logger)
        {
            if (await context.TaskApplications.AnyAsync())
            {
                logger.LogInformation("Applications already exist, skipping");
                return;
            }

            var workerUser = await userManager.FindByEmailAsync("worker@labormarketplace.com");
            var mohamedUser = await userManager.FindByEmailAsync("mohamed@labormarketplace.com");
            var fatmaUser = await userManager.FindByEmailAsync("fatma@labormarketplace.com");
            var omarUser = await userManager.FindByEmailAsync("omar@labormarketplace.com");
            var laylaUser = await userManager.FindByEmailAsync("layla@labormarketplace.com");
            var adminUser = await userManager.FindByEmailAsync("admin@labormarketplace.com");

            var tasks = context.Tasks.ToList();
            var openTasks = tasks.Where(t => t.Status == TaskStatus.Open).ToList();
            var inProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList();

            var applications = new List<TaskApplication>();

            // Applications for open tasks
            if (openTasks.Count >= 1 && workerUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[0].Id,
                    WorkerId = workerUser.Id,
                    ProposedBudget = 450,
                    EstimatedHours = 3,
                    Message = "I have 5 years of experience in cleaning. I can do this job perfectly!",
                    Status = ApplicationStatus.Accepted,
                    RespondedAt = DateTime.UtcNow.AddDays(-1)
                });
            }

            if (openTasks.Count >= 2 && mohamedUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[1].Id,
                    WorkerId = mohamedUser.Id,
                    ProposedBudget = 120,
                    EstimatedHours = 5,
                    Message = "I can provide 2 workers for your move. Professional movers with van.",
                    Status = ApplicationStatus.Pending
                });
            }

            if (openTasks.Count >= 2 && omarUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[1].Id,
                    WorkerId = omarUser.Id,
                    ProposedBudget = 100,
                    EstimatedHours = 4,
                    Message = "I have a truck and can help with moving. Good rates!",
                    Status = ApplicationStatus.Pending
                });
            }

            if (openTasks.Count >= 3 && fatmaUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[2].Id,
                    WorkerId = fatmaUser.Id,
                    ProposedBudget = 180,
                    EstimatedHours = 2,
                    Message = "Professional plumber here. I'll fix your faucet same day!",
                    Status = ApplicationStatus.Accepted,
                    RespondedAt = DateTime.UtcNow.AddHours(-5)
                });
            }

            if (openTasks.Count >= 4 && laylaUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[3].Id,
                    WorkerId = laylaUser.Id,
                    ProposedBudget = 90,
                    EstimatedHours = 7,
                    Message = "I love gardening and can make your garden beautiful!",
                    Status = ApplicationStatus.Pending
                });
            }

            if (openTasks.Count >= 5 && workerUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[4].Id,
                    WorkerId = workerUser.Id,
                    ProposedBudget = 750,
                    EstimatedHours = 16,
                    Message = "Professional painter with 10 years experience. Will do great job!",
                    Status = ApplicationStatus.Pending
                });
            }

            if (openTasks.Count >= 6 && mohamedUser != null)
            {
                applications.Add(new TaskApplication
                {
                    TaskItemId = openTasks[5].Id,
                    WorkerId = mohamedUser.Id,
                    ProposedBudget = 380,
                    EstimatedHours = 3,
                    Message = "Certified AC technician. I'll install it properly.",
                    Status = ApplicationStatus.Pending
                });
            }

            // Applications for in-progress tasks (already accepted)
            foreach (var task in inProgressTasks)
            {
                var randomWorker = new[] { workerUser, mohamedUser, fatmaUser, omarUser }.FirstOrDefault(w => w != null);
                if (randomWorker != null)
                {
                    applications.Add(new TaskApplication
                    {
                        TaskItemId = task.Id,
                        WorkerId = randomWorker.Id,
                        ProposedBudget = task.Budget * 0.9m,
                        EstimatedHours = task.EstimatedHours,
                        Message = "I'd love to help with this task!",
                        Status = ApplicationStatus.Accepted,
                        RespondedAt = DateTime.UtcNow.AddDays(-3)
                    });
                }
            }

            context.TaskApplications.AddRange(applications);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} sample applications", applications.Count);
        }

        private static async Task SeedBookingsAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger logger)
        {
            if (await context.Bookings.AnyAsync())
            {
                logger.LogInformation("Bookings already exist, skipping");
                return;
            }

            var workerUser = await userManager.FindByEmailAsync("worker@labormarketplace.com");
            var mohamedUser = await userManager.FindByEmailAsync("mohamed@labormarketplace.com");
            var fatmaUser = await userManager.FindByEmailAsync("fatma@labormarketplace.com");
            var adminUser = await userManager.FindByEmailAsync("admin@labormarketplace.com");

            var completedTasks = context.Tasks.Where(t => t.Status == TaskStatus.Completed).ToList();
            var inProgressTasks = context.Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList();

            var bookings = new List<Booking>();

            // Bookings for completed tasks
            foreach (var task in completedTasks)
            {
                var worker = task.Title.Contains("Bathroom") ? fatmaUser :
                             task.Title.Contains("Office") ? mohamedUser :
                             task.Title.Contains("Carpet") ? fatmaUser :
                             task.Title.Contains("Kitchen") ? mohamedUser : workerUser;

                if (worker != null)
                {
                    bookings.Add(new Booking(
                        task.Budget,
                        task.AssignedAt,
                        task.CompletedAt,
                        task.Id,
                        worker.Id,
                        task.PosterId
                    )
                    {
                        Status = BookingStatus.Completed,
                        LastUpdateRate = task.CompletedAt ?? DateTime.UtcNow
                    });
                }
            }

            // Bookings for in-progress tasks
            foreach (var task in inProgressTasks)
            {
                var worker = task.Title.Contains("Electrical") ? mohamedUser :
                             task.Title.Contains("Furniture") ? workerUser :
                             task.Title.Contains("Data") ? mohamedUser : fatmaUser;

                if (worker != null)
                {
                    var startTime = DateTime.UtcNow.AddDays(-1);
                    var booking = new Booking(
                        task.Budget,
                        startTime,
                        null,
                        task.Id,
                        worker.Id,
                        task.PosterId
                    )
                    {
                        Status = BookingStatus.InProgress,
                        LastUpdateRate = startTime
                    };
                    bookings.Add(booking);
                }
            }

            context.Bookings.AddRange(bookings);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} sample bookings", bookings.Count);
        }

        private static async Task SeedPaymentsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Payments.AnyAsync())
            {
                logger.LogInformation("Payments already exist, skipping");
                return;
            }

            var completedBookings = context.Bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
            var inProgressBookings = context.Bookings.Where(b => b.Status == BookingStatus.InProgress).ToList();

            var payments = new List<Payment>();

            // Payments for completed bookings (released)
            foreach (var booking in completedBookings)
            {
                payments.Add(new Payment
                {
                    UserId = booking.WorkerId,
                    BookingId = booking.Id,
                    Amount = booking.AgreedRate,
                    Currency = "USD",
                    PaymentMethod = "CreditCard",
                    Status = PaymentStatus.Released,
                    PaymentDate = booking.CreatedAt,
                    ProcessedDate = booking.EndTime,
                    ReleasedAt = booking.EndTime,
                    CreatedAt = booking.CreatedAt
                });
            }

            // Payments for in-progress bookings (held)
            foreach (var booking in inProgressBookings)
            {
                payments.Add(new Payment
                {
                    UserId = booking.WorkerId,
                    BookingId = booking.Id,
                    Amount = booking.AgreedRate,
                    Currency = "USD",
                    PaymentMethod = "CreditCard",
                    Status = PaymentStatus.Held,
                    PaymentDate = booking.CreatedAt,
                    CreatedAt = booking.CreatedAt
                });
            }

            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} sample payments", payments.Count);
        }

        private static async Task SeedRatingsAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger logger)
        {
            if (await context.Ratings.AnyAsync())
            {
                logger.LogInformation("Ratings already exist, skipping");
                return;
            }

            var completedBookings = context.Bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
            var adminUser = await userManager.FindByEmailAsync("admin@labormarketplace.com");

            var ratings = new List<Rating>();

            var reviewTemplates = new[]
            {
                new { Score = 5m, Comment = "Excellent work! Very professional and on time. Highly recommended!" },
                new { Score = 5m, Comment = "Great job! The work was done perfectly. Will hire again." },
                new { Score = 4m, Comment = "Good work overall. Slight delay but quality was good." },
                new { Score = 5m, Comment = "Amazing service! Very satisfied with the results." },
                new { Score = 4m, Comment = "Nice work, arrived on time and completed the job as requested." }
            };

            var i = 0;
            foreach (var booking in completedBookings)
            {
                var review = reviewTemplates[i % reviewTemplates.Length];
                ratings.Add(new Rating
                {
                    RaterId = booking.PosterId ?? adminUser?.Id ?? "",
                    RateeId = booking.WorkerId,
                    Score = review.Score,
                    Comment = review.Comment,
                    bookingId = booking.Id,
                    CreatedAt = booking.EndTime ?? DateTime.UtcNow.AddDays(-1)
                });
                i++;
            }

            context.Ratings.AddRange(ratings);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} sample ratings", ratings.Count);
        }
    }
}
