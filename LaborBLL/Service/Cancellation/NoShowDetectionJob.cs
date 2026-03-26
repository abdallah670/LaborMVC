using LaborBLL.Service.Abstract;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Cancellation
{
    /// <summary>
    /// Background job for detecting no-show scenarios
    /// Runs periodically via Hangfire to check for tasks that have passed the no-show threshold
    /// </summary>
    public class NoShowDetectionJob
    {
        private readonly ICancellationService _cancellationService;
        private readonly ILogger<NoShowDetectionJob> _logger;

        public NoShowDetectionJob(
            ICancellationService cancellationService,
            ILogger<NoShowDetectionJob> logger)
        {
            _cancellationService = cancellationService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the no-show detection job
        /// Called by Hangfire on a schedule (e.g., every 5 minutes)
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("No-show detection job started at {Time:O}", DateTimeOffset.UtcNow);

            try
            {
                // Get all tasks eligible for no-show detection
                var taskIds = await _cancellationService.GetTasksForNoShowDetectionAsync();

                if (!taskIds.Any())
                {
                    _logger.LogInformation("No tasks found for no-show detection");
                    return;
                }

                _logger.LogInformation("Found {Count} tasks for no-show detection", taskIds.Count());

                int processed = 0;
                int failed = 0;

                foreach (var taskId in taskIds)
                {
                    try
                    {
                        var result = await _cancellationService.DetectNoShowAsync(taskId);

                        if (result.Success)
                        {
                            _logger.LogInformation(
                                "No-show processed for TaskId: {TaskId}, Outcome: {Outcome}",
                                taskId, result.OutcomeDescription);
                            processed++;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "No-show detection failed for TaskId: {TaskId}, Error: {Error}",
                                taskId, result.ErrorMessage);
                            failed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception processing no-show for TaskId: {TaskId}", taskId);
                        failed++;
                    }
                }

                _logger.LogInformation(
                    "No-show detection job completed. Processed: {Processed}, Failed: {Failed}",
                    processed, failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No-show detection job failed");
                throw;
            }
        }
    }
}
