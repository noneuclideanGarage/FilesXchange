namespace FilesXchange.API.Contracts;

public sealed record FileInfoResponse(
    string Token,
    int FileCount,
    DateTime ExpiresAt,
    IReadOnlyList<string> FileNames,
    string DownloadUrl);