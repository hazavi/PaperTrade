using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.Api.Health;

public sealed class DatabaseHealthCheck(PaperTradeDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
}
