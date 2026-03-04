using System;
using System.Threading;
using System.Threading.Tasks;
using LaborDAL.DB;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LaborPL.HealthChecks
{
    /// <summary>
    /// Health check to verify database connectivity via ApplicationDbContext
    /// </summary>
    public class ApplicationDbContextHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _dbContext;

        public ApplicationDbContextHealthCheck(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to connect to the database
                bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Database connection is healthy.");
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to database.");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Database health check failed.",
                    ex);
            }
        }
    }
}
