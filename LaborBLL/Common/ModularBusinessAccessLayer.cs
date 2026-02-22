using LaborBLL.Mapping;
using LaborBLL.Service;
using LaborBLL.Service.Abstract;
using LaborBLL.Service.Abstract.Rating;
using LaborBLL.Service.Implementation;
using LaborBLL.Service.Implementation.Rating;

namespace LaborBLL.Common
{
    public static class ModularBusinessAccessLayer
    {
        public static IServiceCollection AddModularBusinessLogicLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(x => x.AddProfile(new AutoMapperProfile()));

            // Register services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IVerificationService, VerificationService>();

            // Booking service
            services.AddScoped<IBookingService, BookingService>();

            // Task and Application services
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IApplicationService, ApplicationService>();

            // Dispute service
            services.AddScoped<IDisputeService, DisputeService>();
            services.AddScoped<IRatingService, RatingService>();


            //  services.AddScoped<IEmailService, EmailService>();

            //   services.AddScoped<IStripePaymentService, StripePaymentService>();

            return services;
        }
    }
}
