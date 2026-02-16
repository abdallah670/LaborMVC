

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

            // Booking service
            services.AddScoped<IBookingService, BookingService>();


            //  services.AddScoped<IEmailService, EmailService>();

            //   services.AddScoped<IStripePaymentService, StripePaymentService>();

            return services;
        }
    }
}
