using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Twilio implementation of SMS service for production use
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
            _fromPhoneNumber = configuration["Twilio:PhoneNumber"] ?? "";

            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken))
            {
                TwilioClient.Init(accountSid, authToken);
                _isConfigured = true;
                _logger.LogInformation("Twilio SMS service initialized");
            }
            else
            {
                _isConfigured = false;
                _logger.LogWarning("Twilio not configured. SMS will not be sent.");
            }
        }

        /// <summary>
        /// Send an SMS message to a phone number
        /// </summary>
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (!IsValidPhoneNumber(phoneNumber))
            {
                _logger.LogWarning("Invalid phone number format: {PhoneNumber}", phoneNumber);
                return false;
            }

            if (!_isConfigured)
            {
                _logger.LogError("Twilio is not configured. Cannot send SMS.");
                return false;
            }

            try
            {
                var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
                {
                    From = new PhoneNumber(_fromPhoneNumber),
                    Body = message
                };

                var msg = await MessageResource.CreateAsync(messageOptions);

                if (msg.Status == MessageResource.StatusEnum.Queued ||
                    msg.Status == MessageResource.StatusEnum.Sending ||
                    msg.Status == MessageResource.StatusEnum.Sent)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. SID: {Sid}", phoneNumber, msg.Sid);
                    return true;
                }

                _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {Status}, Error: {Error}",
                    phoneNumber, msg.Status, msg.ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        /// <summary>
        /// Validate phone number format
        /// </summary>
        public bool IsValidPhoneNumber(string phoneNumber, string countryCode = "+20")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove any non-digit characters except +
            var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

            // Check if it starts with country code or can be formatted
            if (cleaned.StartsWith(countryCode))
            {
                // Country code + remaining digits should be valid length
                var digitsOnly = cleaned.Substring(countryCode.Length);
                return digitsOnly.Length >= 8 && digitsOnly.Length <= 12;
            }
            else if (!cleaned.StartsWith("+"))
            {
                // Assume it's a local number, add country code
                var withCountryCode = countryCode + cleaned;
                var digitsOnly = withCountryCode.Substring(countryCode.Length);
                return digitsOnly.Length >= 8 && digitsOnly.Length <= 12;
            }

            return false;
        }

        /// <summary>
        /// Format phone number to E.164 standard
        /// </summary>
        public string FormatPhoneNumber(string phoneNumber, string countryCode = "+20")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove any non-digit characters except +
            var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

            // If already has country code, return as is
            if (cleaned.StartsWith(countryCode))
                return cleaned;

            // If has different country code, return as is
            if (cleaned.StartsWith("+") && !cleaned.StartsWith(countryCode))
                return cleaned;

            // Add country code
            return countryCode + cleaned;
        }
    }
}
