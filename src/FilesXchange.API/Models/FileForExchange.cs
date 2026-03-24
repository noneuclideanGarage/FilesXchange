namespace FilesXchange.API.Models;

public record FileForExchange(
    Guid Id,
    string Token,
    string PathToFile,
    DateTime CreatedAt,
    DateTime ExpiresAt
);
