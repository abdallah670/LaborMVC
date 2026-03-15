using Hangfire;
using LaborBLL.Common;
using LaborBLL.Hubs;
using LaborBLL.Mapping;
using LaborBLL.Service;
using LaborBLL.Service.Abstract;
using LaborBLL.Service.Implementation;
using LaborDAL.Common;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using LaborDAL.Repo.Implementation;
using LaborPL.HealthChecks;
using LaborPL.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Serilog;
using Stripe;



var builder = WebApplication.CreateBuilder(args);
 Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSignalR();

// Add services to the container.
builder.Services.AddControllersWithViews();
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

// Configure DbContext with resilience
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.UseNetTopologySuite();
            // Enable retry on failure for transient faults
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// Configure Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
  builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowSpecificOrigins",
          policy =>
          {
              policy.WithOrigins(
                      "https://localhost:139",
                      "http://localhost:139",
                      "https://localhost:5001",
                      "http://localhost:5000")
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
          });
  });
      builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

// Configure Identity cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add logging
builder.Services.AddLogging();
builder.Services.AddHangfire(x => x.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// 2. API Rate Limiting - Prevent brute force and abuse
builder.Services.AddRateLimiter(options =>
{
    // General API limit: 100 requests per minute per client
    options.AddFixedWindowLimiter("General", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    // Strict limit for login endpoints: 5 attempts per 5 minutes
    options.AddFixedWindowLimiter("Login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(5);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });

    // Payment endpoints: 10 requests per minute
    options.AddFixedWindowLimiter("Payment", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 5;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

// 4. Response Compression - Improve performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/css", "application/javascript" });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// 5. Health Checks - Monitor system health
builder.Services.AddHealthChecks()
    .AddCheck<ApplicationDbContextHealthCheck>("database", tags: new[] { "db" });

builder.Services.AddModularDataAccessLayer();
builder.Services.AddModularBusinessLogicLayer();

// Register Notification Service
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register File Upload Validation Service
builder.Services.Configure<FileUploadSecuritySettings>(
    builder.Configuration.GetSection("FileUpload"));
builder.Services.Configure<ZipValidationSettings>(
    builder.Configuration.GetSection("FileUpload:ZipValidation"));
builder.Services.Configure<ImageValidationSettings>(
    builder.Configuration.GetSection("FileUpload:ImageValidation"));
builder.Services.Configure<UploadRateLimitSettings>(
    builder.Configuration.GetSection("FileUpload:RateLimiting"));
builder.Services.Configure<FileUploadAuditSettings>(
    builder.Configuration.GetSection("FileUpload:Audit"));

// Register File Upload Security Services
builder.Services.AddScoped<IFileUploadValidationService, FileUploadValidationService>();
builder.Services.AddScoped<IZipSecurityValidator, ZipSecurityValidator>();
builder.Services.AddScoped<IImageValidationService, ImageValidationService>();
builder.Services.AddScoped<IUserUploadRateLimiter, UserUploadRateLimiter>();
builder.Services.AddScoped<IFileUploadAuditRepo, FileUploadAuditRepo>();
builder.Services.AddScoped<IFileUploadAuditService, FileUploadAuditService>();
builder.Services.AddScoped<IContentInspector, ContentInspector>();

// Add Memory Cache for rate limiting
builder.Services.AddMemoryCache();

// Register Image Processing and Storage Services
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();

// Register Verification Service
builder.Services.AddScoped<IVerificationService, VerificationService>();

// Register SMS Service - Using Twilio for production
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
             
var app = builder.Build();

// ✅ لازم يكون أول حاجة
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/StripeWebhook"))
    {
        context.Request.EnableBuffering();
    }
    await next();
});
// Seed database with roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await LaborDAL.DB.DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// 4. Global Error Handling
app.UseGlobalExceptionHandler();

// 6. Audit Logging - Log all requests
app.UseAuditLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 3. Security Headers - Add security headers to all responses
app.UseSecurityHeaders();

// 4. Response Compression
app.UseResponseCompression();

// 2. Rate Limiting
app.UseRateLimiter();

// And this:
app.UseHangfireDashboard();
app.UseHangfireServer();

// Schedule recurring jobs after Hangfire is initialized
RecurringJob.AddOrUpdate<PaymentReleaseJob>(
    "auto-release-payments",
    job => job.AutoReleasePayments(),
    Cron.Hourly);

// Schedule notification processing job
RecurringJob.AddOrUpdate<INotificationService>(
    "process-notifications",
    service => service.ProcessPendingNotificationsAsync(50),
    Cron.Minutely);

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowSpecificOrigins"); // ✅ أضيف هنا

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<DirectChatHub>("/DirectChatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// 5. Health Checks endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration,
                Exception = e.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.Run();
