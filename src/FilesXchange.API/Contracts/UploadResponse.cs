namespace FilesXchange.API.Contracts;

public sealed record UploadResponse(
    string Token,
    int FileCount,
    DateTime ExpiresAt,
    string DownloadUrl);