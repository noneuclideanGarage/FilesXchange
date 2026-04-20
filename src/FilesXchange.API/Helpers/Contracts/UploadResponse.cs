namespace FilesXchange.API.Helpers.Contracts;

public sealed record UploadResponse(
    string Token,
    int FileCount,
    DateTime ExpiresAt,
    string DownloadUrl);