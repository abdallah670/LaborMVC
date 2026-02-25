using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using System.Text;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for generating payment receipts and invoices
    /// </summary>
    public interface IPaymentReceiptService
    {
        Task<byte[]> GenerateReceiptPdfAsync(int paymentId);
        Task<string> GenerateReceiptHtmlAsync(int paymentId);
        Task<bool> SendReceiptEmailAsync(int paymentId, string email);
        Task<byte[]> GenerateInvoicePdfAsync(int paymentId);
    }

    public class PaymentReceiptService : IPaymentReceiptService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentReceiptService> _logger;

        public PaymentReceiptService(IUnitOfWork unitOfWork, ILogger<PaymentReceiptService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(int paymentId)
        {
            try
            {
                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(paymentId);
                if (payment == null)
                {
                    throw new Exception($"Payment {paymentId} not found");
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(payment.BookingId);
                var poster = await _unitOfWork.AppUsers.GetByIdAsync(payment.UserId);
                var worker = booking?.Worker;

                // For now, return HTML as bytes (in production, use a PDF library like iTextSharp or DinkToPdf)
                var html = await GenerateReceiptHtmlAsync(paymentId);
                return Encoding.UTF8.GetBytes(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate receipt PDF for PaymentId: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<string> GenerateReceiptHtmlAsync(int paymentId)
        {
            try
            {
                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(paymentId);
                if (payment == null)
                {
                    throw new Exception($"Payment {paymentId} not found");
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(payment.BookingId);
                var poster = await _unitOfWork.AppUsers.GetByIdAsync(payment.UserId);
                var worker = booking?.Worker;
                var task = booking?.Task;

                var platformFee = payment.Amount * 0.10m; // 10% fee
                var workerAmount = payment.Amount * 0.90m;  // 90% to worker

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Payment Receipt #{payment.Id}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ text-align: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 30px; }}
        .receipt-title {{ font-size: 24px; font-weight: bold; color: #333; }}
        .receipt-id {{ font-size: 14px; color: #666; margin-top: 5px; }}
        .section {{ margin-bottom: 25px; }}
        .section-title {{ font-size: 16px; font-weight: bold; color: #333; border-bottom: 1px solid #ddd; padding-bottom: 5px; margin-bottom: 10px; }}
        .row {{ display: flex; justify-content: space-between; margin-bottom: 8px; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #333; }}
        .amount-row {{ font-size: 16px; }}
        .total {{ font-size: 18px; font-weight: bold; border-top: 2px solid #333; padding-top: 10px; margin-top: 10px; }}
        .footer {{ margin-top: 40px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #ddd; padding-top: 20px; }}
        .status-badge {{ display: inline-block; padding: 5px 10px; border-radius: 4px; font-weight: bold; }}
        .status-released {{ background-color: #d4edda; color: #155724; }}
        .status-held {{ background-color: #fff3cd; color: #856404; }}
        .status-refunded {{ background-color: #f8d7da; color: #721c24; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='receipt-title'>Labor Marketplace</div>
        <div class='receipt-title' style='font-size: 20px; margin-top: 10px;'>Payment Receipt</div>
        <div class='receipt-id'>Receipt #: RCP-{payment.Id:D6}</div>
        <div class='receipt-id'>Transaction ID: {payment.TransactionId ?? "N/A"}</div>
        <div style='margin-top: 10px;'>
            <span class='status-badge status-{payment.Status.ToString().ToLower()}'>{payment.Status}</span>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>Payment Details</div>
        <div class='row'><span class='label'>Payment Date:</span> <span class='value'>{payment.PaymentDate:MMMM dd, yyyy HH:mm}</span></div>
        <div class='row'><span class='label'>Payment Method:</span> <span class='value'>{payment.PaymentMethod}</span></div>
        <div class='row'><span class='label'>Currency:</span> <span class='value'>{payment.Currency}</span></div>
        <div class='row'><span class='label'>Description:</span> <span class='value'>{payment.Description}</span></div>
    </div>

    <div class='section'>
        <div class='section-title'>Task Information</div>
        <div class='row'><span class='label'>Task:</span> <span class='value'>{task?.Title ?? "N/A"}</span></div>
        <div class='row'><span class='label'>Booking ID:</span> <span class='value'>#{payment.BookingId}</span></div>
    </div>

    <div class='section'>
        <div class='section-title'>Parties</div>
        <div class='row'><span class='label'>Client (Poster):</span> <span class='value'>{poster?.FirstName} {poster?.LastName}</span></div>
        <div class='row'><span class='label'>Worker:</span> <span class='value'>{worker?.FirstName} {worker?.LastName}</span></div>
    </div>

    <div class='section'>
        <div class='section-title'>Payment Breakdown</div>
        <div class='row amount-row'><span class='label'>Total Amount:</span> <span class='value'>${payment.Amount:N2}</span></div>
        <div class='row'><span class='label'>Platform Fee (10%):</span> <span class='value'>-${platformFee:N2}</span></div>
        <div class='row total'><span class='label'>Worker Receives:</span> <span class='value'>${workerAmount:N2}</span></div>
    </div>

    <div class='section'>
        <div class='section-title'>Status History</div>
        <div class='row'><span class='label'>Created:</span> <span class='value'>{payment.CreatedAt:MMM dd, yyyy HH:mm}</span></div>
        {(payment.ReleasedAt.HasValue ? $"<div class='row'><span class='label'>Released:</span> <span class='value'>{payment.ReleasedAt.Value:MMM dd, yyyy HH:mm}</span></div>" : "")}
    </div>

    <div class='footer'>
        <p>Thank you for using Labor Marketplace!</p>
        <p>This is an official receipt for your records.</p>
        <p>For questions, contact support@labormarketplace.com</p>
    </div>
</body>
</html>";

                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate receipt HTML for PaymentId: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> SendReceiptEmailAsync(int paymentId, string email)
        {
            try
            {
                var htmlReceipt = await GenerateReceiptHtmlAsync(paymentId);
                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(paymentId);

                // In a real implementation, use an email service like SendGrid, SMTP, etc.
                // For now, just log that the email would be sent
                _logger.LogInformation(
                    "Receipt email would be sent to {Email} for PaymentId: {PaymentId}. " +
                    "Subject: Payment Receipt #{ReceiptId}",
                    email,
                    paymentId,
                    payment?.Id
                );

                // TODO: Implement actual email sending
                // Example using SMTP:
                // await _emailService.SendAsync(email, $"Payment Receipt #{payment.Id}", htmlReceipt, isHtml: true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send receipt email for PaymentId: {PaymentId} to {Email}", paymentId, email);
                return false;
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int paymentId)
        {
            // Similar to receipt but with more business details
            // For now, return the same as receipt
            return await GenerateReceiptPdfAsync(paymentId);
        }
    }
}
