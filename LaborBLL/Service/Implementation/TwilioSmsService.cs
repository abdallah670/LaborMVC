using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Twilio implementation of SMS service
    /// </summary>
    public class TwilioSmsService : ISmsService
    {
        private readonly ILogger<TwilioSmsService> _logger;
        private readonly string _fromPhoneNumber;
        private readonly bool _isConfigured;

        public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
        {
            _logger = logger;
            
            var accountSid = configuration["Twilio:AccountSid"];
            var authToken = configuration["Twilio:AuthToken"];
            _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"] ?? "";

            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken))
            {
                TwilioClient.Init(accountSid, authToken);
                _isConfigured = true;
                _logger.LogInformation("Twilio SMS service initialized");
            }
            else
            {
                _isConfigured = false;
                _logger.LogWarning("Twilio not configured. SMS will be logged but not sent.");
            }
        }

        /// <summary>
        /// Send a basic SMS message
        /// </summary>
        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
        {
            if (!ValidatePhoneNumber(toPhoneNumber))
            {
                _logger.LogWarning("Invalid phone number format: {PhoneNumber}", toPhoneNumber);
                return false;
            }

            try
            {
                if (!_isConfigured)
                {
                    _logger.LogInformation("[SMS MOCK] To: {Phone}, Message: {Message}", toPhoneNumber, message);
                    return true;
                }

                var messageOptions = new CreateMessageOptions(new PhoneNumber(toPhoneNumber))
                {
                    From = new PhoneNumber(_fromPhoneNumber),
                    Body = message
                };

                var msg = await MessageResource.CreateAsync(messageOptions);
                
                if (msg.Status == MessageResource.StatusEnum.Queued || 
                    msg.Status == MessageResource.StatusEnum.Sending ||
                    msg.Status == MessageResource.StatusEnum.Sent)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. SID: {Sid}", toPhoneNumber, msg.Sid);
                    return true;
                }

                _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {Status}, Error: {Error}", 
                    toPhoneNumber, msg.Status, msg.ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending SMS to {PhoneNumber}", toPhoneNumber);
                return false;
            }
        }

        /// <summary>
        /// Send verification code via SMS
        /// </summary>
        public async Task<bool> SendVerificationCodeAsync(string toPhoneNumber, string verificationCode)
        {
            var message = $"Your Labor Marketplace verification code is: {verificationCode}. This code expires in 15 minutes.";
            return await SendSmsAsync(toPhoneNumber, message);
        }

        /// <summary>
        /// Send booking confirmation SMS
        /// </summary>
        public async Task<bool> SendBookingConfirmationAsync(string toPhoneNumber, string taskTitle, string workerName)
        {
            var message = $"Your booking for '{taskTitle}' with {workerName} has been confirmed. View details in the app.";
            return await SendSmsAsync(toPhoneNumber, message);
        }

        /// <summary>
        /// Send task reminder SMS
        /// </summary>
        public async Task<bool> SendTaskReminderAsync(string toPhoneNumber, string taskTitle, DateTime taskDate)
        {
            var message = $"Reminder: Your task '{taskTitle}' is scheduled for {taskDate:MMM dd} at {taskDate:HH:mm}.";
            return await SendSmsAsync(toPhoneNumber, message);
        }

        /// <summary>
        /// Send payment notification SMS
        /// </summary>
        public async Task<bool> SendPaymentNotificationAsync(string toPhoneNumber, decimal amount, string status)
        {
            var message = $"Payment of ${amount:F2} has been {status.ToLower()}. Check the app for details.";
            return await SendSmsAsync(toPhoneNumber, message);
        }

        /// <summary>
        /// Validate phone number format (E.164)
        /// </summary>
        public bool ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // E.164 format: + followed by 1-15 digits
            // Examples: +1234567890, +14155552671
            var pattern = @"^\+[1-9]\d{1,14}$";
            return Regex.IsMatch(phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", ""), pattern);
        }

        /// <summary>
        /// Format phone number to E.164 standard
        /// </summary>
        public string? FormatPhoneNumber(string phoneNumber, string countryCode = "1")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            // Remove all non-digit characters
            var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

            // If it starts with the country code, just add +
            if (digitsOnly.StartsWith(countryCode) && digitsOnly.Length > countryCode.Length)
            {
                return $"+{digitsOnly}";
            }

            // If it's 10 digits (US/Canada without country code), add +1
            if (digitsOnly.Length == 10)
            {
                return $"+{countryCode}{digitsOnly}";
            }

            // If it's already valid, return as is
            if (digitsOnly.Length > 10)
            {
                return $"+{digitsOnly}";
            }

            return null;
        }
    }
}
