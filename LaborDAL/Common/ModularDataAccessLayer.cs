


namespace LaborDAL.Common
{
    public static class ModularDataAccessLayer
    {
        public static IServiceCollection AddModularDataAccessLayer(this IServiceCollection services)
        {
         
            services.AddScoped<IAppUserRepository, AppUserRepository>();

            // Booking Repository
            services.AddScoped<IBookingRepo, BookingRepo>();


            // Add Unit of Work if you have it
            services.AddScoped<IUnitOfWork, UnitOfWork>();


            return services;
        }
    }
}
