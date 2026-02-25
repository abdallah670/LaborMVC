using LaborDAL.Enums;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace LaborBLL.Service.Implementation
{
    public class PaymentService : IPaymentService
    {
        public IUnitOfWork UnitOfWork { get; }
        public IMapper Mapper { get; }

        private readonly IStripeService _stripeService;
        private readonly IPaymentRetryService _retryService;
        private readonly IPaymentAuditService _auditService;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IStripeService stripeService,
            IPaymentRetryService retryService, IPaymentAuditService auditService)
        {
            UnitOfWork = unitOfWork;
            Mapper = mapper;
            _stripeService = stripeService;
            _retryService = retryService;
            _auditService = auditService;
        }


        #region Crud Operation
        public async Task<Response<PaymentVM>> GetPaymentByBookingIdAsync(int bookingId)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                if (payment == null)
                {
                    return new Response<PaymentVM>(null, false, "Payment not found for the given Booking ID.");
                }
                var paymentVM = Mapper.Map<PaymentVM>(payment);
                return new Response<PaymentVM>(paymentVM,true,null);

            }
            catch (Exception ex)
            {
                return new Response<PaymentVM>(null,false, $"Error in getting payment by Booking ID: {ex.Message}");
            }
        }

        public async Task<Response<PaymentVM>> CreateAsync(PaymentVM model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.TransactionId))
                {
                    // Generate unique idempotency key for this payment attempt
                    string idempotencyKey = $"{model.BookingId}_{model.UserId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
                    
                    var Intent = await _stripeService.CreatePaymentIntentAsync(
                        (double)model.Amount, 
                        model.Currency ?? "usd", 
                        model.Description ?? "Booking Payment",
                        model.BookingId,
                        idempotencyKey);
                    
                    model.TransactionId = Intent.PaymentIntentId; 
                    var paymentEntity = Mapper.Map<Payment>(model);
                    paymentEntity.PaymentDate = DateTime.UtcNow;
                    paymentEntity.Status = PaymentStatus.Pending;
                    paymentEntity.PaymentType = model.PaymentType??"Booking";
                    paymentEntity.Notes = $"IdempotencyKey: {idempotencyKey}"; // Store for reference
                    
                    await UnitOfWork.Payments.AddAsync(paymentEntity);
                    await UnitOfWork.SaveAsync();
                    
                    if (paymentEntity.Id > 0)
                    {
                        var paymentVM = Mapper.Map<PaymentVM>(paymentEntity);
                        return new Response<PaymentVM>(paymentVM, true, null);
                    }
                    else
                    {
                        return new Response<PaymentVM>(null, false, "Failed to create payment record.");
                    }
                }
                else
                {
                    return new Response<PaymentVM>(null, false, "Failed to create payment record.");
                }
            }
            catch (Exception ex)
            {
                return new Response<PaymentVM>(null, false, $"Error in creating payment: {ex.Message}");
            }
        }

        public async Task<Response<PaymentVM>> GetByIdAsync(int id)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetByIdAsync(id);
                if (payment != null)
                {
                    var vm = Mapper.Map<PaymentVM>(payment);
                    return new Response<PaymentVM>(vm,true,null);
                }
                return new Response<PaymentVM>(null,false, "Payment not found.");
            }
            catch (Exception ex)
            {
                return new Response<PaymentVM>(null, false,$"Error: {ex.Message}");
            }
        }

        public async Task<Response<List<PaymentVM>>> GetAllAsync()
        {
            try
            {
                var payments = await UnitOfWork.Payments.GetAllAsync();
                var paymentVMs = Mapper.Map<List<PaymentVM>>(payments);
                return new Response<List<PaymentVM>>(paymentVMs, true, null);
            }
            catch (Exception ex)
            {
                return new Response<List<PaymentVM>>(null, false, $"Error in getting all payments: {ex.Message}");
            }
        }

        public async Task<Response<List<PaymentVM>>> GetByUserIdAsync(string userId)
        {
            try
            {
                var payments = await UnitOfWork.Payments.GetPaymentsByUserIdAsync(userId);
                var paymentVMs = Mapper.Map<List<PaymentVM>>(payments);
                return new Response<List<PaymentVM>>(paymentVMs, true, null);
            }
            catch (Exception ex)
            {
                return new Response<List<PaymentVM>>(null, false, $"Error in getting payments by User ID: {ex.Message}");
            }
        }
        public async Task<Response<List<PaymentVM>>> GetByStatusAsync(string status)
        {
            try
            {
              

                var payments = await UnitOfWork.Payments.GetPaymentsByStatusAsync(status);
                var paymentVMs = Mapper.Map<List<PaymentVM>>(payments);
                return new Response<List<PaymentVM>>(paymentVMs, true, null);

            }
            catch (Exception ex)
            {
                return new Response<List<PaymentVM>>(null, false, $"Error in getting payments by status: {ex.Message}");
            }
        }

        public async Task<Response<PaymentVM>> UpdateAsync(PaymentVM model)
        {
            try
            {
                var paymentEntity = Mapper.Map<Payment>(model);
                await UnitOfWork.Payments.UpdateAsync(paymentEntity);
                await UnitOfWork.SaveAsync();
                var paymentVM = Mapper.Map<PaymentVM>(paymentEntity);
                return new Response<PaymentVM>(paymentVM, true, null);
            }
            catch (Exception ex)
            {
                return new Response<PaymentVM>(null, false, $"Error in updating payment: {ex.Message}");

            }

        }
        public async Task<Response<bool>> DeleteAsync(int id)
        {
            try
            {
                var payment = UnitOfWork.Payments.GetByIdAsync(id).Result;
                if (payment == null)
                {
                    return new Response<bool>(false, false, "Payment not found.");
                }
                await UnitOfWork.Payments.RemoveAsync(payment);
                await UnitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error in deleting payment: {ex.Message}");
            }
        }
        #endregion

        public async Task<Response<bool>> ProcessPaymentAsync(int id)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetByIdAsync(id);
                if (payment == null)
                {
                    return new Response<bool>(false,false, "Payment not found.");
                }
                if (payment.Status != PaymentStatus.Held)
                {
                    return new Response<bool>(false,false, "Only payments in 'Held' status can be processed.");
                }
                await _stripeService.CapturePaymentIntentAsync(payment.TransactionId);
                payment.Status = PaymentStatus.Released;
                payment.ReleasedAt = DateTime.UtcNow;
                await UnitOfWork.Payments.UpdateAsync(payment);
                await UnitOfWork.SaveAsync();
                return new Response<bool>(true,true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false,false, $"Error in processing payment: {ex.Message}");
            }
        }
       
        public async Task<Response<bool>> CapturePaymentAsync(int bookingId)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                if (payment == null)
                {
                    return new Response<bool>(false, false, "Payment not found for the given Booking ID.");
                }
                if (payment.Status != PaymentStatus.Held)
                {
                    return new Response<bool>(false, false, "Only payments in 'Held' status can be captured.");
                }
                await _stripeService.CapturePaymentIntentAsync(payment.TransactionId);
                payment.Status = PaymentStatus.Released;
                payment.ReleasedAt = DateTime.UtcNow;
                await UnitOfWork.Payments.UpdateAsync(payment);
                await UnitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error in capturing payment: {ex.Message}");
            }

        }

        public async Task<Response<bool>> PartialRefundAsync(int Id, decimal amount)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetByIdAsync(Id);
                if (payment == null)
                {
                    return new Response<bool>(false, false, "Payment not found.");
                }
                if (payment.Status != PaymentStatus.Held && payment.Status != PaymentStatus.Released)
                {
                    return new Response<bool>(false, false, "Payment cannot be refunded. Invalid status.");
                }
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = payment.TransactionId,
                    Amount = (long)(amount * 100), // Stripe expects amounts in cents
                };
                var refundService = new RefundService();
                await refundService.CreateAsync(refundOptions);
                payment.Status = PaymentStatus.Refunded;
                payment.UpdatedAt = DateTime.UtcNow;
                await UnitOfWork.Payments.UpdateAsync(payment);
                await UnitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error in processing partial refund: {ex.Message}");
            }
        }
        public async Task<Response<bool>> RefundPaymentAsync(int id)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetByIdAsync(id);
                if (payment == null)
                {
                    return new Response<bool>(false, false, "Payment not found.");
                }
                if (payment.Status != PaymentStatus.Held && payment.Status != PaymentStatus.Released)
                {
                    return new Response<bool>(false, false, "Payment cannot be refunded. Invalid status.");
                }
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = payment.TransactionId,
                    Amount = (long)(payment.Amount * 100), // Stripe expects amounts in cents
                };
                var refundService = new RefundService();
                await refundService.CreateAsync(refundOptions);
                payment.Status = PaymentStatus.Refunded;
                payment.UpdatedAt = DateTime.UtcNow;
                await UnitOfWork.Payments.UpdateAsync(payment);
                await UnitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error in processing refund: {ex.Message}");
            }
        }

     

        public async Task<Response<bool>> TransferToWorkerAsync(int paymentId, string workerStripeAccountId)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetByIdAsync(paymentId);
                var transferOptions = new TransferCreateOptions
                {
                    Amount = (long)((float)payment.Amount * 0.90 * 100), // 90% to worker (minus 10% fee)
                    Currency = "usd",
                    Destination = workerStripeAccountId,
                    TransferGroup = paymentId.ToString()
                };
                var transferService = new TransferService();
                var transfer = await transferService.CreateAsync(transferOptions);
                return new Response<bool>(true, true, null);

            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, $"Error in Transfering : {ex.Message}");

            }
        }

        public async Task<Response<PaymentVM>> GetPaymentStatusAsync(int bookingId)
        {
            try
            {
                var payment = await UnitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                if (payment == null)
                {
                    return new Response<PaymentVM>(null, false, "Payment not found for the given Booking ID.");
                }
              
                var paymentVM = Mapper.Map<PaymentVM>(payment);
                return new Response<PaymentVM>(paymentVM, true, null);
            }
            catch (Exception ex)
            {
                return new Response<PaymentVM>(null, false, $"Error in getting payment: {ex.Message}");
            }
        }

     
    }
}