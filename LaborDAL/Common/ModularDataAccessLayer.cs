using LaborDAL.Repo.Abstract;
using LaborDAL.Repo.Implementation;

namespace LaborDAL.Common
{
    public static class ModularDataAccessLayer
    {
        public static IServiceCollection AddModularDataAccessLayer(this IServiceCollection services)
        {
          
            services.AddScoped<IAppUserRepository, AppUserRepository>();

            // Booking Repository
            services.AddScoped<IBookingRepo, BookingRepo>();

            // Task Repository
            services.AddScoped<ITaskRepository, TaskRepository>();

            // Add Unit of Work if you have it
            services.AddScoped<IUnitOfWork, UnitOfWork>();


            return services;
        }
    }
}
