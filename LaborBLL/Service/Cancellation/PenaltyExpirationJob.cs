using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Cancellation
{
    /// <summary>
    /// Background job for expiring old penalties and freeing restricted users
    /// Runs periodically via Hangfire to check for expired penalties, restrictions, and suspensions
    /// </summary>
    public class PenaltyExpirationJob
    {
        private readonly IPenaltyService _penaltyService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PenaltyExpirationJob> _logger;

        public PenaltyExpirationJob(
            IPenaltyService penaltyService,
            IEmailService emailService,
            ILogger<PenaltyExpirationJob> logger)
        {
            _penaltyService = penaltyService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the penalty expiration job
        /// Called by Hangfire on a schedule (e.g., daily at midnight)
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Penalty expiration job started at {Time:O}", DateTimeOffset.UtcNow);

            try
            {
                // Expire old penalties and get the count
                var expiredCount = await _penaltyService.ExpireOldPenaltiesAsync();

                if (expiredCount > 0)
                {
                    _logger.LogInformation("Expired {Count} penalties", expiredCount);
                }
                else
                {
                    _logger.LogInformation("No penalties to expire");
                }

                // TODO: Send notifications to users whose restrictions were lifted
                // This would require enhancing the PenaltyService to return affected user IDs

                _logger.LogInformation("Penalty expiration job completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Penalty expiration job failed");
                throw;
            }
        }
    }
}
