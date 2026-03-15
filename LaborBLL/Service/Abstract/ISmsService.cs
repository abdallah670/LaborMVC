namespace LaborBLL.Service.Abstract
{
    /// <summary>
    /// Service for sending SMS messages
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Send an SMS message to a phone number
        /// </summary>
        /// <param name="phoneNumber">Phone number in E.164 format (e.g., +201234567890)</param>
        /// <param name="message">Message body</param>
        /// <returns>True if message was sent successfully</returns>
        Task<bool> SendSmsAsync(string phoneNumber, string message);

        /// <summary>
        /// Validate phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <param name="countryCode">Country code (e.g., +20 for Egypt)</param>
        /// <returns>True if phone number is valid</returns>
        bool IsValidPhoneNumber(string phoneNumber, string countryCode = "+20");

        /// <summary>
        /// Format phone number to E.164 standard
        /// </summary>
        /// <param name="phoneNumber">Raw phone number</param>
        /// <param name="countryCode">Country code</param>
        /// <returns>Formatted phone number</returns>
        string FormatPhoneNumber(string phoneNumber, string countryCode = "+20");
    }
}
