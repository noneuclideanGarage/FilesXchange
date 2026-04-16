using FilesXchange.API.Models;

namespace FilesXchange.API.Helpers.Interfaces;

public interface ICacheService
{
    Task<FileForExchange?> GetFileExchangeByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    void SetFileExchange(FileForExchange fileExchange);

    void RemoveFileExchange(string token);
}