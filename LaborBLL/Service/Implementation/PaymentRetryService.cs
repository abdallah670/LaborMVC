using Polly;
using Polly.Retry;
using Stripe;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service for handling payment operations with retry logic
    /// Uses Polly for resilient payment processing
    /// </summary>
    public interface IPaymentRetryService
    {
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName);
        Task ExecuteWithRetryAsync(Func<Task> operation, string operationName);
    }

    public class PaymentRetryService : IPaymentRetryService
    {
        private readonly ILogger<PaymentRetryService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncRetryPolicy _stripeRetryPolicy;

        public PaymentRetryService(ILogger<PaymentRetryService> logger)
        {
            _logger = logger;

            // General retry policy for database operations
            _retryPolicy = Policy
                .Handle<Exception>(ex => ex is not StripeException) // Don't retry Stripe exceptions with this policy
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 seconds
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Retry {RetryCount} after {Delay}s for operation: {Operation}",
                            retryCount,
                            timeSpan.TotalSeconds,
                            context["operationName"] ?? "Unknown"
                        );
                    });

            // Special retry policy for Stripe API calls
            _stripeRetryPolicy = Policy
                .Handle<StripeException>(ex =>
                    (int)ex.HttpStatusCode == 429 || // Too Many Requests
                    (int)ex.HttpStatusCode == 503 || // Service Unavailable
                    (int)ex.HttpStatusCode == 504 || // Gateway Timeout
                    (int)ex.HttpStatusCode == 500 || // Internal Server Error (temporary)
                    ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + // 2, 4, 8, 16, 32 seconds
                        TimeSpan.FromMilliseconds(new Random().Next(0, 1000)), // Add jitter
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Stripe API Retry {RetryCount} after {Delay}s for operation: {Operation}. Status: {StatusCode}",
                            retryCount,
                            timeSpan.TotalSeconds,
                            context["operationName"] ?? "Unknown",
                            (exception as StripeException)?.HttpStatusCode ?? 0
                        );
                    });
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            var context = new Context { { "operationName", operationName } };

            try
            {
                return await _retryPolicy.ExecuteAsync(async ctx => await operation(), context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation failed after all retries: {Operation}", operationName);
                throw;
            }
        }

        public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
        {
            var context = new Context { { "operationName", operationName } };

            try
            {
                await _retryPolicy.ExecuteAsync(async ctx => await operation(), context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation failed after all retries: {Operation}", operationName);
                throw;
            }
        }

        /// <summary>
        /// Execute Stripe API call with specific retry policy for Stripe exceptions
        /// </summary>
        public async Task<T> ExecuteStripeWithRetryAsync<T>(Func<Task<T>> stripeOperation, string operationName)
        {
            var context = new Context { { "operationName", operationName } };

            try
            {
                return await _stripeRetryPolicy.ExecuteAsync(async ctx => await stripeOperation(), context);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe operation failed after all retries: {Operation}. Status: {StatusCode}, StripeError: {StripeError}",
                    operationName,
                    ex.HttpStatusCode,
                    ex.StripeError?.Message ?? "Unknown"
                );
                throw;
            }
        }

        /// <summary>
        /// Execute Stripe API call with specific retry policy for Stripe exceptions
        /// </summary>
        public async Task ExecuteStripeWithRetryAsync(Func<Task> stripeOperation, string operationName)
        {
            var context = new Context { { "operationName", operationName } };

            try
            {
                await _stripeRetryPolicy.ExecuteAsync(async ctx => await stripeOperation(), context);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe operation failed after all retries: {Operation}. Status: {StatusCode}, StripeError: {StripeError}",
                    operationName,
                    ex.HttpStatusCode,
                    ex.StripeError?.Message ?? "Unknown"
                );
                throw;
            }
        }
    }
}
