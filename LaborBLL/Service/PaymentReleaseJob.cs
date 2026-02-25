using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.Service
{

    public class PaymentReleaseJob
    {
        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentReleaseJob(IPaymentService paymentService, IUnitOfWork unitOfWork)
        {
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task AutoReleasePayments()
        {
            var pendingPayments = await _unitOfWork.Payments
                .GetPaymentsPendingReleaseAsync(TimeSpan.FromHours(24));

            foreach (var payment in pendingPayments)
            {
                await _paymentService.CapturePaymentAsync(payment.BookingId);
            }
        }
    }

}
