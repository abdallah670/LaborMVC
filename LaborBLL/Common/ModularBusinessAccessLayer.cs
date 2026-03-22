using Microsoft.Extensions.DependencyInjection;
using LaborBLL.Mapping;
using LaborBLL.Service;
using LaborBLL.Service.Abstract;
using LaborBLL.Service.Implementation;
using LaborBLL.Service.Implementation.Rating;

namespace LaborBLL.Common
{
    public static class ModularBusinessAccessLayer
    {
        public static IServiceCollection AddModularBusinessLogicLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(x => x.AddProfile(new AutoMapperProfile()));

            // Register caching service
            services.AddScoped<ICacheService, CacheService>();

            // Register notification services
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<ISmsService, TwilioSmsService>();

            // Register verification limit service (M6)
            // Note: ITaskRepository is already registered in AddModularDataAccessLayer
            services.AddScoped<IVerificationLimitService, VerificationLimitService>();

            // Register services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IVerificationService, VerificationService>();

            services.AddScoped<IchatService, ChatService>();
            // Booking service
            services.AddScoped<IBookingService, BookingService>();

            // Task and Application services
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IApplicationService, ApplicationService>();

            // Dispute service
            services.AddScoped<IDisputeService, DisputeService>();
            services.AddScoped<IRatingService, RatingService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IEscrowService, EscrowService>();

            // Payment-related services
            services.AddScoped<IPaymentAuditService, PaymentAuditService>();
            services.AddScoped<IPaymentRetryService, PaymentRetryService>();
            services.AddScoped<IPaymentReceiptService, PaymentReceiptService>();
            services.AddScoped<IStripeService, StripeService>();

            services.AddScoped<IMessageService, MessageService>();

            // Distributed Transaction Services (C1 - Saga Pattern Implementation)
            services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();
            services.AddScoped<ICompensationService, CompensationService>();
            services.AddScoped<IDistributedTransactionService, DistributedTransactionService>();

            // Background Jobs (C1 - Outbox Pattern & Transfer Queue)
            services.AddScoped<IOutboxProcessorJob, OutboxProcessorJob>();
            services.AddScoped<ITransferProcessorJob, TransferProcessorJob>();

            //Images
            services.AddScoped<IImageProcessingService, ImageProcessingService>();

            // Redesign tracking service
            services.AddScoped<IRedesignService, RedesignService>();

            // Storage service
            services.AddScoped<IStorageService, LocalStorageService>();

            return services;
        }
    }
}
