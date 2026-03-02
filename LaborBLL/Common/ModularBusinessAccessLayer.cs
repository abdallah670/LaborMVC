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

            services.AddScoped<IchatService, ChatService>       ();
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



            //  services.AddScoped<IEmailService, EmailService>();

            //   services.AddScoped<IStripePaymentService, StripePaymentService>();

            return services;
        }
    }
}
