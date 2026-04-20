using FilesXchange.API.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FilesXchange.API.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly FilesXchangeDbContext _context;

    public DatabaseHealthCheck(FilesXchangeDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("SQLite connection is available.");
            }

            return HealthCheckResult.Unhealthy("SQLite connection check returned false.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SQLite connection check failed.", exception);
        }
    }
}