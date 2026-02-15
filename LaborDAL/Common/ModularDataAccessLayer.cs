







using LaborDAL.Repo.Abstract;
using LaborDAL.Repo.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace LaborDAL.Common
{
    public static class ModularDataAccessLayer
    {
        public static IServiceCollection AddModularDataAccessLayer(this IServiceCollection services)
        {
         
            services.AddScoped<IAppUserRepository, AppUserRepository>();
          
           
         

            // Add Unit of Work if you have it
             services.AddScoped<IUnitOfWork, UnitOfWork>();


            return services;
        }
    }
}
