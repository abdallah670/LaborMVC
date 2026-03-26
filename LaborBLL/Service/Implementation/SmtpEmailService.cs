using System.Net;
using System.Net.Mail;
using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// SMTP implementation of email service using Gmail or other SMTP provider
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _logger = logger;
            
            // Try EmailSettings first, fallback to SendGrid settings
            _smtpHost = configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
            _smtpUser = configuration["EmailSettings:SmtpUser"] ?? "";
            _smtpPass = configuration["EmailSettings:SmtpPass"] ?? "";
            _fromEmail = configuration["EmailSettings:FromEmail"] ?? configuration["SendGrid:FromEmail"] ?? "noreply@labormarketplace.com";
            _fromName = configuration["EmailSettings:FromName"] ?? configuration["SendGrid:FromName"] ?? "Labor Marketplace";
        }

        /// <summary>
        /// Send a basic email
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_fromEmail, _fromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = htmlContent;
                message.IsBodyHtml = true;

                if (!string.IsNullOrEmpty(plainTextContent))
                {
                    message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                        plainTextContent, null, "text/plain"));
                }

                using var client = new SmtpClient(_smtpHost, _smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);

                await client.SendMailAsync(message);
                
                _logger.LogInformation("Email sent successfully to {Email} via SMTP", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} via SMTP", toEmail);
                return false;
            }
        }

        /// <summary>
        /// Send email using a template (simulated with HTML content)
        /// </summary>
        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateId, object templateData)
        {
            // For SMTP, we don't have template support, so we just log and return
            _logger.LogWarning("Templated email not supported in SMTP implementation. TemplateId: {TemplateId}", templateId);
            return false;
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
        public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink)
        {
            var subject = "Password Reset Request - Labor Marketplace";
            
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
