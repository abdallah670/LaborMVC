using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// SendGrid implementation of email service
    /// </summary>
    public class SendGridEmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<SendGridEmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
        {
            _logger = logger;
            var apiKey = configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
            _sendGridClient = new SendGridClient(apiKey);
            _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@labormarketplace.com";
            _fromName = configuration["SendGrid:FromName"] ?? "Labor Marketplace";
        }

        /// <summary>
        /// Send a basic email
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent ?? "", htmlContent);
                
                var response = await _sendGridClient.SendEmailAsync(msg);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                    return true;
                }
                
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Error: {Error}", 
                    toEmail, response.StatusCode, errorBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending email to {Email}", toEmail);
                return false;
            }
        }

        /// <summary>
        /// Send email using a SendGrid template
        /// </summary>
        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateId, object templateData)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = new SendGridMessage
                {
                    From = from,
                    TemplateId = templateId
                };
                msg.AddTo(to);
                msg.SetTemplateData(templateData);

                var response = await _sendGridClient.SendEmailAsync(msg);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Templated email sent successfully to {Email}", toEmail);
                    return true;
                }
                
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send templated email to {Email}. Status: {StatusCode}, Error: {Error}", 
                    toEmail, response.StatusCode, errorBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending templated email to {Email}", toEmail);
                return false;
            }
        }

        /// <summary>
        /// Send booking confirmation email
        /// </summary>
        public async Task<bool> SendBookingConfirmationAsync(string toEmail, string clientName, string taskTitle, string workerName, decimal amount)
        {
            var subject = "Booking Confirmed - Labor Marketplace";
            var htmlContent = $@"
                <h2>Booking Confirmation</h2>
                <p>Hello {clientName},</p>
                <p>Your booking has been confirmed!</p>
                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3>Booking Details</h3>
                    <p><strong>Task:</strong> {taskTitle}</p>
                    <p><strong>Worker:</strong> {workerName}</p>
                    <p><strong>Amount:</strong> ${amount:F2}</p>
                </div>
                <p>You can track your booking progress in your dashboard.</p>
                <p>Thank you for using Labor Marketplace!</p>
            ";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send payment receipt email
        /// </summary>
        public async Task<bool> SendPaymentReceiptAsync(string toEmail, string clientName, decimal amount, string paymentId, DateTime paymentDate)
        {
            var subject = "Payment Receipt - Labor Marketplace";
            var htmlContent = $@"
                <h2>Payment Receipt</h2>
                <p>Hello {clientName},</p>
                <p>Thank you for your payment. Here are the details:</p>
                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Payment ID:</strong> {paymentId}</p>
                    <p><strong>Amount:</strong> ${amount:F2}</p>
                    <p><strong>Date:</strong> {paymentDate:yyyy-MM-dd HH:mm:ss}</p>
                    <p><strong>Status:</strong> Completed</p>
                </div>
                <p>If you have any questions, please contact our support team.</p>
            ";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send verification code email
        /// </summary>
        public async Task<bool> SendVerificationCodeAsync(string toEmail, string userName, string verificationCode)
        {
            var subject = "Your Verification Code - Labor Marketplace";
            var htmlContent = $@"
                <h2>Email Verification</h2>
                <p>Hello {userName},</p>
                <p>Your verification code is:</p>
                <div style='background-color: #e3f2fd; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0;'>
                    <h1 style='color: #1976d2; letter-spacing: 5px; margin: 0;'>{verificationCode}</h1>
                </div>
                <p>This code will expire in 15 minutes.</p>
                <p>If you didn't request this code, please ignore this email.</p>
            ";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send task application notification
        /// </summary>
        public async Task<bool> SendTaskApplicationNotificationAsync(string toEmail, string workerName, string taskTitle)
        {
            var subject = "New Task Application - Labor Marketplace";
            var htmlContent = $@"
                <h2>New Application Received</h2>
                <p>Hello,</p>
                <p><strong>{workerName}</strong> has applied for your task:</p>
                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Task:</strong> {taskTitle}</p>
                </div>
                <p>Review the application in your dashboard to accept or decline.</p>
            ";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send dispute resolution notification
        /// </summary>
        public async Task<bool> SendDisputeResolutionAsync(string toEmail, string userName, string disputeId, string resolution)
        {
            var subject = "Dispute Resolution - Labor Marketplace";
            var htmlContent = $@"
                <h2>Dispute Resolved</h2>
                <p>Hello {userName},</p>
                <p>Your dispute <strong>#{disputeId}</strong> has been resolved.</p>
                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Resolution:</strong> {resolution}</p>
                </div>
                <p>If you have any questions about this resolution, please contact support.</p>
            ";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send password reset email
        /// </summary>
        public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetToken)
        {
            var subject = "Password Reset Request - Labor Marketplace";
            var resetLink = $"https://yourdomain.com/account/reset-password?token={resetToken}&email={Uri.EscapeDataString(toEmail)}";
            
            var htmlContent = $@"
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>We received a request to reset your password. Click the link below to proceed:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #1976d2; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a>
                </div>
                <p>Or copy and paste this link: {resetLink}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
            ";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }
    }
}
