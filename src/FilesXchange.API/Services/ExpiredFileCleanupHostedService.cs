using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Options;
using Microsoft.Extensions.Options;

namespace FilesXchange.API.Services;

public sealed class ExpiredFileCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ExpiredFileCleanupHostedService> _logger;
    private readonly TimeSpan _interval;

    public ExpiredFileCleanupHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<AppOptions> options,
        ILogger<ExpiredFileCleanupHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.CleanupIntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CLEANUP] Background cleanup service started with interval {IntervalSeconds}s",
            _interval.TotalSeconds);

        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }

        _logger.LogInformation("[CLEANUP] Background cleanup service stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cleanupService = scope.ServiceProvider.GetRequiredService<IExpiredFileCleanupService>();
            var result = await cleanupService.ExecuteOnceAsync(cancellationToken);

            _logger.LogInformation(
                "[CLEANUP] ExpiredFound={ExpiredFound}, Removed={Removed}, Failed={Failed}",
                result.ExpiredFound,
                result.Removed,
                result.Failed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "[CLEANUP ERROR] Background cleanup iteration failed");
        }
    }
}