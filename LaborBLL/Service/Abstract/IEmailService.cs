namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Email notification service interface
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send a basic email
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null);

        /// <summary>
        /// Send email using a template
        /// </summary>
        Task<bool> SendTemplatedEmailAsync(string toEmail, string templateId, object templateData);

        /// <summary>
        /// Send booking confirmation email to client
        /// </summary>
        Task<bool> SendBookingConfirmationAsync(string toEmail, string clientName, string taskTitle, string workerName, decimal amount);

        /// <summary>
        /// Send payment receipt email
        /// </summary>
        Task<bool> SendPaymentReceiptAsync(string toEmail, string clientName, decimal amount, string paymentId, DateTime paymentDate);

        /// <summary>
        /// Send verification code email
        /// </summary>
        Task<bool> SendVerificationCodeAsync(string toEmail, string userName, string verificationCode);

        /// <summary>
        /// Send task application notification to worker
        /// </summary>
        Task<bool> SendTaskApplicationNotificationAsync(string toEmail, string workerName, string taskTitle);

        /// <summary>
        /// Send dispute resolution notification
        /// </summary>
        Task<bool> SendDisputeResolutionAsync(string toEmail, string userName, string disputeId, string resolution);

        /// <summary>
        /// Send password reset email
        /// </summary>
        Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink);
    }
}
