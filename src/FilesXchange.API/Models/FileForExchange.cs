namespace FilesXchange.API.Models;

public sealed class FileForExchange
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string PathsToFiles { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
