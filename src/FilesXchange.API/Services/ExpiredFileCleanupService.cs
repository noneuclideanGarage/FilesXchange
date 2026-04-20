using FilesXchange.API.Data;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Helpers.ServiceObjects.ExpiredFileCleanup;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Services;

public sealed class ExpiredFileCleanupService : IExpiredFileCleanupService
{
    private readonly FilesXchangeDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ExpiredFileCleanupService> _logger;

    public ExpiredFileCleanupService(
        FilesXchangeDbContext context,
        IFileStorageService fileStorageService,
        ICacheService cacheService,
        ILogger<ExpiredFileCleanupService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ExpiredCleanupResult> ExecuteOnceAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredExchanges = await _context.Files
            .Where(exchange => exchange.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        var removed = 0;
        var failed = 0;

        foreach (var exchange in expiredExchanges)
        {
            var relativePaths = Array.Empty<string>();

            try
            {
                relativePaths = StoredFileMetadataSerializer.Deserialize(exchange.PathsToFiles)
                    .Select(static file => file.RelativePath)
                    .ToArray();
                _context.Files.Remove(exchange);
                await _context.SaveChangesAsync(cancellationToken);
                _cacheService.RemoveFileExchange(exchange.Token);

                try
                {
                    await _fileStorageService.DeleteFilesDirectoryAsync(relativePaths, cancellationToken);
                }
                catch (Exception exception)
                {
                    failed++;
                    _logger.LogError(exception,
                        "[CLEANUP ERROR] Expired token {Token} was removed from the database, but file deletion failed",
                        exchange.Token);
                }

                removed++;
            }
            catch (Exception exception)
            {
                _context.Entry(exchange).State = EntityState.Unchanged;
                failed++;
                _logger.LogError(exception,
                    "[CLEANUP ERROR] Failed to remove expired token {Token} expiring at {ExpiresAt}",
                    exchange.Token,
                    exchange.ExpiresAt);
            }
        }

        return new ExpiredCleanupResult(expiredExchanges.Count, removed, failed);
    }
}