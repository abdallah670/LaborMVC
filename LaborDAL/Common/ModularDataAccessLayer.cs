using LaborDAL.Repo;
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
            services.AddScoped<IchatRepo, chatRepo>();

            // Task Repository
            services.AddScoped<ITaskRepository, TaskRepository>();

            // Dispute Repository
            services.AddScoped<IDisputeRepo, DisputeRepo>();

            // Add Unit of Work if you have it
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPaymentRepo, PaymentRepo>();
            services.AddScoped<IMessageRepo, MessageRepo>();
            services.AddScoped<IRatingRepo,RatingRepo>();

            // Distributed Transaction Repositories (C1 - Saga Pattern & Outbox Pattern)
            services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
            services.AddScoped<IPendingTransferRepository, PendingTransferRepository>();
            services.AddScoped<ISagaRepository, SagaRepository>();


            return services;
        }
    }
}
