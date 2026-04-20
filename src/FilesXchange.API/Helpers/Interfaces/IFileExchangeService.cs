using FilesXchange.API.Helpers.ServiceObjects.FileExchange;

namespace FilesXchange.API.Helpers.Interfaces;

public interface IFileExchangeService
{
    Task<FileExchangeCreationResult> CreateAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default);
}