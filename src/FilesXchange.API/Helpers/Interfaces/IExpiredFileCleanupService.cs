using FilesXchange.API.Helpers.ServiceObjects.ExpiredFileCleanup;

namespace FilesXchange.API.Helpers.Interfaces;

public interface IExpiredFileCleanupService
{
    Task<ExpiredCleanupResult> ExecuteOnceAsync(CancellationToken cancellationToken = default);
}