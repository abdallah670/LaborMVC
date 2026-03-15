using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Mock SMS service for testing - logs messages to console instead of sending real SMS
    /// In production, replace with TwilioSmsService or similar
    /// </summary>
    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;

        // Store sent codes for verification (in-memory, per-instance)
        private static readonly Dictionary<string, string> _sentCodes = new();

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            // Extract verification code from message
            var code = ExtractCodeFromMessage(message);
            if (!string.IsNullOrEmpty(code))
            {
                _sentCodes[phoneNumber] = code;
            }

            // Log to console (simulating SMS sending)
            _logger.LogInformation("\n========== MOCK SMS ==========");
            _logger.LogInformation("To: {PhoneNumber}", phoneNumber);
            _logger.LogInformation("Message: {Message}", message);
            _logger.LogInformation("==============================\n");

            // In a real implementation, this would call Twilio or similar
            return Task.FromResult(true);
        }

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

        /// <summary>
        /// For testing: retrieve the code that was "sent" to a phone number
        /// </summary>
        public static string? GetSentCode(string phoneNumber)
        {
            var formattedNumber = new MockSmsService(null!).FormatPhoneNumber(phoneNumber);
            return _sentCodes.TryGetValue(formattedNumber, out var code) ? code : null;
        }

        /// <summary>
        /// For testing: clear stored codes
        /// </summary>
        public static void ClearCodes()
        {
            _sentCodes.Clear();
        }

        private string? ExtractCodeFromMessage(string message)
        {
            // Extract 6-digit code from message
            var match = Regex.Match(message, @"\b(\d{6})\b");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
