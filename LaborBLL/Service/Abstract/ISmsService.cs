namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// SMS notification service interface
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Send a basic SMS message
        /// </summary>
        Task<bool> SendSmsAsync(string toPhoneNumber, string message);

        /// <summary>
        /// Send verification code via SMS
        /// </summary>
        Task<bool> SendVerificationCodeAsync(string toPhoneNumber, string verificationCode);

        /// <summary>
        /// Send booking confirmation SMS
        /// </summary>
        Task<bool> SendBookingConfirmationAsync(string toPhoneNumber, string taskTitle, string workerName);

        /// <summary>
        /// Send task reminder SMS
        /// </summary>
        Task<bool> SendTaskReminderAsync(string toPhoneNumber, string taskTitle, DateTime taskDate);

        /// <summary>
        /// Send payment notification SMS
        /// </summary>
        Task<bool> SendPaymentNotificationAsync(string toPhoneNumber, decimal amount, string status);

        /// <summary>
        /// Validate phone number format
        /// </summary>
        bool ValidatePhoneNumber(string phoneNumber);
    }
}
