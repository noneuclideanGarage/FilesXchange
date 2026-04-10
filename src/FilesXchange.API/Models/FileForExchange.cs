namespace FilesXchange.API.Models;

public record FileForExchange(
    Guid Id,
    string Token,
    IList<string> PathsToFiles,
    DateTime CreatedAt,
    DateTime ExpiresAt
);
